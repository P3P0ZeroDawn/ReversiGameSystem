using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Tests.TestDoubles;
using AppGameplayMatchService = ReversiGameSystem.Server.Application.Matches.GameplayMatchApplicationService;
using AppMatchService = ReversiGameSystem.Server.Application.Matches.MatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.GameplayMatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchAuditLoggingTests
{
    private const string ActiveSessionToken = "registered-token-1";
    private const string GuestSessionToken = "guest-token-1";

    [TestMethod]
    public void LobbyCreateAndJoin_RegisteredAndGuest_LogsSafeActorsWithoutTokens()
    {
        UtcClockStub clock = new UtcClockStub();
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

        Match waitingMatch = new Match
        {
            Id = 50,
            Code = "ABC234",
            State = "Waiting",
            TurnDurationSeconds = 30,
            Participants = new[]
            {
                new MatchParticipant { SeatNumber = 1, AccountId = 10, DisplayName = "HostPlayer" }
            }
        };
        Match startedMatch = new Match
        {
            Id = 50,
            Code = "ABC234",
            State = "InProgress",
            TurnDurationSeconds = 30,
            Participants = new[]
            {
                new MatchParticipant { SeatNumber = 1, AccountId = 10, DisplayName = "HostPlayer" },
                new MatchParticipant { SeatNumber = 2, DisplayName = "Guest-Challenger", IsGuest = true }
            }
        };
        MatchRepositoryStub repository = new MatchRepositoryStub
        {
            CreateMatchResult = new MatchResult<Match>(waitingMatch),
            JoinMatchResult = new MatchResult<Match>(startedMatch)
        };
        RecordingLogger<AppMatchService> lobbyLogger = new RecordingLogger<AppMatchService>();
        AppMatchService lobby = new AppMatchService(sessions, repository, clock, lobbyLogger);

        MatchResult<Match> created = lobby.CreateMatch(
            ActiveSessionToken,
            new MatchSettings { OpponentKind = "Human", TurnDurationSeconds = 30 });
        Assert.AreEqual(MatchStatus.Success, created.Status);

        MatchResult<Match> joined = lobby.JoinMatch(GuestSessionToken, "ABC234");
        Assert.AreEqual(MatchStatus.Success, joined.Status);

        Assert.IsTrue(lobbyLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Account:10")
            && entry.Message.Contains("created")));
        Assert.IsTrue(lobbyLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Guest:Guest-Challenger")
            && entry.Message.Contains("joined")));

        string allLogs = string.Join("\n", lobbyLogger.Entries.Select(e => e.Message));
        Assert.IsFalse(allLogs.Contains(ActiveSessionToken));
        Assert.IsFalse(allLogs.Contains(GuestSessionToken));
    }

    [TestMethod]
    public void GameplayForfeitAndDisconnectionTimeout_LogsAuditEvents()
    {
        UtcClockStub clock = new UtcClockStub();
        SessionStore sessions = new SessionStore(clock, NullLogger<SessionStore>.Instance);
        sessions.TryAddAuthenticated(new AuthenticatedSession(
            ActiveSessionToken,
            10,
            "PlayerOne",
            "p1@example.com",
            "en-US",
            clock.UtcNow.AddHours(1)));

        GameplayRepositoryStub repository = new GameplayRepositoryStub();
        RecordingLogger<AppGameplayMatchService> gameplayLogger =
            new RecordingLogger<AppGameplayMatchService>();
        AppGameplayMatchService gameplay = new AppGameplayMatchService(
            sessions,
            repository,
            clock,
            gameplayLogger);

        Match match = new Match
        {
            Id = 99,
            Code = "ABCDEF",
            State = "InProgress",
            TurnDurationSeconds = 30,
            Participants = new[]
            {
                new MatchParticipant { SeatNumber = 1, AccountId = 10, DisplayName = "PlayerOne" },
                new MatchParticipant { SeatNumber = 2, AccountId = 20, DisplayName = "PlayerTwo" }
            }
        };
        gameplay.TrackStartedMatch(match);

        MatchResult<Match> result = gameplay.ForfeitDisconnectedParticipant(99, 1);

        Assert.AreEqual(MatchStatus.Success, result.Status);
        Assert.IsTrue(gameplayLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Gameplay tracking started for match 99")));
        Assert.IsTrue(gameplayLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("99")
            && entry.Message.Contains("30s disconnection timeout")
            && entry.Message.Contains("seat 1")));
    }

    [TestMethod]
    public void GameplayForfeitMatch_Voluntary_LogsInformationWithoutTokens()
    {
        UtcClockStub clock = new UtcClockStub();
        SessionStore sessions = new SessionStore(clock, NullLogger<SessionStore>.Instance);
        sessions.TryAddAuthenticated(new AuthenticatedSession(
            ActiveSessionToken,
            10,
            "PlayerOne",
            "p1@example.com",
            "en-US",
            clock.UtcNow.AddHours(1)));

        GameplayRepositoryStub repository = new GameplayRepositoryStub();
        RecordingLogger<AppGameplayMatchService> gameplayLogger =
            new RecordingLogger<AppGameplayMatchService>();
        AppGameplayMatchService gameplay = new AppGameplayMatchService(
            sessions,
            repository,
            clock,
            gameplayLogger);

        Match match = new Match
        {
            Id = 77,
            Code = "XYZ123",
            State = "InProgress",
            TurnDurationSeconds = 30,
            Participants = new[]
            {
                new MatchParticipant { SeatNumber = 1, AccountId = 10, DisplayName = "PlayerOne" },
                new MatchParticipant { SeatNumber = 2, AccountId = 20, DisplayName = "PlayerTwo" }
            }
        };
        gameplay.TrackStartedMatch(match);

        MatchResult<Match> result = gameplay.ForfeitMatch(ActiveSessionToken, 77);

        Assert.AreEqual(MatchStatus.Success, result.Status);
        Assert.IsTrue(gameplayLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Match 77 forfeited by seat 1")));
        Assert.IsFalse(gameplayLogger.Entries.Any(e => e.Message.Contains(ActiveSessionToken)));
    }
}
