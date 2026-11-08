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
        Proxy.AuthenticatedSession alpha = sessions.CompleteLoginAsync("ReviewAlpha", "000001").GetAwaiter().GetResult();
        Proxy.AuthenticatedSession? beta = null;
        Proxy.GuestSession? guest = null;
        try
        {
            beta = sessions.CompleteLoginAsync("ReviewBeta", "000002").GetAwaiter().GetResult();
            using RemoteMatchClient first = RemoteMatchClientTests.Create(alpha.SessionToken);
            using RemoteMatchClient second = RemoteMatchClientTests.Create(beta.SessionToken);
            CancelledCode_RemoteBoundary_RejectsJoin(first, second);
            Console.WriteLine("PASS: canceled code and registered outsider authorization.");
            Proxy.LobbySnapshot waiting = first.CreateAsync(40).GetAwaiter().GetResult();
            TestAssert.AreEqual(Proxy.MatchState.Waiting, waiting.State);
            TestAssert.AreEqual(1, waiting.Participants.Length);
            TestAssert.AreEqual(1, waiting.Participants[0].SeatNumber);
            TestAssert.AreEqual(false, first.ListAsync().GetAwaiter().GetResult()
                .Any(match => match.MatchCode == waiting.MatchCode));
            TestAssert.AreEqual(true, second.ListAsync().GetAwaiter().GetResult()
                .Any(match => match.MatchCode == waiting.MatchCode && match.CreatorDisplayName == "ReviewAlpha"));

            Proxy.LobbySnapshot joined = second.JoinAsync(waiting.MatchCode).GetAwaiter().GetResult();
            TestAssert.AreEqual(Proxy.MatchState.InProgress, joined.State);
            TestAssert.AreEqual(2, joined.Participants.Length);
            TestAssert.AreEqual(true, SpinWait.SpinUntil(() => first.TryReadState(waiting.MatchId, out _),
                TimeSpan.FromSeconds(10)));
            first.TryReadState(waiting.MatchId, out Proxy.MatchStateSnapshot? started);
            AssertBoard(started!);
            AssertBoard(second.LoadStateAsync(waiting.MatchId).GetAwaiter().GetResult());
            Proxy.MatchInformation firstInfo = first.LoadInformationAsync(waiting.MatchId).GetAwaiter().GetResult();
            Proxy.MatchInformation secondInfo = second.LoadInformationAsync(waiting.MatchId).GetAwaiter().GetResult();
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

            sessions.CloseSessionAsync(beta.SessionToken).GetAwaiter().GetResult();
            beta = null;
            TestAssert.AreEqual(true, SpinWait.SpinUntil(
                () => first.ReadParticipantConnection(waiting.MatchId, 2)
                    == ParticipantConnectionState.Disconnected,
                TimeSpan.FromSeconds(10)));
            TestAssert.AreEqual(
                Proxy.MatchState.InProgress,
                first.LoadStateAsync(waiting.MatchId).GetAwaiter().GetResult().State);
            Console.WriteLine("PASS: session closure publishes participant disconnection without ending the match.");

            guest = sessions.OpenGuestSessionAsync("Outsider" + Guid.NewGuid().ToString("N").Substring(0, 6))
                .GetAwaiter().GetResult();
            using RemoteMatchClient outsider = RemoteMatchClientTests.Create(guest.SessionToken);
            FaultException<Proxy.SessionFault> guestInformation = TestAssert.Throws<FaultException<Proxy.SessionFault>>(
                () => outsider.LoadInformationAsync(waiting.MatchId).GetAwaiter().GetResult());
            TestAssert.AreEqual(Proxy.SessionFailure.InvalidSession, guestInformation.Detail.Reason);
            FaultException<Proxy.SessionFault> guestChat = TestAssert.Throws<FaultException<Proxy.SessionFault>>(
                () => outsider.OpenChatAsync(waiting.MatchId).GetAwaiter().GetResult());
            TestAssert.AreEqual(Proxy.SessionFailure.InvalidSession, guestChat.Detail.Reason);
        }
        finally
        {
            if (guest != null) { sessions.CloseSessionAsync(guest.SessionToken).GetAwaiter().GetResult(); }
            if (beta != null) { sessions.CloseSessionAsync(beta.SessionToken).GetAwaiter().GetResult(); }
            sessions.CloseSessionAsync(alpha.SessionToken).GetAwaiter().GetResult();
        }
    }

    private static void AssertBoard(Proxy.MatchStateSnapshot state)
    {
        TestAssert.AreEqual(Proxy.MatchState.InProgress, state.State);
        TestAssert.AreEqual(64, state.Board.Length);
        TestAssert.AreEqual(2, state.BlackPieceCount);
        TestAssert.AreEqual(2, state.WhitePieceCount);
        TestAssert.AreEqual(1, state.CurrentTurnSeatNumber);
        TestAssert.AreEqual(Proxy.BoardCellState.White, state.Board[27]);
        TestAssert.AreEqual(Proxy.BoardCellState.Black, state.Board[28]);
        TestAssert.AreEqual(Proxy.BoardCellState.Black, state.Board[35]);
        TestAssert.AreEqual(Proxy.BoardCellState.White, state.Board[36]);
        TestAssert.AreEqual(60, state.Board.Count(cell => cell == Proxy.BoardCellState.Empty));
    }

    private static void CancelledCode_RemoteBoundary_RejectsJoin(RemoteMatchClient first, RemoteMatchClient second)
    {
        Proxy.LobbySnapshot cancelled = first.CreateAsync(40).GetAwaiter().GetResult();
        FaultException<Proxy.MatchFault> unauthorized = TestAssert.Throws<FaultException<Proxy.MatchFault>>(
            () => second.LoadInformationAsync(cancelled.MatchId).GetAwaiter().GetResult());
        TestAssert.AreEqual(Proxy.MatchFailure.Unauthorized, unauthorized.Detail.Reason);
        first.CancelAsync(cancelled.MatchId).GetAwaiter().GetResult();
        FaultException<Proxy.MatchFault> unavailable = TestAssert.Throws<FaultException<Proxy.MatchFault>>(
            () => second.JoinAsync(cancelled.MatchCode).GetAwaiter().GetResult());
        TestAssert.AreEqual(Proxy.MatchFailure.MatchCodeUnavailable, unavailable.Detail.Reason);
    }

    private static void Chat_RealDuplex_ReconcilesReturnCallbackAndHistory(
        RemoteMatchClient first, RemoteMatchClient second, long matchId)
    {
        first.OpenChatAsync(matchId).GetAwaiter().GetResult();
        second.OpenChatAsync(matchId).GetAwaiter().GetResult();
        first.RefreshChatAsync(matchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(0, first.ReadMessages(matchId).Length);
        first.SendAsync(matchId, "  Hello from alpha  ").GetAwaiter().GetResult();
        TestAssert.AreEqual(true, SpinWait.SpinUntil(() => second.ReadMessages(matchId).Length == 1,
            TimeSpan.FromSeconds(10)));
        first.RefreshChatAsync(matchId).GetAwaiter().GetResult();
        second.RefreshChatAsync(matchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(1, first.ReadMessages(matchId).Length);
        TestAssert.AreEqual("Hello from alpha", second.ReadMessages(matchId)[0].Content);
        TestAssert.AreEqual(1, second.ReadMessages(matchId)[0].SenderSeatNumber);
        second.SendAsync(matchId, "Hello from beta").GetAwaiter().GetResult();
        TestAssert.AreEqual(true, SpinWait.SpinUntil(() => first.ReadMessages(matchId).Length == 2,
            TimeSpan.FromSeconds(10)));
        FaultException<Proxy.ChatFault> blocked = TestAssert.Throws<FaultException<Proxy.ChatFault>>(
            () => first.SendAsync(matchId, "blockedword").GetAwaiter().GetResult());
        TestAssert.AreEqual(Proxy.ChatFailure.MessageBlocked, blocked.Detail.Reason);
        first.RefreshChatAsync(matchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(2, first.ReadMessages(matchId).Length);
        TestAssert.AreEqual(2, first.ReadMessages(matchId)[1].SenderSeatNumber);
    }
}
