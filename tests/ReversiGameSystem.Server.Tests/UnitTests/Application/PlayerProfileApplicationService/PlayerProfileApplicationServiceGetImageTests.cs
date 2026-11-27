using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PlayerProfileApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class PlayerProfileApplicationServiceGetImageTests
    : PlayerProfileApplicationServiceTestBase
{
    [TestMethod]
    public void GetImage_MissingSessionToken_ReturnsMissingSessionTokenWithoutRepositoryCall()
    {
        ProfileStatus expectedStatus = ProfileStatus.MissingSessionToken;
        int expectedCallCount = 0;

        GetProfileImageResult actualResult = Service.GetImage(" ", null);
        ProfileStatus actualStatus = actualResult.Status;
        int actualCallCount = Profiles.GetImageReferenceCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetImage_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ProfileStatus expectedStatus = ProfileStatus.InvalidSession;
        int expectedCallCount = 0;

        GetProfileImageResult actualResult = Service.GetImage("invalid-token", null);
        ProfileStatus actualStatus = actualResult.Status;
        int actualCallCount = Profiles.GetImageReferenceCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-2L)]
    public void GetImage_NonPositiveTargetAccountId_ReturnsInvalidAccountIdWithoutRepositoryCall(
        long targetAccountId)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        ProfileStatus expectedStatus = ProfileStatus.InvalidAccountId;
        int expectedCallCount = 0;

        GetProfileImageResult actualResult = Service.GetImage(token, targetAccountId);
        ProfileStatus actualStatus = actualResult.Status;
        int actualCallCount = Profiles.GetImageReferenceCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetImage_OwnReferenceUnavailable_ReturnsInactiveAccountWithoutReadingImage()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.ImageReferenceResult = new ProfileResult<string>(ProfileStatus.AccountNotFound);
        ProfileStatus expectedStatus = ProfileStatus.InactiveAccount;
        int expectedReadCount = 0;

        GetProfileImageResult actualResult = Service.GetImage(token, null);
        ProfileStatus actualStatus = actualResult.Status;
        int actualReadCount = Images.ReadCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedReadCount, actualReadCount);
    }

    [TestMethod]
    public void GetImage_OtherAccountWithInactiveCaller_ReturnsInactiveAccountWithoutReadingImage()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.IsActiveResult = false;
        ProfileStatus expectedStatus = ProfileStatus.InactiveAccount;
        int expectedReadCount = 0;

        GetProfileImageResult actualResult = Service.GetImage(token, 20L);
        ProfileStatus actualStatus = actualResult.Status;
        int actualReadCount = Images.ReadCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedReadCount, actualReadCount);
    }

    [TestMethod]
    public void GetImage_EmptyImageReference_ReturnsSuccessWithNullImageWithoutReadingFile()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.ImageReferenceResult = new ProfileResult<string>(string.Empty);
        ProfileStatus expectedStatus = ProfileStatus.Success;
        int expectedReadCount = 0;

        GetProfileImageResult actualResult = Service.GetImage(token, null);
        ProfileStatus actualStatus = actualResult.Status;
        ProfileImageData? actualImage = actualResult.Image;
        int actualReadCount = Images.ReadCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNull(actualImage);
        Assert.AreEqual(expectedReadCount, actualReadCount);
    }

    [TestMethod]
    public void GetImage_ValidReference_ReadsAndReturnsImage()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        string expectedReference = "profiles/avatar.png";
        Profiles.ImageReferenceResult = new ProfileResult<string>(expectedReference);
        ProfileStatus expectedStatus = ProfileStatus.Success;
        ProfileImageData expectedImage = Images.StoredImage;

        GetProfileImageResult actualResult = Service.GetImage(token, null);
        ProfileStatus actualStatus = actualResult.Status;
        ProfileImageData? actualImage = actualResult.Image;
        string actualReadReference = Images.LastReadReference;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedImage, actualImage);
        Assert.AreEqual(expectedReference, actualReadReference);
    }
}
