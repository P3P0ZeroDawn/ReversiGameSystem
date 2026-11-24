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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[TestCategory("LiveIntegration")]
public sealed class MatchEndToEndTests
{
    [TestMethod]
    public void TwoPlayers_RealServer_StartsConfirmedBoardAndExchangesChat()
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
            Assert.AreEqual(false, LoadMatches(firstLobby)
                .Any(match => match.MatchCode == waiting.MatchCode));
            Assert.AreEqual(true, LoadMatches(secondLobby)
                .Any(match => match.MatchCode == waiting.MatchCode && match.CreatorDisplayName == "ReviewAlpha"));

            LobbySnapshot joined = JoinMatch(secondLobby, waiting.MatchCode);
            Assert.AreEqual(waiting.MatchId, joined.MatchId);
            Assert.AreEqual(true, SpinWait.SpinUntil(() => started == 1,
                TimeSpan.FromSeconds(10)));
            AssertBoard(LoadState(first, waiting.MatchId));
            AssertBoard(LoadState(second, waiting.MatchId));
            MatchInformation firstInfo = LoadInformation(first, waiting.MatchId);
            MatchInformation secondInfo = LoadInformation(second, waiting.MatchId);
            Assert.AreEqual(1, firstInfo.AssignedSeatNumber);
            Assert.AreEqual(2, secondInfo.AssignedSeatNumber);
            Assert.AreEqual("ReviewAlpha", firstInfo.Participants.Single(player => player.SeatNumber == 1).DisplayName);
            Assert.AreEqual("ReviewBeta", firstInfo.Participants.Single(player => player.SeatNumber == 2).DisplayName);
            Assert.AreEqual(40, firstInfo.TurnDurationSeconds);
            Assert.AreEqual(true, SpinWait.SpinUntil(
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
            Assert.AreEqual(true, SpinWait.SpinUntil(
                () => first.ReadParticipantConnection(waiting.MatchId, 2)
                    == ParticipantConnectionState.Disconnected,
                TimeSpan.FromSeconds(10)));
            Assert.AreEqual(
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
            Assert.AreEqual(MatchStatus.InvalidSession, guestInformation);
            ChatStatus guestChat = outsiderChat.OpenChatChannel(waiting.MatchId)
                .GetAwaiter().GetResult();
            Assert.AreEqual(ChatStatus.InvalidSession, guestChat);
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
        Assert.AreEqual(PreferencesStatus.Success,
            preferences.ChangeLanguage(token, "en-US").GetAwaiter().GetResult());
        using Proxy.PlayerPreferencesServiceClient afterClient = CreateClient();
        Proxy.PlayerPreferences after = afterClient.GetPlayerPreferences(token);
        Assert.AreEqual("en-US", after.PreferredLocaleCode);
        Assert.AreEqual(before.PieceStyleCode, after.PieceStyleCode);
        Assert.AreEqual(before.TimeZoneId, after.TimeZoneId);
        Assert.AreEqual(before.UseCustomBoardBackground, after.UseCustomBoardBackground);
    }

    private static AuthenticatedAccountSession Authenticate(
        SessionClient sessions,
        string login,
        string verificationCode)
    {
        SessionResult<AuthenticatedAccountSession> result = sessions.CompleteLogin(
            login,
            verificationCode).GetAwaiter().GetResult();
        Assert.AreEqual(SessionStatus.Success, result.Status);
        return TestAssert.IsNotNull(result.Value);
    }

    private static GuestAccountSession OpenGuest(SessionClient sessions, string displayName)
    {
        SessionResult<GuestAccountSession> result = sessions.OpenGuestSession(displayName)
            .GetAwaiter().GetResult();
        Assert.AreEqual(SessionStatus.Success, result.Status);
        return TestAssert.IsNotNull(result.Value);
    }

    private static void AssertBoard(MatchStateSnapshot state)
    {
        Assert.AreEqual(ConfirmedMatchState.InProgress, state.State);
        Assert.AreEqual(64, state.Board.Length);
        Assert.AreEqual(2, state.BlackPieceCount);
        Assert.AreEqual(2, state.WhitePieceCount);
        Assert.AreEqual(1, state.CurrentTurnSeatNumber);
        Assert.AreEqual(ConfirmedBoardCell.White, state.Board[27]);
        Assert.AreEqual(ConfirmedBoardCell.Black, state.Board[28]);
        Assert.AreEqual(ConfirmedBoardCell.Black, state.Board[35]);
        Assert.AreEqual(ConfirmedBoardCell.White, state.Board[36]);
        Assert.AreEqual(60, state.Board.Count(cell => cell == ConfirmedBoardCell.Empty));
    }

    private static void CancelledCode_RemoteBoundary_RejectsJoin(
        MatchLobbyClient firstLobby, MatchLobbyClient secondLobby, GameplayClient second)
    {
        LobbySnapshot cancelled = CreateMatch(firstLobby);
        MatchStatus unauthorized = second
            .LoadMatchInformation(cancelled.MatchId).GetAwaiter().GetResult();
        Assert.AreEqual(MatchStatus.Unauthorized, unauthorized);
        firstLobby.CancelMatch(cancelled.MatchId).GetAwaiter().GetResult();
        MatchStatus unavailable = secondLobby.JoinMatch(cancelled.MatchCode)
            .GetAwaiter().GetResult();
        Assert.AreEqual(MatchStatus.MatchCodeUnavailable, unavailable);
    }

    private static void Chat_RealDuplex_ReconcilesReturnCallbackAndHistory(
        MatchChatClient first, MatchChatClient second, long matchId)
    {
        first.OpenChatChannel(matchId).GetAwaiter().GetResult();
        second.OpenChatChannel(matchId).GetAwaiter().GetResult();
        first.LoadChatHistory(matchId).GetAwaiter().GetResult();
        Assert.AreEqual(0, first.ReadMessages(matchId).Length);
        first.SendChatMessage(matchId, "  Hello from alpha  ").GetAwaiter().GetResult();
        Assert.AreEqual(true, SpinWait.SpinUntil(() => second.ReadMessages(matchId).Length == 1,
            TimeSpan.FromSeconds(10)));
        first.LoadChatHistory(matchId).GetAwaiter().GetResult();
        second.LoadChatHistory(matchId).GetAwaiter().GetResult();
        Assert.AreEqual(1, first.ReadMessages(matchId).Length);
        Assert.AreEqual("Hello from alpha", second.ReadMessages(matchId)[0].Content);
        Assert.AreEqual(1, second.ReadMessages(matchId)[0].SenderSeatNumber);
        second.SendChatMessage(matchId, "Hello from beta").GetAwaiter().GetResult();
        Assert.AreEqual(true, SpinWait.SpinUntil(() => first.ReadMessages(matchId).Length == 2,
            TimeSpan.FromSeconds(10)));
        ChatStatus blocked = first.SendChatMessage(matchId, "blockedword")
            .GetAwaiter().GetResult();
        Assert.AreEqual(ChatStatus.MessageBlocked, blocked);
        first.LoadChatHistory(matchId).GetAwaiter().GetResult();
        Assert.AreEqual(2, first.ReadMessages(matchId).Length);
        Assert.AreEqual(2, first.ReadMessages(matchId)[1].SenderSeatNumber);
    }

    private static LobbySnapshot CreateMatch(MatchLobbyClient client)
    {
        Assert.AreEqual(MatchStatus.Success, client.CreateMatch(40).GetAwaiter().GetResult());
        return client.ReadCurrentLobby();
    }

    private static LobbySnapshot JoinMatch(MatchLobbyClient client, string code)
    {
        Assert.AreEqual(MatchStatus.Success, client.JoinMatch(code).GetAwaiter().GetResult());
        return client.ReadCurrentLobby();
    }

    private static AvailableMatch[] LoadMatches(MatchLobbyClient client)
    {
        Assert.AreEqual(MatchStatus.Success, client.LoadAvailableMatches().GetAwaiter().GetResult());
        return client.ReadAvailableMatches();
    }

    private static MatchStateSnapshot LoadState(GameplayClient client, long matchId)
    {
        Assert.AreEqual(MatchStatus.Success, client.LoadMatchState(matchId).GetAwaiter().GetResult());
        return client.ReadMatchState(matchId);
    }

    private static MatchInformation LoadInformation(GameplayClient client, long matchId)
    {
        Assert.AreEqual(MatchStatus.Success,
            client.LoadMatchInformation(matchId).GetAwaiter().GetResult());
        return client.ReadMatchInformation(matchId);
    }
}
