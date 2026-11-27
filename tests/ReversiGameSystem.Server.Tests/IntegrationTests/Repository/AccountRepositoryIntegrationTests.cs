using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Model;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("Database")]
public sealed class AccountRepositoryIntegrationTests : RepositoryIntegrationTestBase
{
    private const string SampleUsername = "alphaPlayer";
    private const string SampleEmail = "alpha@example.com";
    private const string OtherUsername = "betaPlayer";
    private const string OtherEmail = "beta@example.com";
    private const string InitialPasswordHash = "HASH_INITIAL";
    private const string UpdatedPasswordHash = "HASH_UPDATED";
    private const string ActiveStatusCode = "ACTIVE";
    private const string DeactivatedStatusCode = "DEACTIVATED";
    private const string SpanishLocaleCode = "es-MX";
    private const string EnglishLocaleCode = "en-US";
    private const string StandardPieceStyleCode = "STANDARD";

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
    public void CreateAccount_ValidRegistration_PersistsAccountAndDefaultSettings()
    {
        DateTime fixedNowUtc = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        AccountRepository repository = new AccountRepository(new UtcClockStub(fixedNowUtc));
        AccountRegistrationData registration = new AccountRegistrationData(
            SampleUsername,
            SampleEmail,
            InitialPasswordHash,
            SpanishLocaleCode,
            null);
        AccountStatus expectedStatus = AccountStatus.Success;

        AccountResult<long> actualResult = repository.CreateAccount(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.IsTrue(actualResult.Value > 0);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        Account? persistedAccount = verificationContext.Account
            .SingleOrDefault(account => account.accountId == actualResult.Value);
        AccountSettings? persistedSettings = verificationContext.AccountSettings
            .SingleOrDefault(settings => settings.accountId == actualResult.Value);
        Assert.IsNotNull(persistedAccount);
        Assert.AreEqual(SampleUsername, persistedAccount.username);
        Assert.AreEqual(SampleEmail, persistedAccount.emailAddress);
        Assert.AreEqual(ActiveStatusCode, persistedAccount.statusCode);
        Assert.IsNotNull(persistedSettings);
        Assert.AreEqual(SpanishLocaleCode, persistedSettings.preferredLocaleCode);
        Assert.AreEqual(StandardPieceStyleCode, persistedSettings.pieceStyleCode);
    }

    [TestMethod]
    public void CreateAccount_DuplicateUsername_ReturnsUsernameUnavailable()
    {
        AccountRepository repository = new AccountRepository(new UtcClockStub());
        SeedAccount(SampleUsername, SampleEmail);
        AccountStatus expectedStatus = AccountStatus.UsernameUnavailable;
        int expectedAccountCount = 1;

        AccountResult<long> actualResult = repository.CreateAccount(
            new AccountRegistrationData(
                SampleUsername,
                OtherEmail,
                InitialPasswordHash,
                SpanishLocaleCode,
                null));

        Assert.AreEqual(expectedStatus, actualResult.Status);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        int actualAccountCount = verificationContext.Account.Count();
        Assert.AreEqual(expectedAccountCount, actualAccountCount);
    }

    [TestMethod]
    public void CreateAccount_DuplicateEmail_ReturnsEmailUnavailable()
    {
        AccountRepository repository = new AccountRepository(new UtcClockStub());
        SeedAccount(SampleUsername, SampleEmail);
        AccountStatus expectedStatus = AccountStatus.EmailUnavailable;
        int expectedAccountCount = 1;

        AccountResult<long> actualResult = repository.CreateAccount(
            new AccountRegistrationData(
                OtherUsername,
                SampleEmail,
                InitialPasswordHash,
                SpanishLocaleCode,
                null));

        Assert.AreEqual(expectedStatus, actualResult.Status);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        int actualAccountCount = verificationContext.Account.Count();
        Assert.AreEqual(expectedAccountCount, actualAccountCount);
    }

    [TestMethod]
    public void FindByLogin_UsernameOrEmail_ReturnsPersistedAccountFromDatabase()
    {
        AccountRepository repository = new AccountRepository(new UtcClockStub());
        long expectedAccountId = SeedAccount(SampleUsername, SampleEmail);
        SeedAccountSettings(expectedAccountId, EnglishLocaleCode);
        AccountStatus expectedStatus = AccountStatus.Success;

        AccountResult<LoginAccount> actualByUsername = repository.FindByLogin(SampleUsername);
        AccountResult<LoginAccount> actualByEmail = repository.FindByLogin(SampleEmail);

        Assert.AreEqual(expectedStatus, actualByUsername.Status);
        Assert.IsNotNull(actualByUsername.Value);
        Assert.AreEqual(expectedAccountId, actualByUsername.Value?.AccountId);
        Assert.AreEqual(EnglishLocaleCode, actualByUsername.Value?.PreferredLocaleCode);
        Assert.AreEqual(expectedStatus, actualByEmail.Status);
        Assert.IsNotNull(actualByEmail.Value);
        Assert.AreEqual(expectedAccountId, actualByEmail.Value?.AccountId);
    }

    [TestMethod]
    public void UpdatePasswordHash_ActiveAccount_PersistsNewHash()
    {
        AccountRepository repository = new AccountRepository(new UtcClockStub());
        long accountId = SeedAccount(SampleUsername, SampleEmail);
        AccountStatus expectedStatus = AccountStatus.Success;

        AccountStatus actualStatus = repository.UpdatePasswordHash(
            accountId,
            UpdatedPasswordHash);

        Assert.AreEqual(expectedStatus, actualStatus);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        string actualPasswordHash = verificationContext.Account
            .Single(account => account.accountId == accountId)
            .passwordHash;
        Assert.AreEqual(UpdatedPasswordHash, actualPasswordHash);
    }

    [TestMethod]
    public void DeactivateAccount_AccountInOpenMatch_ReturnsActiveMatchInProgress()
    {
        AccountRepository repository = new AccountRepository(new UtcClockStub());
        long accountId = SeedAccount(SampleUsername, SampleEmail);
        SeedOpenMatchForAccount(accountId);
        AccountStatus expectedStatus = AccountStatus.ActiveMatchInProgress;

        AccountStatus actualStatus = repository.DeactivateAccount(accountId);

        Assert.AreEqual(expectedStatus, actualStatus);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        string actualAccountStatus = verificationContext.Account
            .Single(account => account.accountId == accountId)
            .statusCode;
        Assert.AreEqual(ActiveStatusCode, actualAccountStatus);
    }

    [TestMethod]
    public void DeactivateAccount_AccountWithoutOpenMatch_UpdatesStatusToDeactivated()
    {
        AccountRepository repository = new AccountRepository(new UtcClockStub());
        long accountId = SeedAccount(SampleUsername, SampleEmail);
        AccountStatus expectedStatus = AccountStatus.Success;

        AccountStatus actualStatus = repository.DeactivateAccount(accountId);

        Assert.AreEqual(expectedStatus, actualStatus);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        string actualAccountStatus = verificationContext.Account
            .Single(account => account.accountId == accountId)
            .statusCode;
        Assert.AreEqual(DeactivatedStatusCode, actualAccountStatus);
    }

    private static void SeedOpenMatchForAccount(long accountId)
    {
        DateTime nowUtc = DateTime.UtcNow;
        using ReversiGameSystemEntities context = CreateFreshContext();
        GameMatch match = new GameMatch
        {
            matchCode = "OPEN001",
            statusCode = "WAITING",
            turnDurationSeconds = 30,
            countsTowardStatistics = true,
            createdAtUtc = nowUtc
        };
        context.GameMatch.Add(match);
        context.SaveChanges();

        MatchParticipant participant = new MatchParticipant
        {
            matchId = match.matchId,
            seatNumber = 1,
            accountId = accountId,
            participantTypeCode = "REGISTERED",
            displayNameSnapshot = SampleUsername,
            pieceColorCode = "BLACK",
            consecutiveTimeoutCount = 0,
            joinedAtUtc = nowUtc,
            isCreator = true
        };
        context.MatchParticipant.Add(participant);
        context.SaveChanges();
    }
}
