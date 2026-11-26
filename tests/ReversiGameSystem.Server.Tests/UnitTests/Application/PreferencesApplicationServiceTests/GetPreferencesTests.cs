using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Preferences;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PreferencesApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class GetPreferencesTests : PreferencesApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void GetPreferences_MissingToken_ReturnsMissingSessionTokenWithoutRead(string token)
    {
        PreferencesStatus expectedStatus = PreferencesStatus.MissingSessionToken;
        const int expectedReadCount = 0;

        PreferencesAccess access = Service.GetPreferences(token);
        PreferencesStatus actualStatus = access.Status;
        int actualReadCount = Repository.ReadCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedReadCount, actualReadCount);
    }

    [TestMethod]
    public void GetPreferences_RevokedSession_ReturnsInvalidSessionWithoutRead()
    {
        Sessions.RevokeSession(Session.SessionToken);
        PreferencesStatus expectedStatus = PreferencesStatus.InvalidSession;
        const int expectedReadCount = 0;

        PreferencesAccess access = Service.GetPreferences(Session.SessionToken);
        PreferencesStatus actualStatus = access.Status;
        int actualReadCount = Repository.ReadCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedReadCount, actualReadCount);
    }

    [TestMethod]
    public void GetPreferences_InactiveAccount_ReturnsInactiveAccount()
    {
        Repository.IsActiveAccount = false;
        PreferencesStatus expectedStatus = PreferencesStatus.InactiveAccount;

        PreferencesAccess access = Service.GetPreferences(Session.SessionToken);
        PreferencesStatus actualStatus = access.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void GetPreferences_ValidSession_ReturnsStoredPreferencesForCaller()
    {
        PreferencesStatus expectedStatus = PreferencesStatus.Success;
        PlayerPreferences expectedPreferences = Repository.StoredPreferences;
        long expectedAccountId = Session.AccountId;

        PreferencesAccess access = Service.GetPreferences(Session.SessionToken);
        PreferencesStatus actualStatus = access.Status;
        PlayerPreferences actualPreferences = access.Preferences;
        long actualAccountId = Repository.LastAccountId;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedPreferences, actualPreferences);
        Assert.AreEqual(expectedAccountId, actualAccountId);
    }
}
