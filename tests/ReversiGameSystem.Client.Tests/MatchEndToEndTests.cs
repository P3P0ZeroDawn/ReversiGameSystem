using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using MatchInformation = ReversiGameSystem.Matches.MatchInformation;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

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
            VerifyLanguagePersistence(alpha.SessionToken);
            Console.WriteLine("PASS: language persisted through the real preferences proxy.");
            using MatchLobbyClient firstLobby = MatchClientsTests.CreateLobby(alpha.SessionToken);
            using GameplayClient first = MatchClientsTests.CreateGameplay(alpha.SessionToken);
            using MatchChatClient firstChat = MatchClientsTests.CreateChat(alpha.SessionToken);
            using MatchLobbyClient secondLobby = MatchClientsTests.CreateLobby(beta.SessionToken);
            using GameplayClient second = MatchClientsTests.CreateGameplay(beta.SessionToken);
            using MatchChatClient secondChat = MatchClientsTests.CreateChat(beta.SessionToken);
            CancelledCode_RemoteBoundary_RejectsJoin(firstLobby, secondLobby, second);
            Console.WriteLine("PASS: canceled code and registered outsider authorization.");
            int started = 0;
            firstLobby.MatchStarted += _ => Interlocked.Increment(ref started);
            LobbySnapshot waiting = CreateMatch(firstLobby);
            TestAssert.AreEqual(false, LoadMatches(firstLobby)
                .Any(match => match.MatchCode == waiting.MatchCode));
            TestAssert.AreEqual(true, LoadMatches(secondLobby)
                .Any(match => match.MatchCode == waiting.MatchCode && match.CreatorDisplayName == "ReviewAlpha"));

            LobbySnapshot joined = JoinMatch(secondLobby, waiting.MatchCode);
            TestAssert.AreEqual(waiting.MatchId, joined.MatchId);
            TestAssert.AreEqual(true, SpinWait.SpinUntil(() => started == 1,
                TimeSpan.FromSeconds(10)));
            AssertBoard(LoadState(first, waiting.MatchId));
            AssertBoard(LoadState(second, waiting.MatchId));
            MatchInformation firstInfo = LoadInformation(first, waiting.MatchId);
            MatchInformation secondInfo = LoadInformation(second, waiting.MatchId);
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
            Chat_RealDuplex_ReconcilesReturnCallbackAndHistory(firstChat, secondChat, waiting.MatchId);
            Console.WriteLine("PASS: two-way chat, history reconciliation and moderation.");

            sessions.CloseSession(beta.SessionToken).GetAwaiter().GetResult();
            beta = null;
            TestAssert.AreEqual(true, SpinWait.SpinUntil(
                () => first.ReadParticipantConnection(waiting.MatchId, 2)
                    == ParticipantConnectionState.Disconnected,
                TimeSpan.FromSeconds(10)));
            TestAssert.AreEqual(
                ConfirmedMatchState.InProgress,
                LoadState(first, waiting.MatchId).State);
            Console.WriteLine("PASS: session closure publishes participant disconnection without ending the match.");

            guest = OpenGuest(
                sessions,
                "Outsider" + Guid.NewGuid().ToString("N").Substring(0, 6));
            using MatchLobbyClient outsiderLobby = MatchClientsTests.CreateLobby(guest.SessionToken);
            using GameplayClient outsider = MatchClientsTests.CreateGameplay(guest.SessionToken);
            using MatchChatClient outsiderChat = MatchClientsTests.CreateChat(guest.SessionToken);
            MatchStatus guestInformation = outsider
                .LoadMatchInformation(waiting.MatchId).GetAwaiter().GetResult();
            TestAssert.AreEqual(MatchStatus.InvalidSession, guestInformation);
            ChatStatus guestChat = outsiderChat.OpenChatChannel(waiting.MatchId)
                .GetAwaiter().GetResult();
            TestAssert.AreEqual(ChatStatus.InvalidSession, guestChat);
        }
        finally
        {
            if (guest != null) { sessions.CloseSession(guest.SessionToken).GetAwaiter().GetResult(); }
            if (beta != null) { sessions.CloseSession(beta.SessionToken).GetAwaiter().GetResult(); }
            sessions.CloseSession(alpha.SessionToken).GetAwaiter().GetResult();
        }
    }

    private static void VerifyLanguagePersistence(string token)
    {
        const string Address = "net.tcp://localhost:18098/ReversiReview/PlayerPreferences";
        Proxy.PlayerPreferencesServiceClient CreateClient()
        {
            return new Proxy.PlayerPreferencesServiceClient(new NetTcpBinding(SecurityMode.None),
                new EndpointAddress(Address));
        }
        PlayerPreferencesClient preferences = new PlayerPreferencesClient(
            NullLogger<PlayerPreferencesClient>.Instance, CreateClient);
        using Proxy.PlayerPreferencesServiceClient beforeClient = CreateClient();
        Proxy.PlayerPreferences before = beforeClient.GetPlayerPreferences(token);
        TestAssert.AreEqual(PreferencesStatus.Success,
            preferences.ChangeLanguage(token, "en-US").GetAwaiter().GetResult());
        using Proxy.PlayerPreferencesServiceClient afterClient = CreateClient();
        Proxy.PlayerPreferences after = afterClient.GetPlayerPreferences(token);
        TestAssert.AreEqual("en-US", after.PreferredLocaleCode);
        TestAssert.AreEqual(before.PieceStyleCode, after.PieceStyleCode);
        TestAssert.AreEqual(before.TimeZoneId, after.TimeZoneId);
        TestAssert.AreEqual(before.UseCustomBoardBackground, after.UseCustomBoardBackground);
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

    private static void CancelledCode_RemoteBoundary_RejectsJoin(
        MatchLobbyClient firstLobby, MatchLobbyClient secondLobby, GameplayClient second)
    {
        LobbySnapshot cancelled = CreateMatch(firstLobby);
        MatchStatus unauthorized = second
            .LoadMatchInformation(cancelled.MatchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(MatchStatus.Unauthorized, unauthorized);
        firstLobby.CancelMatch(cancelled.MatchId).GetAwaiter().GetResult();
        MatchStatus unavailable = secondLobby.JoinMatch(cancelled.MatchCode)
            .GetAwaiter().GetResult();
        TestAssert.AreEqual(MatchStatus.MatchCodeUnavailable, unavailable);
    }

    private static void Chat_RealDuplex_ReconcilesReturnCallbackAndHistory(
        MatchChatClient first, MatchChatClient second, long matchId)
    {
        first.OpenChatChannel(matchId).GetAwaiter().GetResult();
        second.OpenChatChannel(matchId).GetAwaiter().GetResult();
        first.LoadChatHistory(matchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(0, first.ReadMessages(matchId).Length);
        first.SendChatMessage(matchId, "  Hello from alpha  ").GetAwaiter().GetResult();
        TestAssert.AreEqual(true, SpinWait.SpinUntil(() => second.ReadMessages(matchId).Length == 1,
            TimeSpan.FromSeconds(10)));
        first.LoadChatHistory(matchId).GetAwaiter().GetResult();
        second.LoadChatHistory(matchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(1, first.ReadMessages(matchId).Length);
        TestAssert.AreEqual("Hello from alpha", second.ReadMessages(matchId)[0].Content);
        TestAssert.AreEqual(1, second.ReadMessages(matchId)[0].SenderSeatNumber);
        second.SendChatMessage(matchId, "Hello from beta").GetAwaiter().GetResult();
        TestAssert.AreEqual(true, SpinWait.SpinUntil(() => first.ReadMessages(matchId).Length == 2,
            TimeSpan.FromSeconds(10)));
        ChatStatus blocked = first.SendChatMessage(matchId, "blockedword")
            .GetAwaiter().GetResult();
        TestAssert.AreEqual(ChatStatus.MessageBlocked, blocked);
        first.LoadChatHistory(matchId).GetAwaiter().GetResult();
        TestAssert.AreEqual(2, first.ReadMessages(matchId).Length);
        TestAssert.AreEqual(2, first.ReadMessages(matchId)[1].SenderSeatNumber);
    }

    private static LobbySnapshot CreateMatch(MatchLobbyClient client)
    {
        TestAssert.AreEqual(MatchStatus.Success, client.CreateMatch(40).GetAwaiter().GetResult());
        return client.ReadCurrentLobby();
    }

    private static LobbySnapshot JoinMatch(MatchLobbyClient client, string code)
    {
        TestAssert.AreEqual(MatchStatus.Success, client.JoinMatch(code).GetAwaiter().GetResult());
        return client.ReadCurrentLobby();
    }

    private static AvailableMatch[] LoadMatches(MatchLobbyClient client)
    {
        TestAssert.AreEqual(MatchStatus.Success, client.LoadAvailableMatches().GetAwaiter().GetResult());
        return client.ReadAvailableMatches();
    }

    private static MatchStateSnapshot LoadState(GameplayClient client, long matchId)
    {
        TestAssert.AreEqual(MatchStatus.Success, client.LoadMatchState(matchId).GetAwaiter().GetResult());
        return client.ReadMatchState(matchId);
    }

    private static MatchInformation LoadInformation(GameplayClient client, long matchId)
    {
        TestAssert.AreEqual(MatchStatus.Success,
            client.LoadMatchInformation(matchId).GetAwaiter().GetResult());
        return client.ReadMatchInformation(matchId);
    }
}
