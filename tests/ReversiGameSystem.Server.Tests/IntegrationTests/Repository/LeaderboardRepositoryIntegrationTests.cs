using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Persistence.Leaderboards;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("Database")]
public sealed class LeaderboardRepositoryIntegrationTests : RepositoryIntegrationTestBase
{
    private const string AlphaUsername = "alphaBoard";
    private const string BetaUsername = "betaBoard";
    private const string InactiveUsername = "inactiveBoard";
    private const string SampleImageReference = "img/a.png";

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
    public void GetLeaderboardPlayers_ActiveAndDeactivatedAccounts_ExcludesDeactivated()
    {
        long expectedActiveId = SeedAccount(AlphaUsername, "alpha.lb@example.com");
        SeedAccount(InactiveUsername, "inactive.lb@example.com", "DEACTIVATED");
        LeaderboardRepository repository = new LeaderboardRepository();
        int expectedPlayerCount = 1;

        IReadOnlyList<LeaderboardPlayer> actualPlayers = repository.GetLeaderboardPlayers();

        Assert.AreEqual(expectedPlayerCount, actualPlayers.Count);
        Assert.AreEqual(expectedActiveId, actualPlayers[0].AccountId);
        Assert.AreEqual(AlphaUsername, actualPlayers[0].Username);
    }

    [TestMethod]
    public void GetLeaderboardPlayers_CountedAndUncountedMatches_AggregatesOnlyCountedFinished()
    {
        long alphaId = SeedAccountWithImage(
            AlphaUsername,
            "alpha2.lb@example.com",
            SampleImageReference);
        long betaId = SeedAccount(BetaUsername, "beta2.lb@example.com");
        SeedMatch(new MatchSeedData("LB0001", "FINISHED", true, alphaId, "WIN", betaId, "LOSS"));
        SeedMatch(new MatchSeedData("LB0002", "FINISHED", true, alphaId, "DRAW", betaId, "DRAW"));
        SeedMatch(new MatchSeedData("LB0003", "FINISHED", false, alphaId, "LOSS", betaId, "WIN"));
        SeedMatch(new MatchSeedData("LB0004", "IN_PROGRESS", true, alphaId, null, betaId, null));
        LeaderboardRepository repository = new LeaderboardRepository();
        int expectedMatchesPlayed = 2;
        int expectedAlphaWins = 1;
        int expectedBetaWins = 0;

        IReadOnlyList<LeaderboardPlayer> actualPlayers = repository.GetLeaderboardPlayers();
        LeaderboardPlayer actualAlpha = actualPlayers.Single(player => player.AccountId == alphaId);
        LeaderboardPlayer actualBeta = actualPlayers.Single(player => player.AccountId == betaId);

        Assert.AreEqual(expectedMatchesPlayed, actualAlpha.MatchesPlayed);
        Assert.AreEqual(expectedAlphaWins, actualAlpha.Wins);
        Assert.AreEqual(SampleImageReference, actualAlpha.ProfileImageUri);
        Assert.AreEqual(expectedMatchesPlayed, actualBeta.MatchesPlayed);
        Assert.AreEqual(expectedBetaWins, actualBeta.Wins);
    }

    private static void SeedMatch(MatchSeedData seed)
    {
        DateTime nowUtc = DateTime.UtcNow;
        using ReversiGameSystemEntities context = CreateFreshContext();
        GameMatch match = new GameMatch
        {
            matchCode = seed.MatchCode,
            statusCode = seed.StatusCode,
            turnDurationSeconds = 30,
            countsTowardStatistics = seed.CountsTowardStatistics,
            createdAtUtc = nowUtc,
            startedAtUtc = nowUtc,
            endedAtUtc = seed.StatusCode == "FINISHED" ? nowUtc.AddMinutes(5) : null,
            endReasonCode = seed.StatusCode == "FINISHED" ? "BOARD_FULL" : null
        };
        context.GameMatch.Add(match);
        context.SaveChanges();

        context.MatchParticipant.Add(new MatchParticipant
        {
            matchId = match.matchId,
            seatNumber = 1,
            accountId = seed.FirstAccountId,
            participantTypeCode = "REGISTERED",
            displayNameSnapshot = AlphaUsername,
            pieceColorCode = "BLACK",
            resultCode = seed.FirstResultCode,
            consecutiveTimeoutCount = 0,
            joinedAtUtc = nowUtc,
            isCreator = true
        });
        context.MatchParticipant.Add(new MatchParticipant
        {
            matchId = match.matchId,
            seatNumber = 2,
            accountId = seed.SecondAccountId,
            participantTypeCode = "REGISTERED",
            displayNameSnapshot = BetaUsername,
            pieceColorCode = "WHITE",
            resultCode = seed.SecondResultCode,
            consecutiveTimeoutCount = 0,
            joinedAtUtc = nowUtc,
            isCreator = false
        });
        context.SaveChanges();
    }

    private sealed class MatchSeedData
    {
        internal MatchSeedData(
            string matchCode,
            string statusCode,
            bool countsTowardStatistics,
            long firstAccountId,
            string? firstResultCode,
            long secondAccountId,
            string? secondResultCode)
        {
            MatchCode = matchCode;
            StatusCode = statusCode;
            CountsTowardStatistics = countsTowardStatistics;
            FirstAccountId = firstAccountId;
            FirstResultCode = firstResultCode;
            SecondAccountId = secondAccountId;
            SecondResultCode = secondResultCode;
        }

        internal string MatchCode { get; }

        internal string StatusCode { get; }

        internal bool CountsTowardStatistics { get; }

        internal long FirstAccountId { get; }

        internal string? FirstResultCode { get; }

        internal long SecondAccountId { get; }

        internal string? SecondResultCode { get; }
    }
}
