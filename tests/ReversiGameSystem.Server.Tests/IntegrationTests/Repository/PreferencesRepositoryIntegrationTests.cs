using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Persistence.Model;
using ReversiGameSystem.Server.Persistence.Preferences;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("Database")]
public sealed class PreferencesRepositoryIntegrationTests : RepositoryIntegrationTestBase
{
    private const string SampleUsername = "prefPlayer";
    private const string SampleEmail = "pref@example.com";
    private const string DefaultLocaleCode = "en-US";
    private const string SpanishLocaleCode = "es-MX";
    private const string DefaultPieceStyle = "STANDARD";
    private const string SampleTimeZone = "America/Mexico_City";
    private const string SamplePieceStyle = "MARBLE";
    private const string SampleBackgroundReference = "board-backgrounds/wood.png";

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
    public void ReadPreferences_ActiveAccountWithoutSettingsRow_ReturnsDatabaseDefaults()
    {
        long accountId = SeedAccount(SampleUsername, SampleEmail);
        PreferencesRepository repository = new PreferencesRepository();
        PreferencesStatus expectedStatus = PreferencesStatus.Success;
        string expectedBackgroundReference = string.Empty;

        PreferencesAccess actualAccess = repository.ReadPreferences(accountId);

        Assert.AreEqual(expectedStatus, actualAccess.Status);
        Assert.IsNotNull(actualAccess.Preferences);
        Assert.AreEqual(DefaultLocaleCode, actualAccess.Preferences.PreferredLocaleCode);
        Assert.AreEqual(DefaultPieceStyle, actualAccess.Preferences.PieceStyleCode);
        Assert.IsFalse(actualAccess.Preferences.UseCustomBoardBackground);
        Assert.AreEqual(
            expectedBackgroundReference,
            actualAccess.Preferences.BoardBackgroundReference);
    }

    [TestMethod]
    public void SavePreferences_ActiveAccount_CreatesRowAndPersistsValues()
    {
        long accountId = SeedAccount(SampleUsername, SampleEmail);
        PreferencesRepository repository = new PreferencesRepository();
        PreferencesStatus expectedStatus = PreferencesStatus.Success;
        bool expectedCustomBackground = true;
        PlayerPreferences expectedPreferences = new PlayerPreferences
        {
            PreferredLocaleCode = SpanishLocaleCode,
            TimeZoneId = SampleTimeZone,
            PieceStyleCode = SamplePieceStyle,
            UseCustomBoardBackground = expectedCustomBackground
        };

        PreferencesStatus actualStatus = repository.SavePreferences(accountId, expectedPreferences);
        PreferencesAccess actualReadBack = repository.ReadPreferences(accountId);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedStatus, actualReadBack.Status);
        Assert.IsNotNull(actualReadBack.Preferences);
        Assert.AreEqual(SpanishLocaleCode, actualReadBack.Preferences?.PreferredLocaleCode);
        Assert.AreEqual(SampleTimeZone, actualReadBack.Preferences?.TimeZoneId);
        Assert.AreEqual(SamplePieceStyle, actualReadBack.Preferences?.PieceStyleCode);
        Assert.AreEqual(
            expectedCustomBackground,
            actualReadBack.Preferences?.UseCustomBoardBackground);
    }

    [TestMethod]
    public void SetBoardBackground_ActiveAccount_PersistsBackgroundAndEnablesCustomFlag()
    {
        long accountId = SeedAccount(SampleUsername, SampleEmail);
        PreferencesRepository repository = new PreferencesRepository();
        PreferencesStatus expectedStatus = PreferencesStatus.Success;
        bool expectedCustomBackground = true;

        PreferencesStatus actualStatus = repository.SetBoardBackground(
            accountId,
            SampleBackgroundReference);
        PreferencesAccess actualReadBack = repository.ReadPreferences(accountId);

        Assert.AreEqual(expectedStatus, actualStatus);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        string actualBackgroundUri = verificationContext.AccountSettings
            .Single(settings => settings.accountId == accountId)
            .boardBackgroundUri;
        Assert.AreEqual(SampleBackgroundReference, actualBackgroundUri);
        Assert.AreEqual(expectedStatus, actualReadBack.Status);
        Assert.AreEqual(
            expectedCustomBackground,
            actualReadBack.Preferences?.UseCustomBoardBackground);
    }

    [TestMethod]
    public void SavePreferences_DeactivatedAccount_ReturnsInactiveAccount()
    {
        long accountId = SeedAccount(SampleUsername, SampleEmail, "DEACTIVATED");
        PreferencesRepository repository = new PreferencesRepository();
        PreferencesStatus expectedStatus = PreferencesStatus.InactiveAccount;

        PreferencesStatus actualStatus = repository.SavePreferences(
            accountId,
            new PlayerPreferences
            {
                PreferredLocaleCode = SpanishLocaleCode,
                PieceStyleCode = SamplePieceStyle
            });

        Assert.AreEqual(expectedStatus, actualStatus);
    }
}
