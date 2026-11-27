using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Persistence.Model;
using ReversiGameSystem.Server.Persistence.Players;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("Database")]
public sealed class PlayerProfileRepositoryIntegrationTests : RepositoryIntegrationTestBase
{
    private const string PrimaryUsername = "profileOne";
    private const string PrimaryEmail = "profile1@example.com";
    private const string SecondaryUsername = "profileTwo";
    private const string SecondaryEmail = "profile2@example.com";
    private const string UpdatedUsername = "profileUpdated";
    private const string SampleImageReference = "profile-images/avatar-1.png";

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
    public void GetProfile_FinishedCountedAndUncountedMatches_AggregatesOnlyCountedMatches()
    {
        long accountId = SeedAccountWithImage(
            PrimaryUsername,
            PrimaryEmail,
            SampleImageReference);
        long opponentId = SeedAccount(SecondaryUsername, SecondaryEmail);
        SeedFinishedMatch(new MatchSeedData("STAT01", true, accountId, "WIN", opponentId, "LOSS"));
        SeedFinishedMatch(new MatchSeedData("STAT02", true, accountId, "DRAW", opponentId, "DRAW"));
        SeedFinishedMatch(new MatchSeedData("CASL01", false, accountId, "LOSS", opponentId, "WIN"));
        PlayerProfileRepository repository = new PlayerProfileRepository();
        ProfileStatus expectedStatus = ProfileStatus.Success;
        int expectedMatchesPlayed = 2;
        int expectedWins = 1;
        int expectedLosses = 0;
        int expectedDraws = 1;

        ProfileResult<PlayerProfile> actualResult = repository.GetProfile(accountId);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.IsNotNull(actualResult.Value);
        Assert.AreEqual(PrimaryUsername, actualResult.Value.Username);
        Assert.AreEqual(SampleImageReference, actualResult.Value.ProfileImageUri);
        Assert.AreEqual(expectedMatchesPlayed, actualResult.Value.MatchesPlayed);
        Assert.AreEqual(expectedWins, actualResult.Value.Wins);
        Assert.AreEqual(expectedLosses, actualResult.Value.Losses);
        Assert.AreEqual(expectedDraws, actualResult.Value.Draws);
    }

    [TestMethod]
    public void ChangeUsername_DuplicateTarget_ReturnsUsernameUnavailable()
    {
        long firstAccountId = SeedAccount(PrimaryUsername, PrimaryEmail);
        SeedAccount(SecondaryUsername, SecondaryEmail);
        PlayerProfileRepository repository = new PlayerProfileRepository();
        ProfileStatus expectedStatus = ProfileStatus.UsernameUnavailable;

        ProfileStatus actualStatus = repository.ChangeUsername(firstAccountId, SecondaryUsername);

        Assert.AreEqual(expectedStatus, actualStatus);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        string actualUsername = verificationContext.Account
            .Single(account => account.accountId == firstAccountId)
            .username;
        Assert.AreEqual(PrimaryUsername, actualUsername);
    }

    [TestMethod]
    public void ChangeUsername_UniqueTarget_UpdatesUsernameInDatabase()
    {
        long accountId = SeedAccount(PrimaryUsername, PrimaryEmail);
        PlayerProfileRepository repository = new PlayerProfileRepository();
        ProfileStatus expectedStatus = ProfileStatus.Success;

        ProfileStatus actualStatus = repository.ChangeUsername(accountId, UpdatedUsername);

        Assert.AreEqual(expectedStatus, actualStatus);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        string actualUsername = verificationContext.Account
            .Single(account => account.accountId == accountId)
            .username;
        Assert.AreEqual(UpdatedUsername, actualUsername);
    }

    [TestMethod]
    public void SetProfileImage_ActiveAccount_PersistsReferenceReadableInFreshContext()
    {
        long accountId = SeedAccount(PrimaryUsername, PrimaryEmail);
        PlayerProfileRepository repository = new PlayerProfileRepository();
        ProfileStatus expectedStatus = ProfileStatus.Success;

        ProfileStatus actualStatus = repository.SetProfileImage(accountId, SampleImageReference);
        ProfileResult<string> actualReference = repository.GetProfileImageReference(accountId);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedStatus, actualReference.Status);
        Assert.AreEqual(SampleImageReference, actualReference.Value);
    }

    private static void SeedFinishedMatch(MatchSeedData seed)
    {
        DateTime nowUtc = DateTime.UtcNow;
        using ReversiGameSystemEntities context = CreateFreshContext();
        GameMatch match = new GameMatch
        {
            matchCode = seed.MatchCode,
            statusCode = "FINISHED",
            turnDurationSeconds = 30,
            countsTowardStatistics = seed.CountsTowardStatistics,
            createdAtUtc = nowUtc,
            startedAtUtc = nowUtc,
            endedAtUtc = nowUtc.AddMinutes(5),
            endReasonCode = "BOARD_FULL"
        };
        context.GameMatch.Add(match);
        context.SaveChanges();

        context.MatchParticipant.Add(new MatchParticipant
        {
            matchId = match.matchId,
            seatNumber = 1,
            accountId = seed.FirstAccountId,
            participantTypeCode = "REGISTERED",
            displayNameSnapshot = PrimaryUsername,
            pieceColorCode = "BLACK",
            resultCode = seed.FirstResultCode,
            finalPieceCount = 34,
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
            displayNameSnapshot = SecondaryUsername,
            pieceColorCode = "WHITE",
            resultCode = seed.SecondResultCode,
            finalPieceCount = 30,
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
            bool countsTowardStatistics,
            long firstAccountId,
            string firstResultCode,
            long secondAccountId,
            string secondResultCode)
        {
            MatchCode = matchCode;
            CountsTowardStatistics = countsTowardStatistics;
            FirstAccountId = firstAccountId;
            FirstResultCode = firstResultCode;
            SecondAccountId = secondAccountId;
            SecondResultCode = secondResultCode;
        }

        internal string MatchCode { get; }

        internal bool CountsTowardStatistics { get; }

        internal long FirstAccountId { get; }

        internal string FirstResultCode { get; }

        internal long SecondAccountId { get; }

        internal string SecondResultCode { get; }
    }
}
