using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Preferences;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PreferencesApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class UpdatePreferencesTests : PreferencesApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void UpdatePreferences_MissingToken_ReturnsMissingSessionTokenWithoutAccess(
        string token)
    {
        PlayerPreferences preferences = CreatePreferences();
        PreferencesStatus expectedStatus = PreferencesStatus.MissingSessionToken;
        const int expectedReadCount = 0;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus = Service.UpdatePreferences(token, preferences);
        int actualReadCount = Repository.ReadCount;
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedReadCount, actualReadCount);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    public void UpdatePreferences_RevokedSession_ReturnsInvalidSessionWithoutAccess()
    {
        Sessions.RevokeSession(Session.SessionToken);
        PlayerPreferences preferences = CreatePreferences();
        PreferencesStatus expectedStatus = PreferencesStatus.InvalidSession;
        const int expectedReadCount = 0;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus =
            Service.UpdatePreferences(Session.SessionToken, preferences);
        int actualReadCount = Repository.ReadCount;
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedReadCount, actualReadCount);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    public void UpdatePreferences_InactiveAccount_ReturnsInactiveAccountWithoutWrite()
    {
        Repository.IsActiveAccount = false;
        PlayerPreferences preferences = CreatePreferences();
        PreferencesStatus expectedStatus = PreferencesStatus.InactiveAccount;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus =
            Service.UpdatePreferences(Session.SessionToken, preferences);
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    public void UpdatePreferences_NullPreferences_ReturnsMissingPreferencesWithoutWrite()
    {
        PreferencesStatus expectedStatus = PreferencesStatus.MissingPreferences;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus = Service.UpdatePreferences(Session.SessionToken, null);
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    public void UpdatePreferences_UnsupportedLocale_ReturnsInvalidLocaleWithoutWrite()
    {
        PlayerPreferences preferences = CreatePreferences(localeCode: "fr-FR");
        PreferencesStatus expectedStatus = PreferencesStatus.InvalidLocale;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus =
            Service.UpdatePreferences(Session.SessionToken, preferences);
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    public void UpdatePreferences_UnsupportedPieceStyle_ReturnsInvalidPieceStyleWithoutWrite()
    {
        PlayerPreferences preferences = CreatePreferences(pieceStyleCode: "UNKNOWN");
        PreferencesStatus expectedStatus = PreferencesStatus.InvalidPieceStyle;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus =
            Service.UpdatePreferences(Session.SessionToken, preferences);
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("NotAnExistingTimeZone")]
    public void UpdatePreferences_InvalidTimeZone_ReturnsInvalidTimeZoneWithoutWrite(
        string timeZoneId)
    {
        PlayerPreferences preferences = CreatePreferences(timeZoneId: timeZoneId);
        PreferencesStatus expectedStatus = PreferencesStatus.InvalidTimeZone;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus =
            Service.UpdatePreferences(Session.SessionToken, preferences);
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    [DataRow("en-US", "STANDARD", null)]
    [DataRow("es-MX", "PROFILE_PICTURES", "UTC")]
    public void UpdatePreferences_ValidInput_PersistsPreferencesAndReturnsSuccess(
        string localeCode,
        string pieceStyleCode,
        string? timeZoneId)
    {
        PlayerPreferences preferences = CreatePreferences(localeCode, pieceStyleCode, timeZoneId);
        PreferencesStatus expectedStatus = PreferencesStatus.Success;

        PreferencesStatus actualStatus =
            Service.UpdatePreferences(Session.SessionToken, preferences);
        string actualLocaleCode = Repository.StoredPreferences.PreferredLocaleCode;
        string actualPieceStyleCode = Repository.StoredPreferences.PieceStyleCode;
        string? actualTimeZoneId = Repository.StoredPreferences.TimeZoneId;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(localeCode, actualLocaleCode);
        Assert.AreEqual(pieceStyleCode, actualPieceStyleCode);
        Assert.AreEqual(timeZoneId, actualTimeZoneId);
    }
}
