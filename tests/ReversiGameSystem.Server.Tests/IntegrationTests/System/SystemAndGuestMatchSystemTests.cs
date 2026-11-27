using System.Linq;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Leaderboards;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.SystemTests;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("System")]
[TestCategory("Database")]
public sealed class SystemAndGuestMatchSystemTests
{
    private const string SoloUsername = "e2eSoloPlayer";
    private const string SoloEmail = "solo@example.com";
    private const string SoloPassword = "StrongPassword1!";
    private const string GuestDisplayName = "GuestAlias";

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
    public void SystemMatch_GuestSessionAndSystemOpponent_DoesNotAlterLeaderboardStatistics()
    {
        if (_host == null)
        {
            Assert.Fail("SystemTestHost was not initialized.");
            return;
        }

        AuthenticatedSession player = _host.RegisterAndLogin(
            SoloUsername,
            SoloEmail,
            SoloPassword);

        using SystemTestHost.ClientChannelScope<ISessionService> sessionScope =
            _host.OpenChannel<ISessionService>();
        using SystemTestHost.ClientChannelScope<IMatchLobbyService> lobbyScope =
            _host.OpenDuplexChannel<IMatchLobbyService, IMatchLobbyServiceCallback>(
                new NoOpLobbyCallback());
        using SystemTestHost.ClientChannelScope<IGameplayMatchService> gameplayScope =
            _host.OpenDuplexChannel<IGameplayMatchService, IGameplayMatchServiceCallback>(
                new NoOpGameplayCallback());
        using SystemTestHost.ClientChannelScope<ILeaderboardService> leaderboardScope =
            _host.OpenChannel<ILeaderboardService>();

        GuestSession guestSession = sessionScope.Channel.OpenGuestSession(GuestDisplayName);
        LobbySnapshot waitingLobby = lobbyScope.Channel.CreateMatch(
            player.SessionToken,
            new MatchConfiguration
            {
                OpponentKind = OpponentKind.Human,
                TurnDurationSeconds = 30,
                CountsTowardStatistics = false
            });
        MatchStateSnapshot startedState = lobbyScope.Channel.StartSystemMatch(
            player.SessionToken,
            waitingLobby.MatchId);
        MatchStateSnapshot afterMove = gameplayScope.Channel.PlacePiece(
            player.SessionToken,
            new PiecePlacement
            {
                MatchId = waitingLobby.MatchId,
                Row = 2,
                Column = 3
            });
        MatchConclusion conclusion = gameplayScope.Channel.ForfeitMatch(
            player.SessionToken,
            waitingLobby.MatchId);
        sessionScope.Channel.CloseSession(guestSession.SessionToken);
        LeaderboardEntry[] ranking = leaderboardScope.Channel.GetLeaderboard(10);

        long expectedMinVersion = 1L;
        int expectedWinnerSeat = 2;
        int expectedMatchesPlayed = 0;
        int expectedWins = 0;
        LeaderboardEntry soloEntry = ranking.Single(
            entry => entry.Account.AccountId == player.Account.AccountId);

        Assert.IsFalse(string.IsNullOrWhiteSpace(guestSession.SessionToken));
        Assert.AreEqual(waitingLobby.MatchId, startedState.MatchId);
        Assert.IsTrue(afterMove.MatchStateVersion >= expectedMinVersion);
        Assert.AreEqual(expectedWinnerSeat, conclusion.WinnerSeatNumber);
        Assert.AreEqual(expectedMatchesPlayed, soloEntry.MatchesPlayed);
        Assert.AreEqual(expectedWins, soloEntry.Wins);
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class NoOpLobbyCallback : IMatchLobbyServiceCallback
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
    private sealed class NoOpGameplayCallback : IGameplayMatchServiceCallback
    {
        public void OnMatchStateChanged(MatchStateSnapshot state)
        {
        }

        public void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected)
        {
        }
    }
}
