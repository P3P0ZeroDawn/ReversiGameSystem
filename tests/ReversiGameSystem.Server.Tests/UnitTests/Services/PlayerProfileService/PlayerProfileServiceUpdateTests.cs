using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.PlayerProfileService;

[TestClass]
[TestCategory("Unit")]
public sealed class PlayerProfileServiceUpdateTests : PlayerProfileServiceTestBase
{
    [TestMethod]
    [DataRow(null, SessionFailure.MissingSessionToken)]
    [DataRow("invalid-token", SessionFailure.InvalidSession)]
    public void ChangeUsername_RejectedSession_ThrowsSessionFault(
        string? sessionToken,
        SessionFailure expectedFailure)
    {
        int expectedCallCount = 0;
        Action action = () => Service.ChangeUsername(sessionToken!, "ValidName");

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;
        int actualCallCount = Profiles.ChangeUsernameCalls;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void ChangeUsername_InvalidUsername_ThrowsProfileFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        ProfileFailure expectedFailure = ProfileFailure.InvalidUsername;
        int expectedCallCount = 0;
        Action action = () => Service.ChangeUsername(token, "ab");

        FaultException<ProfileFault> exception =
            Assert.Throws<FaultException<ProfileFault>>(action);
        ProfileFailure actualFailure = exception.Detail.Reason;
        int actualCallCount = Profiles.ChangeUsernameCalls;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void ChangeUsername_UsernameUnavailable_ThrowsProfileFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.ChangeUsernameResult = ProfileStatus.UsernameUnavailable;
        ProfileFailure expectedFailure = ProfileFailure.UsernameUnavailable;
        Action action = () => Service.ChangeUsername(token, "TakenName");

        FaultException<ProfileFault> exception =
            Assert.Throws<FaultException<ProfileFault>>(action);
        ProfileFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void ChangeUsername_ValidUsername_UpdatesRepository()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        string expectedUsername = "UpdatedName";
        int expectedCallCount = 1;

        Service.ChangeUsername(token, expectedUsername);
        string actualUsername = Profiles.LastUsername;
        int actualCallCount = Profiles.ChangeUsernameCalls;

        Assert.AreEqual(expectedUsername, actualUsername);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(null, SessionFailure.MissingSessionToken)]
    [DataRow("invalid-token", SessionFailure.InvalidSession)]
    public void UpdateProfileImage_RejectedSession_ThrowsSessionFault(
        string? sessionToken,
        SessionFailure expectedFailure)
    {
        ImageContent content = new ImageContent
        {
            MediaType = Images.StoredImage.MediaType,
            Content = Images.StoredImage.Content
        };
        int expectedSaveCount = 0;
        Action action = () => Service.UpdateProfileImage(sessionToken!, content);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;
        int actualSaveCount = Images.SaveCount;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
    }

    [TestMethod]
    public void UpdateProfileImage_NullImage_ThrowsProfileFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        ProfileFailure expectedFailure = ProfileFailure.InvalidImage;
        int expectedSaveCount = 0;
        Action action = () => Service.UpdateProfileImage(token, null!);

        FaultException<ProfileFault> exception =
            Assert.Throws<FaultException<ProfileFault>>(action);
        ProfileFailure actualFailure = exception.Detail.Reason;
        int actualSaveCount = Images.SaveCount;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
    }

    [TestMethod]
    public void UpdateProfileImage_ValidImage_SavesProfileImage()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        ImageContent content = new ImageContent
        {
            MediaType = Images.StoredImage.MediaType,
            Content = Images.StoredImage.Content
        };
        PlayerImageKind expectedKind = PlayerImageKind.Profile;
        int expectedSaveCount = 1;

        Service.UpdateProfileImage(token, content);
        PlayerImageKind? actualKind = Images.LastSavedKind;
        int actualSaveCount = Images.SaveCount;

        Assert.AreEqual(expectedKind, actualKind);
        Assert.AreEqual(expectedSaveCount, actualSaveCount);
    }
}
