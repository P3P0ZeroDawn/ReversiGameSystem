using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Matches;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("Database")]
public sealed class MatchRepositoryIntegrationTests : RepositoryIntegrationTestBase
{
    private const string CreatorUsername = "matchCreator";
    private const string CreatorEmail = "creator@example.com";
    private const string OpponentUsername = "matchOpponent";
    private const string OpponentEmail = "opponent@example.com";
    private const string RivalUsername = "matchRival";
    private const string RivalEmail = "rival@example.com";
    private const string WaitingStatusCode = "WAITING";
    private const string InProgressStatusCode = "IN_PROGRESS";

    private static LocalDbTestDatabase? _database;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _database = InitializeDatabase();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        CleanupDatabase(_database);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _database?.ResetMutableTables();
    }

    [TestMethod]
    public void CreateMatch_ValidSettings_PersistsWaitingMatchAndCreatorSeat()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        AuthenticatedSession creator = CreateSession(creatorId, CreatorUsername);
        MatchRepository repository = new MatchRepository();
        DateTime nowUtc = new DateTime(2026, 11, 2, 14, 0, 0, DateTimeKind.Utc);
        MatchStatus expectedStatus = MatchStatus.Success;
        int expectedParticipantCount = 1;

        MatchResult<Match> actualResult = repository.CreateMatch(
            creator,
            new MatchSettings(),
            nowUtc);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.IsNotNull(actualResult.Value);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        GameMatch actualPersistedMatch = verificationContext.GameMatch
            .Single(match => match.matchId == actualResult.Value.Id);
        int actualParticipantCount = verificationContext.MatchParticipant
            .Count(participant => participant.matchId == actualPersistedMatch.matchId);
        Assert.AreEqual(WaitingStatusCode, actualPersistedMatch.statusCode);
        Assert.AreEqual(expectedParticipantCount, actualParticipantCount);
    }

    [TestMethod]
    public void GetAvailableMatches_PublicWaitingMatch_ExcludesCreatorAndIncludesOtherPlayers()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        long opponentId = SeedAccount(OpponentUsername, OpponentEmail);
        MatchRepository repository = new MatchRepository();
        MatchResult<Match> created = repository.CreateMatch(
            CreateSession(creatorId, CreatorUsername),
            new MatchSettings(),
            DateTime.UtcNow);
        int expectedCreatorMatchCount = 0;
        int expectedOpponentMatchCount = 1;
        long? expectedMatchId = created.Value?.Id;

        MatchResult<Match[]> actualCreatorList = repository.GetAvailableMatches(creatorId);
        MatchResult<Match[]> actualOpponentList = repository.GetAvailableMatches(opponentId);

        Assert.AreEqual(expectedCreatorMatchCount, actualCreatorList.Value?.Length);
        Assert.AreEqual(expectedOpponentMatchCount, actualOpponentList.Value?.Length);
        Assert.AreEqual(expectedMatchId, actualOpponentList.Value?[0].Id);
    }

    [TestMethod]
    public void JoinMatch_ConcurrentRequests_AllowsOnlyOneSecondSeat()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        long opponentId = SeedAccount(OpponentUsername, OpponentEmail);
        long rivalId = SeedAccount(RivalUsername, RivalEmail);
        MatchRepository repository = new MatchRepository();
        DateTime nowUtc = DateTime.UtcNow;
        MatchResult<Match> created = repository.CreateMatch(
            CreateSession(creatorId, CreatorUsername),
            new MatchSettings(),
            nowUtc);
        string matchCode = created.Value?.Code ?? string.Empty;
        int expectedSuccessCount = 1;
        int expectedRejectionCount = 1;
        int expectedParticipantCount = 2;

        Task<MatchResult<Match>> firstJoinTask = Task.Run(() => repository.JoinMatch(
            CreateSession(opponentId, OpponentUsername),
            matchCode,
            nowUtc));
        Task<MatchResult<Match>> secondJoinTask = Task.Run(() => repository.JoinMatch(
            CreateSession(rivalId, RivalUsername),
            matchCode,
            nowUtc));
        Task.WaitAll(firstJoinTask, secondJoinTask);

        MatchResult<Match>[] results = new[] { firstJoinTask.Result, secondJoinTask.Result };
        int actualSuccessCount = results.Count(result => result.Status == MatchStatus.Success);
        int actualRejectionCount = results.Count(result => result.Status != MatchStatus.Success);
        Assert.AreEqual(expectedSuccessCount, actualSuccessCount);
        Assert.AreEqual(expectedRejectionCount, actualRejectionCount);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        long matchId = created.Value?.Id ?? 0L;
        int actualParticipantCount = verificationContext.MatchParticipant
            .Count(participant => participant.matchId == matchId);
        Assert.AreEqual(expectedParticipantCount, actualParticipantCount);
    }

    [TestMethod]
    public void StartSystemMatch_WaitingMatch_TransitionsToInProgressWithSystemOpponent()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        MatchRepository repository = new MatchRepository();
        DateTime nowUtc = DateTime.UtcNow;
        MatchResult<Match> created = repository.CreateMatch(
            CreateSession(creatorId, CreatorUsername),
            new MatchSettings(),
            nowUtc);
        long matchId = created.Value?.Id ?? 0L;
        MatchStatus expectedStatus = MatchStatus.Success;
        byte expectedCurrentTurnSeat = 1;

        MatchResult<Match> actualStarted = repository.StartSystemMatch(
            creatorId,
            matchId,
            nowUtc.AddSeconds(5));

        Assert.AreEqual(expectedStatus, actualStarted.Status);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        GameMatch actualPersisted = verificationContext.GameMatch
            .Single(match => match.matchId == matchId);
        Assert.AreEqual(InProgressStatusCode, actualPersisted.statusCode);
        Assert.IsFalse(actualPersisted.countsTowardStatistics);
        Assert.AreEqual(expectedCurrentTurnSeat, actualPersisted.currentTurnSeatNumber);
    }

    private static AuthenticatedSession CreateSession(long accountId, string username)
    {
        DateTime nowUtc = DateTime.UtcNow;
        return new AuthenticatedSession(
            "token-" + accountId,
            accountId,
            username,
            username + "@example.com",
            "en-US",
            nowUtc.AddHours(1));
    }
}
