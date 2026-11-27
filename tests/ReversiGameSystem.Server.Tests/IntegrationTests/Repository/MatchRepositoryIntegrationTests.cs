using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        MatchPlayer creator = CreatePlayer(creatorId, CreatorUsername);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
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
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        MatchResult<Match> created = repository.CreateMatch(
            CreatePlayer(creatorId, CreatorUsername),
            new MatchSettings(),
            DateTime.UtcNow);
        int expectedCreatorMatchCount = 0;
        int expectedOpponentMatchCount = 1;
        long? expectedMatchId = created.Value?.Id;

        MatchResult<Match[]> actualCreatorList = repository.GetAvailableMatches(CreatePlayer(creatorId, CreatorUsername));
        MatchResult<Match[]> actualOpponentList = repository.GetAvailableMatches(CreatePlayer(opponentId, OpponentUsername));

        Assert.AreEqual(expectedCreatorMatchCount, actualCreatorList.Value?.Length);
        Assert.AreEqual(expectedOpponentMatchCount, actualOpponentList.Value?.Length);
        Assert.AreEqual(expectedMatchId, actualOpponentList.Value?[0].Id);
    }

    [TestMethod]
    public void JoinMatch_TwoRegisteredPlayers_EnablesStatistics()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        long opponentId = SeedAccount(OpponentUsername, OpponentEmail);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        MatchResult<Match> created = repository.CreateMatch(
            CreatePlayer(creatorId, CreatorUsername), new MatchSettings(), DateTime.UtcNow);
        Assert.IsNotNull(created.Value);
        Assert.IsNotNull(created.Value.Code);

        MatchResult<Match> joined = repository.JoinMatch(
            CreatePlayer(opponentId, OpponentUsername), created.Value.Code, DateTime.UtcNow);

        Assert.AreEqual(MatchStatus.Success, joined.Status);
        Assert.IsNotNull(joined.Value);
        Assert.IsTrue(joined.Value.CountsTowardStatistics);
        using ReversiGameSystemEntities context = CreateFreshContext();
        Assert.IsTrue(context.GameMatch.Single(
            match => match.matchId == created.Value.Id).countsTowardStatistics);
    }

    [TestMethod]
    [DataRow("FINISHED", "REGISTERED", "LOSS", false, true)]
    [DataRow("FINISHED", "SYSTEM", "LOSS", true, false)]
    [DataRow("FINISHED", "GUEST", "LOSS", true, false)]
    [DataRow("FINISHED", "REGISTERED", "", true, false)]
    [DataRow("FINISHED", "REGISTERED", "WIN", true, false)]
    [DataRow("CANCELLED", "REGISTERED", "LOSS", true, false)]
    [DataRow("IN_PROGRESS", "REGISTERED", "LOSS", true, true)]
    [DataRow("IN_PROGRESS", "REGISTERED", "LOSS", false, false)]
    public void StatisticsMigration_HistoricalParticipants_PreservesEligibleResults(
        string status, string participantType, string opponentResult,
        bool originalEligibility, bool expectedEligibility)
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        long opponentId = SeedAccount(OpponentUsername, OpponentEmail);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        MatchResult<Match> created = repository.CreateMatch(
            CreatePlayer(creatorId, CreatorUsername), new MatchSettings(), DateTime.UtcNow);
        Assert.IsNotNull(created.Value);
        Assert.IsNotNull(created.Value.Code);
        repository.JoinMatch(CreatePlayer(opponentId, OpponentUsername),
            created.Value.Code, DateTime.UtcNow);
        using ReversiGameSystemEntities context = CreateFreshContext();
        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET statusCode = @p0, endedAtUtc = SYSUTCDATETIME(), "
            + "countsTowardStatistics = @p1 WHERE matchId = @p2; "
            + "UPDATE dbo.MatchParticipant SET finalPieceCount = 32, resultCode = 'WIN' "
            + "WHERE matchId = @p2; "
            + "UPDATE dbo.MatchParticipant SET participantTypeCode = @p3, "
            + "resultCode = NULLIF(@p4, ''), accountId = CASE "
            + "WHEN @p3 = 'REGISTERED' THEN accountId ELSE NULL END "
            + "WHERE matchId = @p2 AND seatNumber = 2;",
            status, originalEligibility, created.Value.Id, participantType, opponentResult);
        string migration = File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "migrations", "20261127_MatchStatisticsEligibility.sql"));

        context.Database.ExecuteSqlCommand(migration);
        context.Database.ExecuteSqlCommand(migration);

        using ReversiGameSystemEntities verification = CreateFreshContext();
        Assert.AreEqual(expectedEligibility, verification.GameMatch.Single(
            game => game.matchId == created.Value.Id).countsTowardStatistics);
        Assert.AreEqual(opponentResult, verification.MatchParticipant.Single(
            participant => participant.matchId == created.Value.Id && participant.seatNumber == 2)
            .resultCode ?? string.Empty);
    }

    [TestMethod]
    public void JoinMatch_ConcurrentRequests_AllowsOnlyOneSecondSeat()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        long opponentId = SeedAccount(OpponentUsername, OpponentEmail);
        long rivalId = SeedAccount(RivalUsername, RivalEmail);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        DateTime nowUtc = DateTime.UtcNow;
        MatchResult<Match> created = repository.CreateMatch(
            CreatePlayer(creatorId, CreatorUsername),
            new MatchSettings(),
            nowUtc);
        string matchCode = created.Value?.Code ?? string.Empty;
        int expectedSuccessCount = 1;
        int expectedRejectionCount = 1;
        int expectedParticipantCount = 2;

        Task<MatchResult<Match>> firstJoinTask = Task.Run(() => repository.JoinMatch(
            CreatePlayer(opponentId, OpponentUsername),
            matchCode,
            nowUtc));
        Task<MatchResult<Match>> secondJoinTask = Task.Run(() => repository.JoinMatch(
            CreatePlayer(rivalId, RivalUsername),
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
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        DateTime nowUtc = DateTime.UtcNow;
        MatchResult<Match> created = repository.CreateMatch(
            CreatePlayer(creatorId, CreatorUsername),
            new MatchSettings(),
            nowUtc);
        long matchId = created.Value?.Id ?? 0L;
        MatchStatus expectedStatus = MatchStatus.Success;
        byte expectedCurrentTurnSeat = 1;

        MatchResult<Match> actualStarted = repository.StartSystemMatch(
            CreatePlayer(creatorId, CreatorUsername),
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

    [TestMethod]
    public void SynchronizeMatch_SystemMove_PersistsSupportedSourceCode()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        Match match = CreateSystemMatch(repository, creatorId);
        PlayHumanTurn(match);
        GameplayMatchRules.ApplySystemMoveIfReady(match,
            match.TurnStartedAtUtc!.Value.Add(GameplayMatchRules.SystemMoveDelay));

        repository.SynchronizeMatch(match);

        using ReversiGameSystemEntities context = CreateFreshContext();
        string source = context.GameMove.Single(move => move.matchId == match.Id
            && move.seatNumber == ReversiEngine.White).moveSourceCode;
        Assert.AreEqual("SYSTEM", source);
    }

    [TestMethod]
    public void LoadMatch_SavedSystemMove_PreservesAutomaticOrigin()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        Match match = CreateSystemMatch(repository, creatorId);
        PlayHumanTurn(match);
        GameplayMatchRules.ApplySystemMoveIfReady(match,
            match.TurnStartedAtUtc!.Value.Add(GameplayMatchRules.SystemMoveDelay));
        repository.SynchronizeMatch(match);

        Match restored = repository.LoadMatch(match.Id).Value
            ?? throw new InvalidOperationException("The system match was not saved.");

        Assert.AreEqual("System", restored.Moves.Last().Origin);
    }

    [TestMethod]
    public void LoadMatchHistory_CompletedSystemMatch_IncludesFinishedGame()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        Match match = CreateSystemMatch(repository, creatorId);
        const int MaximumMoves = ReversiEngine.CellCount;
        for (int turn = 0; turn < MaximumMoves && match.State == "InProgress"; turn++)
        {
            if (match.CurrentTurnSeatNumber == ReversiEngine.Black)
            {
                PlayHumanTurn(match);
            }
            else
            {
                GameplayMatchRules.ApplySystemMoveIfReady(match,
                    match.TurnStartedAtUtc!.Value.Add(GameplayMatchRules.SystemMoveDelay));
            }
        }
        repository.SynchronizeMatch(match);

        Match[] history = repository.LoadMatchHistory(creatorId).Value
            ?? throw new InvalidOperationException("The history query failed.");

        Assert.AreEqual(match.Id, history.Single().Id);
    }

    [TestMethod]
    public void SynchronizeMatch_RejectedMove_LogsRetryWarning()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        PersistenceWarningLogger logger = new PersistenceWarningLogger();
        MatchRepository repository = new MatchRepository(logger);
        Match match = CreateSystemMatch(repository, creatorId);
        PlayHumanTurn(match);
        const int InvalidSeatNumber = 3;
        match.Moves[0].SeatNumber = InvalidSeatNumber;

        repository.SynchronizeMatch(match);

        Assert.AreEqual(1, logger.WarningCount);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CancelInterruptedMatches_UnfinishedMatch_CancelsWithoutResult(bool started)
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        MatchPlayer creator = CreatePlayer(creatorId, CreatorUsername);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        Match match = repository.CreateMatch(creator, new MatchSettings(), DateTime.UtcNow).Value!;
        if (started)
        {
            repository.StartSystemMatch(creator, match.Id, DateTime.UtcNow);
        }

        repository.CancelInterruptedMatches();

        using ReversiGameSystemEntities context = CreateFreshContext();
        GameMatch persisted = context.GameMatch.Single(candidate => candidate.matchId == match.Id);
        Assert.AreEqual("CANCELLED", persisted.statusCode);
        Assert.IsNotNull(persisted.endedAtUtc);
        Assert.IsNull(persisted.endReasonCode);
        Assert.IsNull(persisted.currentTurnSeatNumber);
        Assert.IsNull(persisted.turnStartedAtUtc);
        Assert.IsFalse(persisted.countsTowardStatistics);
        Assert.IsTrue(context.MatchParticipant.Where(participant => participant.matchId == match.Id)
            .All(participant => participant.resultCode == null));
    }

    [TestMethod]
    public void CancelInterruptedMatches_MatchWithMoves_PreservesRecordedMoves()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        Match match = CreateSystemMatch(repository, creatorId);
        PlayHumanTurn(match);
        repository.SynchronizeMatch(match);

        repository.CancelInterruptedMatches();

        using ReversiGameSystemEntities context = CreateFreshContext();
        Assert.AreEqual(1, context.GameMove.Count(move => move.matchId == match.Id));
    }

    [TestMethod]
    public void CancelInterruptedMatches_FinishedMatch_PreservesConclusion()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        Match match = CreateSystemMatch(repository, creatorId);
        GameplayMatchRules.ForfeitByParticipant(match, ReversiEngine.Black, DateTime.UtcNow);
        repository.SynchronizeMatch(match);

        repository.CancelInterruptedMatches();

        using ReversiGameSystemEntities context = CreateFreshContext();
        GameMatch persisted = context.GameMatch.Single(candidate => candidate.matchId == match.Id);
        Assert.AreEqual("FINISHED", persisted.statusCode);
        Assert.AreEqual("ABANDONMENT", persisted.endReasonCode);
    }

    [TestMethod]
    public void CancelInterruptedMatches_SecondExecution_DoesNotCancelAgain()
    {
        long creatorId = SeedAccount(CreatorUsername, CreatorEmail);
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        CreateSystemMatch(repository, creatorId);
        repository.CancelInterruptedMatches();

        int cancelledAgain = repository.CancelInterruptedMatches();

        Assert.AreEqual(0, cancelledAgain);
    }

    private static Match CreateSystemMatch(MatchRepository repository, long creatorId)
    {
        DateTime now = DateTime.UtcNow;
        Match created = repository.CreateMatch(CreatePlayer(creatorId, CreatorUsername),
            new MatchSettings(), now).Value
            ?? throw new InvalidOperationException("The waiting match could not be created.");
        return repository.StartSystemMatch(CreatePlayer(creatorId, CreatorUsername), created.Id, now).Value
            ?? throw new InvalidOperationException("The system match could not be started.");
    }

    private static void PlayHumanTurn(Match match)
    {
        for (int row = 0; row < ReversiEngine.BoardSize; row++)
        {
            for (int column = 0; column < ReversiEngine.BoardSize; column++)
            {
                if (ReversiEngine.IsLegalMove(match.Board, row, column, ReversiEngine.Black))
                {
                    MatchPlacement placement = new MatchPlacement(
                        match.Id, match.MatchStateVersion, row, column);
                    GameplayMatchRules.ApplyPlayerMove(match, placement,
                        match.TurnStartedAtUtc!.Value.AddSeconds(1));
                    return;
                }
            }
        }
        throw new InvalidOperationException("The human player has no legal move.");
    }

    private sealed class PersistenceWarningLogger : ILogger<MatchRepository>
    {
        internal int WarningCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return NullLogger.Instance.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Warning;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                WarningCount++;
            }
        }
    }

    private static MatchPlayer CreatePlayer(long accountId, string username)
    {
        DateTime nowUtc = DateTime.UtcNow;
        return new MatchPlayer(new AuthenticatedSession(
            "token-" + accountId,
            accountId,
            username,
            username + "@example.com",
            "en-US",
            nowUtc.AddHours(1)));
    }
}
