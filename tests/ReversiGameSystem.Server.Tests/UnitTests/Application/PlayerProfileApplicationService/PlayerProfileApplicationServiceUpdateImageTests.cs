using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PlayerProfileApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class PlayerProfileApplicationServiceUpdateImageTests
    : PlayerProfileApplicationServiceTestBase
{
    [TestMethod]
    public void UpdateImage_MissingSessionToken_ReturnsMissingSessionTokenWithoutRepositoryCall()
    {
        ProfileStatus expectedStatus = ProfileStatus.MissingSessionToken;
        int expectedSaveCount = 0;

        ProfileStatus actualStatus = Service.UpdateImage(" ", Images.StoredImage);
        int actualSaveCount = Images.SaveCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
    }

    [TestMethod]
    public void UpdateImage_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ProfileStatus expectedStatus = ProfileStatus.InvalidSession;
        int expectedSaveCount = 0;

        ProfileStatus actualStatus = Service.UpdateImage("invalid-token", Images.StoredImage);
        int actualSaveCount = Images.SaveCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
    }

    [TestMethod]
    public void UpdateImage_NullImage_ReturnsInvalidImageWithoutSaving()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        ProfileStatus expectedStatus = ProfileStatus.InvalidImage;
        int expectedSaveCount = 0;

        ProfileStatus actualStatus = Service.UpdateImage(token, null);
        int actualSaveCount = Images.SaveCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
    }

    [TestMethod]
    public void UpdateImage_InactiveCaller_ReturnsInactiveAccountWithoutSaving()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.ImageReferenceResult = new ProfileResult<string>(ProfileStatus.AccountNotFound);
        ProfileStatus expectedStatus = ProfileStatus.InactiveAccount;
        int expectedSaveCount = 0;

        ProfileStatus actualStatus = Service.UpdateImage(token, Images.StoredImage);
        int actualSaveCount = Images.SaveCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
    }

    [TestMethod]
    public void UpdateImage_SaveImageFails_ReturnsFailureWithoutUpdatingProfile()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        ProfileStatus expectedStatus = ProfileStatus.InvalidImage;
        Images.SaveResult = new ProfileResult<string>(expectedStatus);
        int expectedSetCalls = 0;

        ProfileStatus actualStatus = Service.UpdateImage(token, Images.StoredImage);
        int actualSetCalls = Profiles.SetProfileImageCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSetCalls, actualSetCalls);
    }

    [TestMethod]
    public void UpdateImage_SetProfileImageRejected_DeletesNewImageAndReturnsStatus()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        string expectedDeletedReference = "profiles/new.png";
        Images.SaveResult = new ProfileResult<string>(expectedDeletedReference);
        ProfileStatus expectedStatus = ProfileStatus.InactiveAccount;
        Profiles.SetProfileImageResult = expectedStatus;

        ProfileStatus actualStatus = Service.UpdateImage(token, Images.StoredImage);
        string actualDeletedReference = Images.DeletedReference;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedDeletedReference, actualDeletedReference);
    }

    [TestMethod]
    public void UpdateImage_SetProfileImageThrows_DeletesNewImageAndRethrows()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        string expectedDeletedReference = "profiles/new.png";
        Images.SaveResult = new ProfileResult<string>(expectedDeletedReference);
        Profiles.SetProfileImageFailure = new InvalidOperationException("Simulated failure.");
        Action action = () => Service.UpdateImage(token, Images.StoredImage);

        Assert.Throws<InvalidOperationException>(action);
        string actualDeletedReference = Images.DeletedReference;
        Assert.AreEqual(expectedDeletedReference, actualDeletedReference);
    }

    [TestMethod]
    public void UpdateImage_SetProfileImageAndCleanupThrow_ThrowsAggregateException()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Images.SaveResult = new ProfileResult<string>("profiles/new.png");
        Profiles.SetProfileImageFailure = new InvalidOperationException("Primary failure.");
        Images.CleanupFailure = new InvalidOperationException("Cleanup failure.");
        int expectedInnerCount = 2;
        Action action = () => Service.UpdateImage(token, Images.StoredImage);

        AggregateException exception = Assert.Throws<AggregateException>(action);
        int actualInnerCount = exception.InnerExceptions.Count;

        Assert.AreEqual(expectedInnerCount, actualInnerCount);
    }

    [TestMethod]
    public void UpdateImage_ValidReplacement_UpdatesProfileAndDeletesPreviousImage()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        string expectedPreviousReference = "profiles/old.png";
        string expectedNewReference = "profiles/new.png";
        Profiles.ImageReferenceResult = new ProfileResult<string>(expectedPreviousReference);
        Images.SaveResult = new ProfileResult<string>(expectedNewReference);
        ProfileStatus expectedStatus = ProfileStatus.Success;
        PlayerImageKind expectedKind = PlayerImageKind.Profile;

        ProfileStatus actualStatus = Service.UpdateImage(token, Images.StoredImage);
        string actualSavedReference = Profiles.LastImageReference;
        string actualDeletedReference = Images.DeletedReference;
        PlayerImageKind? actualKind = Images.LastSavedKind;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedNewReference, actualSavedReference);
        Assert.AreEqual(expectedPreviousReference, actualDeletedReference);
        Assert.AreEqual(expectedKind, actualKind);
    }

    [TestMethod]
    public void UpdateImage_NoPreviousImage_UpdatesProfileWithoutDeleting()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.ImageReferenceResult = new ProfileResult<string>(string.Empty);
        string expectedNewReference = "profiles/new.png";
        Images.SaveResult = new ProfileResult<string>(expectedNewReference);
        ProfileStatus expectedStatus = ProfileStatus.Success;
        string expectedDeletedReference = string.Empty;

        ProfileStatus actualStatus = Service.UpdateImage(token, Images.StoredImage);
        string actualDeletedReference = Images.DeletedReference;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedDeletedReference, actualDeletedReference);
    }
}
