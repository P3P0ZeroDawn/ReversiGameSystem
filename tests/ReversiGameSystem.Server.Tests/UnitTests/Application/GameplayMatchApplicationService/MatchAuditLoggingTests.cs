    using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Tests.TestDoubles;
using AppGameplayMatchService =
    ReversiGameSystem.Server.Application.Matches.GameplayMatchApplicationService;
using AppMatchService = ReversiGameSystem.Server.Application.Matches.MatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.GameplayMatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchAuditLoggingTests
{
    private const string ActiveSessionToken = "registered-token-1";
    private const string GuestSessionToken = "guest-token-1";

    [TestMethod]
    public void CreateMatch_RegisteredPlayer_LogsInformationWithAccountActorWithoutToken()
    {
        UtcClockStub clock = new UtcClockStub();
        SessionStore sessions = CreateSessions(clock);
        MatchRepositoryStub repository = new MatchRepositoryStub
        {
            CreateMatchResult = new MatchResult<Match>(CreateMatchModel(id: 50, state: "Waiting"))
        };
        RecordingLogger<AppMatchService> lobbyLogger = new RecordingLogger<AppMatchService>();
        AppMatchService lobby = new AppMatchService(sessions, repository, clock, lobbyLogger);
        bool expectedLogged = true;

        lobby.CreateMatch(
            ActiveSessionToken,
            new MatchSettings { OpponentKind = "Human", TurnDurationSeconds = 30 });
        bool actualLogged = lobbyLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Account:10")
            && entry.Message.Contains("created")
            && !entry.Message.Contains(ActiveSessionToken));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void JoinMatch_GuestPlayer_LogsInformationWithGuestActorWithoutToken()
    {
        UtcClockStub clock = new UtcClockStub();
        SessionStore sessions = CreateSessions(clock);
        Match startedMatch = CreateMatchModel(id: 50, state: "InProgress");
        startedMatch.Participants = new[]
        {
            new MatchParticipant { SeatNumber = 1, AccountId = 10, DisplayName = "HostPlayer" },
            new MatchParticipant { SeatNumber = 2, DisplayName = "Guest-Challenger", IsGuest = true }
        };
        MatchRepositoryStub repository = new MatchRepositoryStub
        {
            JoinMatchResult = new MatchResult<Match>(startedMatch)
        };
        RecordingLogger<AppMatchService> lobbyLogger = new RecordingLogger<AppMatchService>();
        AppMatchService lobby = new AppMatchService(sessions, repository, clock, lobbyLogger);
        bool expectedLogged = true;

        lobby.JoinMatch(GuestSessionToken, "ABC234");
        bool actualLogged = lobbyLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Guest:Guest-Challenger")
            && entry.Message.Contains("joined")
            && !entry.Message.Contains(GuestSessionToken));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void TrackStartedMatch_InProgressMatch_LogsInformationWithMatchId()
    {
        UtcClockStub clock = new UtcClockStub();
        SessionStore sessions = CreateSessions(clock);
        RecordingLogger<AppGameplayMatchService> gameplayLogger =
            new RecordingLogger<AppGameplayMatchService>();
        AppGameplayMatchService gameplay = new AppGameplayMatchService(
            sessions,
            new GameplayRepositoryStub(),
            clock,
            gameplayLogger);
        Match match = CreateMatchModel(id: 99, state: "InProgress");
        bool expectedLogged = true;

        gameplay.TrackStartedMatch(match);
        bool actualLogged = gameplayLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Gameplay tracking started for match 99"));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void ForfeitDisconnectedParticipant_ActiveMatch_LogsWarningWithTimeoutAndSeat()
    {
        UtcClockStub clock = new UtcClockStub();
        SessionStore sessions = CreateSessions(clock);
        RecordingLogger<AppGameplayMatchService> gameplayLogger =
            new RecordingLogger<AppGameplayMatchService>();
        AppGameplayMatchService gameplay = new AppGameplayMatchService(
            sessions,
            new GameplayRepositoryStub(),
            clock,
            gameplayLogger);
        gameplay.TrackStartedMatch(CreateMatchModel(id: 99, state: "InProgress"));
        bool expectedLogged = true;

        gameplay.ForfeitDisconnectedParticipant(99, 1);
        bool actualLogged = gameplayLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("99")
            && entry.Message.Contains("30s disconnection timeout")
            && entry.Message.Contains("seat 1"));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void ForfeitMatch_Voluntary_LogsInformationWithoutToken()
    {
        UtcClockStub clock = new UtcClockStub();
        SessionStore sessions = CreateSessions(clock);
        RecordingLogger<AppGameplayMatchService> gameplayLogger =
            new RecordingLogger<AppGameplayMatchService>();
        AppGameplayMatchService gameplay = new AppGameplayMatchService(
            sessions,
            new GameplayRepositoryStub(),
            clock,
            gameplayLogger);
        gameplay.TrackStartedMatch(CreateMatchModel(id: 77, state: "InProgress"));
        bool expectedLogged = true;

        gameplay.ForfeitMatch(ActiveSessionToken, 77);
        bool actualLogged = gameplayLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Match 77 forfeited by seat 1")
            && !entry.Message.Contains(ActiveSessionToken));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    private static SessionStore CreateSessions(UtcClockStub clock)
    {
        SessionStore sessions = new SessionStore(clock, NullLogger<SessionStore>.Instance);
        sessions.TryAddAuthenticated(new AuthenticatedSession(
            ActiveSessionToken,
            10,
            "HostPlayer",
            "host@example.com",
            "en-US",
            clock.UtcNow.AddHours(1)));
        sessions.TryReserveGuest(new GuestSession(
            GuestSessionToken,
            "Guest-Challenger",
            clock.UtcNow.AddHours(1)));
        return sessions;
    }

    private static Match CreateMatchModel(long id, string state)
    {
        return new Match
        {
            Id = id,
            Code = "ABC234",
            State = state,
            TurnDurationSeconds = 30,
            Participants = new[]
            {
                new MatchParticipant { SeatNumber = 1, AccountId = 10, DisplayName = "HostPlayer" },
                new MatchParticipant { SeatNumber = 2, AccountId = 20, DisplayName = "PlayerTwo" }
            }
        };
    }
}
