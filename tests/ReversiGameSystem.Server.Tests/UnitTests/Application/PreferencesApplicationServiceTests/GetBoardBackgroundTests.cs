using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Preferences;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PreferencesApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class GetBoardBackgroundTests : PreferencesApplicationServiceTestBase
{
    [TestMethod]
    public void GetBoardBackground_InvalidSession_ReturnsInvalidSessionWithoutImageRead()
    {
        Sessions.RevokeSession(Session.SessionToken);
        PreferencesStatus expectedStatus = PreferencesStatus.InvalidSession;
        const int expectedImageReadCount = 0;

        PreferencesAccess access = Service.GetBoardBackground(Session.SessionToken);
        PreferencesStatus actualStatus = access.Status;
        int actualImageReadCount = Images.ReadCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedImageReadCount, actualImageReadCount);
    }

    [TestMethod]
    public void GetBoardBackground_NoBackgroundReference_ReturnsNullImageWithoutImageRead()
    {
        Repository.StoredPreferences.BoardBackgroundReference = string.Empty;
        PreferencesStatus expectedStatus = PreferencesStatus.Success;
        const int expectedImageReadCount = 0;

        PreferencesAccess access = Service.GetBoardBackground(Session.SessionToken);
        PreferencesStatus actualStatus = access.Status;
        ProfileImageData? actualImage = access.BackgroundImage;
        int actualImageReadCount = Images.ReadCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNull(actualImage);
        Assert.AreEqual(expectedImageReadCount, actualImageReadCount);
    }

    [TestMethod]
    public void GetBoardBackground_DisabledBackgroundWithReference_ReturnsStoredImage()
    {
        const string expectedReference = "board-backgrounds/custom.png";
        Repository.StoredPreferences.UseCustomBoardBackground = false;
        Repository.StoredPreferences.BoardBackgroundReference = expectedReference;
        PreferencesStatus expectedStatus = PreferencesStatus.Success;
        ProfileImageData expectedImage = Images.StoredImage;

        PreferencesAccess access = Service.GetBoardBackground(Session.SessionToken);
        PreferencesStatus actualStatus = access.Status;
        ProfileImageData? actualImage = access.BackgroundImage;
        string actualReference = Images.LastReadReference;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedImage, actualImage);
        Assert.AreEqual(expectedReference, actualReference);
    }
}
