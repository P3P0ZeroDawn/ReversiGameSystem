using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Leaderboards;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.SystemTests;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("System")]
[TestCategory("Database")]
public sealed class MultiplayerMatchSystemTests
{
    private const string PlayerOneUsername = "e2eMatchOne";
    private const string PlayerOneEmail = "match1@example.com";
    private const string PlayerTwoUsername = "e2eMatchTwo";
    private const string PlayerTwoEmail = "match2@example.com";
    private const string SharedPassword = "StrongPassword1!";
    private const string ChatGreeting = "Good luck!";
    private const int CallbackWaitMilliseconds = 5000;

    private static SystemTestHost? _host;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _host = new SystemTestHost();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _host?.Dispose();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _host?.ResetState();
    }

    [TestMethod]
    public void TwoPlayerMatch_LobbyChatGameplayForfeitAndLeaderboard_CompletesEndToEnd()
    {
        if (_host == null)
        {
            Assert.Fail("SystemTestHost was not initialized.");
            return;
        }

        AuthenticatedSession playerOne = _host.RegisterAndLogin(
            PlayerOneUsername,
            PlayerOneEmail,
            SharedPassword);
        AuthenticatedSession playerTwo = _host.RegisterAndLogin(
            PlayerTwoUsername,
            PlayerTwoEmail,
            SharedPassword);

        RecordingLobbyCallback lobbyOneCallback = new RecordingLobbyCallback();
        RecordingLobbyCallback lobbyTwoCallback = new RecordingLobbyCallback();
        RecordingGameplayCallback gameplayOneCallback = new RecordingGameplayCallback();
        RecordingGameplayCallback gameplayTwoCallback = new RecordingGameplayCallback();
        RecordingChatCallback chatOneCallback = new RecordingChatCallback();
        RecordingChatCallback chatTwoCallback = new RecordingChatCallback();

        using SystemTestHost.ClientChannelScope<IMatchLobbyService> lobbyOne =
            _host.OpenDuplexChannel<IMatchLobbyService, IMatchLobbyServiceCallback>(
                lobbyOneCallback);
        using SystemTestHost.ClientChannelScope<IMatchLobbyService> lobbyTwo =
            _host.OpenDuplexChannel<IMatchLobbyService, IMatchLobbyServiceCallback>(
                lobbyTwoCallback);
        using SystemTestHost.ClientChannelScope<IGameplayMatchService> gameplayOne =
            _host.OpenDuplexChannel<IGameplayMatchService, IGameplayMatchServiceCallback>(
                gameplayOneCallback);
        using SystemTestHost.ClientChannelScope<IGameplayMatchService> gameplayTwo =
            _host.OpenDuplexChannel<IGameplayMatchService, IGameplayMatchServiceCallback>(
                gameplayTwoCallback);
        using SystemTestHost.ClientChannelScope<IMatchChatService> chatOne =
            _host.OpenDuplexChannel<IMatchChatService, IMatchChatServiceCallback>(
                chatOneCallback);
        using SystemTestHost.ClientChannelScope<IMatchChatService> chatTwo =
            _host.OpenDuplexChannel<IMatchChatService, IMatchChatServiceCallback>(
                chatTwoCallback);
        using SystemTestHost.ClientChannelScope<ILeaderboardService> leaderboard =
            _host.OpenChannel<ILeaderboardService>();

        lobbyOne.Channel.OpenLobbyChannel(playerOne.SessionToken);
        lobbyTwo.Channel.OpenLobbyChannel(playerTwo.SessionToken);
        LobbySnapshot createdLobby = lobbyOne.Channel.CreateMatch(
            playerOne.SessionToken,
            new MatchConfiguration
            {
                OpponentKind = OpponentKind.Human,
                TurnDurationSeconds = 30,
                CountsTowardStatistics = false
            });
        AvailableMatch[] availableMatches = lobbyTwo.Channel.GetAvailableMatches(
            playerTwo.SessionToken);
        string matchCode = createdLobby.MatchCode ?? string.Empty;
        lobbyTwo.Channel.JoinMatch(playerTwo.SessionToken, matchCode);

        long matchId = createdLobby.MatchId;
        gameplayOne.Channel.OpenMatchChannel(playerOne.SessionToken, matchId);
        gameplayTwo.Channel.OpenMatchChannel(playerTwo.SessionToken, matchId);
        chatOne.Channel.OpenChatChannel(playerOne.SessionToken, matchId);
        chatTwo.Channel.OpenChatChannel(playerTwo.SessionToken, matchId);

        chatOne.Channel.SendChatMessage(playerOne.SessionToken, matchId, ChatGreeting);
        bool chatDelivered = SpinWait.SpinUntil(
            () => chatTwoCallback.LastMessage != null
                && chatTwoCallback.LastMessage.Content == ChatGreeting,
            CallbackWaitMilliseconds);

        MatchStateSnapshot afterMove = gameplayOne.Channel.PlacePiece(
            playerOne.SessionToken,
            new PiecePlacement
            {
                MatchId = matchId,
                Row = 2,
                Column = 3
            });
        bool moveDelivered = SpinWait.SpinUntil(
            () => gameplayTwoCallback.LastState != null
                && gameplayTwoCallback.LastState.MatchStateVersion == afterMove.MatchStateVersion,
            CallbackWaitMilliseconds);

        MatchConclusion conclusion = gameplayTwo.Channel.ForfeitMatch(
            playerTwo.SessionToken,
            matchId);
        LeaderboardEntry[] ranking = leaderboard.Channel.GetLeaderboard(10);

        int expectedAvailableCount = 1;
        long expectedMinVersion = 1L;
        int expectedWinnerSeat = 1;
        int expectedPlayerCount = 2;
        int expectedUncountedWins = 0;
        LeaderboardEntry winnerEntry = ranking.Single(
            entry => entry.Account.AccountId == playerOne.Account.AccountId);
        LeaderboardEntry loserEntry = ranking.Single(
            entry => entry.Account.AccountId == playerTwo.Account.AccountId);

        Assert.AreEqual(expectedAvailableCount, availableMatches.Length);
        Assert.IsTrue(chatDelivered);
        Assert.IsTrue(moveDelivered);
        Assert.IsTrue(afterMove.MatchStateVersion >= expectedMinVersion);
        Assert.AreEqual(expectedWinnerSeat, conclusion.WinnerSeatNumber);
        Assert.AreEqual(expectedPlayerCount, ranking.Length);
        Assert.AreEqual(expectedUncountedWins, winnerEntry.Wins);
        Assert.AreEqual(expectedUncountedWins, loserEntry.Wins);
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class RecordingLobbyCallback : IMatchLobbyServiceCallback
    {
        public void OnMatchStarted(MatchStateSnapshot state)
        {
        }

        public void OnLobbyClosed(long matchId)
        {
        }
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class RecordingGameplayCallback : IGameplayMatchServiceCallback
    {
        private MatchStateSnapshot? _lastState;

        internal MatchStateSnapshot? LastState
        {
            get { return Volatile.Read(ref _lastState); }
        }

        public void OnMatchStateChanged(MatchStateSnapshot state)
        {
            Volatile.Write(ref _lastState, state);
        }

        public void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected)
        {
        }
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class RecordingChatCallback : IMatchChatServiceCallback
    {
        private MatchChatEntry? _lastMessage;

        internal MatchChatEntry? LastMessage
        {
            get { return Volatile.Read(ref _lastMessage); }
        }

        public void OnChatMessageReceived(MatchChatEntry message)
        {
            Volatile.Write(ref _lastMessage, message);
        }

        public void OnPlayerMuted(ChatMuteNotification mute)
        {
        }
    }
}
