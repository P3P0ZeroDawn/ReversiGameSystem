using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PreferencesApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ChangeBoardBackgroundTests : PreferencesApplicationServiceTestBase
{
    [TestMethod]
    public void ChangeBoardBackground_MissingToken_ReturnsMissingSessionTokenWithoutSave()
    {
        ProfileImageData image = CreateValidPngImage();
        PreferencesStatus expectedStatus = PreferencesStatus.MissingSessionToken;
        const int expectedSaveCount = 0;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus = Service.ChangeBoardBackground(string.Empty, image);
        int actualSaveCount = Images.SaveCount;
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    public void ChangeBoardBackground_NullImage_ReturnsInvalidImageWithoutSave()
    {
        PreferencesStatus expectedStatus = PreferencesStatus.InvalidImage;
        const int expectedSaveCount = 0;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus = Service.ChangeBoardBackground(Session.SessionToken, null);
        int actualSaveCount = Images.SaveCount;
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    public void ChangeBoardBackground_InvalidImageBytes_ReturnsInvalidImageWithoutSave()
    {
        ProfileImageData image = new ProfileImageData(
            Array.Empty<byte>(),
            PlayerImagePolicy.PngMediaType);
        PreferencesStatus expectedStatus = PreferencesStatus.InvalidImage;
        const int expectedSaveCount = 0;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus = Service.ChangeBoardBackground(Session.SessionToken, image);
        int actualSaveCount = Images.SaveCount;
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    public void ChangeBoardBackground_InactiveAccount_ReturnsInactiveAccountWithoutSave()
    {
        Repository.IsActiveAccount = false;
        ProfileImageData image = CreateValidPngImage();
        PreferencesStatus expectedStatus = PreferencesStatus.InactiveAccount;
        const int expectedSaveCount = 0;

        PreferencesStatus actualStatus = Service.ChangeBoardBackground(Session.SessionToken, image);
        int actualSaveCount = Images.SaveCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
    }

    [TestMethod]
    public void ChangeBoardBackground_SaveImageRejected_ReturnsInvalidImageWithoutWrite()
    {
        Images.SaveResult = new ProfileResult<string>(ProfileStatus.InvalidImage);
        ProfileImageData image = CreateValidPngImage();
        PreferencesStatus expectedStatus = PreferencesStatus.InvalidImage;
        const int expectedWriteCount = 0;

        PreferencesStatus actualStatus = Service.ChangeBoardBackground(Session.SessionToken, image);
        int actualWriteCount = Repository.WriteCount;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedWriteCount, actualWriteCount);
    }

    [TestMethod]
    public void ChangeBoardBackground_AssociationReturnsFailure_DeletesImageAndReturnsStatus()
    {
        Repository.SetBoardBackgroundStatus = PreferencesStatus.InactiveAccount;
        ProfileImageData image = CreateValidPngImage();
        PreferencesStatus expectedStatus = PreferencesStatus.InactiveAccount;
        const string expectedDeletedReference =
            TestDoubles.PlayerImageRepositoryStub.DefaultImageReference;

        PreferencesStatus actualStatus = Service.ChangeBoardBackground(Session.SessionToken, image);
        string actualDeletedReference = Images.DeletedReference;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedDeletedReference, actualDeletedReference);
    }

    [TestMethod]
    public void ChangeBoardBackground_AssociationFails_DeletesImageAndPreservesException()
    {
        IOException expectedFailure = new IOException("The association failed.");
        Repository.AssociationFailure = expectedFailure;
        ProfileImageData image = CreateValidPngImage();
        const string expectedDeletedReference =
            TestDoubles.PlayerImageRepositoryStub.DefaultImageReference;
        Action action = () => Service.ChangeBoardBackground(Session.SessionToken, image);

        IOException actualFailure = Assert.Throws<IOException>(action);
        string actualDeletedReference = Images.DeletedReference;

        Assert.AreSame(expectedFailure, actualFailure);
        Assert.AreEqual(expectedDeletedReference, actualDeletedReference);
    }

    [TestMethod]
    public void ChangeBoardBackground_CleanupAlsoFails_PreservesBothExceptions()
    {
        IOException expectedAssociationFailure = new IOException("The association failed.");
        UnauthorizedAccessException expectedCleanupFailure =
            new UnauthorizedAccessException("The cleanup failed.");
        Repository.AssociationFailure = expectedAssociationFailure;
        Images.CleanupFailure = expectedCleanupFailure;
        ProfileImageData image = CreateValidPngImage();
        Action action = () => Service.ChangeBoardBackground(Session.SessionToken, image);

        AggregateException reported = Assert.Throws<AggregateException>(action);
        Exception actualFirstException = reported.InnerExceptions[0];
        Exception actualSecondException = reported.InnerExceptions[1];

        Assert.AreSame(expectedAssociationFailure, actualFirstException);
        Assert.AreSame(expectedCleanupFailure, actualSecondException);
    }

    [TestMethod]
    public void ChangeBoardBackground_ValidReplacement_SavesDeletesPreviousAndReturnsSuccess()
    {
        const string expectedPreviousReference = "board-backgrounds/previous.png";
        Repository.StoredPreferences.BoardBackgroundReference = expectedPreviousReference;
        ProfileImageData image = CreateValidPngImage();
        PreferencesStatus expectedStatus = PreferencesStatus.Success;
        PlayerImageKind expectedKind = PlayerImageKind.BoardBackground;
        const string expectedStoredReference =
            TestDoubles.PlayerImageRepositoryStub.DefaultImageReference;

        PreferencesStatus actualStatus = Service.ChangeBoardBackground(Session.SessionToken, image);
        PlayerImageKind? actualKind = Images.LastSavedKind;
        string actualDeletedReference = Images.DeletedReference;
        string actualStoredReference = Repository.StoredPreferences.BoardBackgroundReference;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedKind, actualKind);
        Assert.AreEqual(expectedPreviousReference, actualDeletedReference);
        Assert.AreEqual(expectedStoredReference, actualStoredReference);
    }

    [TestMethod]
    public void ChangeBoardBackground_SameReferenceReturned_DoesNotDeleteCurrentImage()
    {
        Repository.StoredPreferences.BoardBackgroundReference =
            TestDoubles.PlayerImageRepositoryStub.DefaultImageReference;
        ProfileImageData image = CreateValidPngImage();
        PreferencesStatus expectedStatus = PreferencesStatus.Success;
        string expectedDeletedReference = string.Empty;

        PreferencesStatus actualStatus = Service.ChangeBoardBackground(Session.SessionToken, image);
        string actualDeletedReference = Images.DeletedReference;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedDeletedReference, actualDeletedReference);
    }
}
