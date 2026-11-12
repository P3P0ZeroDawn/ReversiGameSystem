using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

/// <summary>Runs real proxy and adapters against the disposable review match host.</summary>
internal static class MatchEndToEndTests
{
    internal static void TwoPlayers_RealServer_StartsConfirmedBoardAndExchangesChat()
    {
        SessionClient sessions = new(NullLogger<SessionClient>.Instance, () => new Proxy.SessionServiceClient(
            new NetTcpBinding(SecurityMode.None), new EndpointAddress("net.tcp://localhost:18091/ReversiReview/Session")));
        AuthenticatedAccountSession alpha = Authenticate(sessions, "ReviewAlpha", "000001");
        AuthenticatedAccountSession? beta = null;
        GuestAccountSession? guest = null;
        try
        {
            beta = Authenticate(sessions, "ReviewBeta", "000002");
            using RemoteMatchClient first = RemoteMatchClientTests.Create(alpha.SessionToken);
            using RemoteMatchClient second = RemoteMatchClientTests.Create(beta.SessionToken);
            CancelledCode_RemoteBoundary_RejectsJoin(first, second);
            Console.WriteLine("PASS: canceled code and registered outsider authorization.");
            LobbySnapshot waiting = Value(first.CreateMatch(40).GetAwaiter().GetResult());
            TestAssert.AreEqual(false, Value(first.LoadAvailableMatches().GetAwaiter().GetResult())
                .Any(match => match.MatchCode == waiting.MatchCode));
            TestAssert.AreEqual(true, Value(second.LoadAvailableMatches().GetAwaiter().GetResult())
                .Any(match => match.MatchCode == waiting.MatchCode && match.CreatorDisplayName == "ReviewAlpha"));

            LobbySnapshot joined = Value(second.JoinMatch(waiting.MatchCode).GetAwaiter().GetResult());
            TestAssert.AreEqual(waiting.MatchId, joined.MatchId);
            TestAssert.AreEqual(true, SpinWait.SpinUntil(() => first.HasMatchState(waiting.MatchId),
                TimeSpan.FromSeconds(10)));
            AssertBoard(first.ReadMatchState(waiting.MatchId));
            AssertBoard(Value(second.LoadMatchState(waiting.MatchId).GetAwaiter().GetResult()));
            RemoteMatchInformation firstInfo = Value(first.LoadRemoteMatchInformation(
                waiting.MatchId).GetAwaiter().GetResult());
            RemoteMatchInformation secondInfo = Value(second.LoadRemoteMatchInformation(
                waiting.MatchId).GetAwaiter().GetResult());
            TestAssert.AreEqual(1, firstInfo.AssignedSeatNumber);
            TestAssert.AreEqual(2, secondInfo.AssignedSeatNumber);
            TestAssert.AreEqual("ReviewAlpha", firstInfo.Participants.Single(player => player.SeatNumber == 1).DisplayName);
            TestAssert.AreEqual("ReviewBeta", firstInfo.Participants.Single(player => player.SeatNumber == 2).DisplayName);
            TestAssert.AreEqual(40, firstInfo.TurnDurationSeconds);
            TestAssert.AreEqual(true, SpinWait.SpinUntil(
                () => first.ReadParticipantConnection(waiting.MatchId, 1)
                    == ParticipantConnectionState.Connected
                    && first.ReadParticipantConnection(waiting.MatchId, 2)
                    == ParticipantConnectionState.Connected,
                TimeSpan.FromSeconds(10)));
            Console.WriteLine("PASS: two participants, lobby callback and initial board.");
            Chat_RealDuplex_ReconcilesReturnCallbackAndHistory(first, second, waiting.MatchId);
            Console.WriteLine("PASS: two-way chat, history reconciliation and moderation.");

            sessions.CloseSession(beta.SessionToken).GetAwaiter().GetResult();
            beta = null;
            TestAssert.AreEqual(true, SpinWait.SpinUntil(
                () => first.ReadParticipantConnection(waiting.MatchId, 2)
                    == ParticipantConnectionState.Disconnected,
                TimeSpan.FromSeconds(10)));
            TestAssert.AreEqual(
                ConfirmedMatchState.InProgress,
                Value(first.LoadMatchState(waiting.MatchId).GetAwaiter().GetResult()).State);
            Console.WriteLine("PASS: session closure publishes participant disconnection without ending the match.");

            guest = OpenGuest(
                sessions,
                "Outsider" + Guid.NewGuid().ToString("N").Substring(0, 6));
            using RemoteMatchClient outsider = RemoteMatchClientTests.Create(guest.SessionToken);
            MatchResult<RemoteMatchInformation> guestInformation = outsider
                .LoadRemoteMatchInformation(waiting.MatchId).GetAwaiter().GetResult();
            TestAssert.AreEqual(MatchStatus.InvalidSession, guestInformation.Status);
            MatchStatus guestChat = outsider.OpenChatChannel(waiting.MatchId)
                .GetAwaiter().GetResult();
            TestAssert.AreEqual(MatchStatus.InvalidSession, guestChat);
        }
        finally
        {
            if (guest != null) { sessions.CloseSession(guest.SessionToken).GetAwaiter().GetResult(); }
            if (beta != null) { sessions.CloseSession(beta.SessionToken).GetAwaiter().GetResult(); }
            sessions.CloseSession(alpha.SessionToken).GetAwaiter().GetResult();
        }
    }

    private static AuthenticatedAccountSession Authenticate(
        SessionClient sessions,
        string login,
        string verificationCode)
    {
        SessionResult<AuthenticatedAccountSession> result = sessions.CompleteLogin(
            login,
            verificationCode).GetAwaiter().GetResult();
        TestAssert.AreEqual(SessionStatus.Success, result.Status);
        return TestAssert.IsNotNull(result.Value);
    }

    private static GuestAccountSession OpenGuest(SessionClient sessions, string displayName)
    {
        SessionResult<GuestAccountSession> result = sessions.OpenGuestSession(displayName)
            .GetAwaiter().GetResult();
        TestAssert.AreEqual(SessionStatus.Success, result.Status);
        return TestAssert.IsNotNull(result.Value);
    }

    private static void AssertBoard(MatchStateSnapshot state)
    {
        TestAssert.AreEqual(ConfirmedMatchState.InProgress, state.State);
        TestAssert.AreEqual(64, state.Board.Length);
        TestAssert.AreEqual(2, state.BlackPieceCount);
        TestAssert.AreEqual(2, state.WhitePieceCount);
        TestAssert.AreEqual(1, state.CurrentTurnSeatNumber);
        TestAssert.AreEqual(ConfirmedBoardCell.White, state.Board[27]);
        TestAssert.AreEqual(ConfirmedBoardCell.Black, state.Board[28]);
        TestAssert.AreEqual(ConfirmedBoardCell.Black, state.Board[35]);
        TestAssert.AreEqual(ConfirmedBoardCell.White, state.Board[36]);
        TestAssert.AreEqual(60, state.Board.Count(cell => cell == ConfirmedBoardCell.Empty));
    }

    private static void CancelledCode_RemoteBoundary_RejectsJoin(RemoteMatchClient first, RemoteMatchClient second)
    {
        LobbySnapshot cancelled = Value(first.CreateMatch(40).GetAwaiter().GetResult());
        MatchResult<RemoteMatchInformation> unauthorized = second
            .LoadRemoteMatchInformation(cancelled.MatchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(MatchStatus.Unauthorized, unauthorized.Status);
        first.CancelMatch(cancelled.MatchId).GetAwaiter().GetResult();
        MatchResult<LobbySnapshot> unavailable = second.JoinMatch(cancelled.MatchCode)
            .GetAwaiter().GetResult();
        TestAssert.AreEqual(MatchStatus.MatchCodeUnavailable, unavailable.Status);
    }

    private static void Chat_RealDuplex_ReconcilesReturnCallbackAndHistory(
        RemoteMatchClient first, RemoteMatchClient second, long matchId)
    {
        first.OpenChatChannel(matchId).GetAwaiter().GetResult();
        second.OpenChatChannel(matchId).GetAwaiter().GetResult();
        first.RefreshChat(matchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(0, first.ReadMessages(matchId).Length);
        first.SendChatMessage(matchId, "  Hello from alpha  ").GetAwaiter().GetResult();
        TestAssert.AreEqual(true, SpinWait.SpinUntil(() => second.ReadMessages(matchId).Length == 1,
            TimeSpan.FromSeconds(10)));
        first.RefreshChat(matchId).GetAwaiter().GetResult();
        second.RefreshChat(matchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(1, first.ReadMessages(matchId).Length);
        TestAssert.AreEqual("Hello from alpha", second.ReadMessages(matchId)[0].Content);
        TestAssert.AreEqual(1, second.ReadMessages(matchId)[0].SenderSeatNumber);
        second.SendChatMessage(matchId, "Hello from beta").GetAwaiter().GetResult();
        TestAssert.AreEqual(true, SpinWait.SpinUntil(() => first.ReadMessages(matchId).Length == 2,
            TimeSpan.FromSeconds(10)));
        ChatSubmission blocked = first.SendChatMessage(matchId, "blockedword")
            .GetAwaiter().GetResult();
        TestAssert.AreEqual(MatchStatus.MessageBlocked, blocked.Status);
        first.RefreshChat(matchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(2, first.ReadMessages(matchId).Length);
        TestAssert.AreEqual(2, first.ReadMessages(matchId)[1].SenderSeatNumber);
    }

    private static T Value<T>(MatchResult<T> result) where T : class
    {
        TestAssert.AreEqual(MatchStatus.Success, result.Status);
        return TestAssert.IsNotNull(result.Value);
    }
}
