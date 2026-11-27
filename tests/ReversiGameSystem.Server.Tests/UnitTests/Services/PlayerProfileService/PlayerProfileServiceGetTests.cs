using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ContractProfile = ReversiGameSystem.Server.Contracts.Players.PlayerProfile;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.PlayerProfileService;

[TestClass]
[TestCategory("Unit")]
public sealed class PlayerProfileServiceGetTests : PlayerProfileServiceTestBase
{
    [TestMethod]
    public void Constructor_NullApplicationService_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.ServiceApi.PlayerProfileService(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    [DataRow(null, SessionFailure.MissingSessionToken)]
    [DataRow("invalid-token", SessionFailure.InvalidSession)]
    public void GetPlayerProfile_RejectedSession_ThrowsSessionFault(
        string? sessionToken,
        SessionFailure expectedFailure)
    {
        int expectedCallCount = 0;
        Action action = () => Service.GetPlayerProfile(sessionToken!, null);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;
        int actualCallCount = Profiles.GetProfileCalls;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetPlayerProfile_InvalidTargetAccountId_ThrowsProfileFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        ProfileFailure expectedFailure = ProfileFailure.InvalidAccountId;
        int expectedCallCount = 0;
        Action action = () => Service.GetPlayerProfile(token, 0L);

        FaultException<ProfileFault> exception =
            Assert.Throws<FaultException<ProfileFault>>(action);
        ProfileFailure actualFailure = exception.Detail.Reason;
        int actualCallCount = Profiles.GetProfileCalls;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetPlayerProfile_TargetAccountNotFound_ThrowsProfileFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.ProfileResult = new ProfileResult<PlayerProfile>(ProfileStatus.AccountNotFound);
        ProfileFailure expectedFailure = ProfileFailure.AccountNotFound;
        Action action = () => Service.GetPlayerProfile(token, 99L);

        FaultException<ProfileFault> exception =
            Assert.Throws<FaultException<ProfileFault>>(action);
        ProfileFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void GetPlayerProfile_ValidSession_ReturnsMappedContractProfile()
    {
        long expectedAccountId = 10L;
        string expectedUsername = "PlayerTen";
        int expectedMatches = 5;
        int expectedWins = 3;
        int expectedLosses = 1;
        int expectedDraws = 1;
        double expectedWinRate = 0.6d;
        string token = AddAuthenticatedSession(expectedAccountId, expectedUsername);
        Profiles.ProfileResult = new ProfileResult<PlayerProfile>(
            new PlayerProfile(
                expectedAccountId,
                expectedUsername,
                "profiles/avatar.png",
                expectedWins,
                expectedLosses,
                expectedDraws));

        ContractProfile actualProfile = Service.GetPlayerProfile(token, null);
        long actualAccountId = actualProfile.Account.AccountId;
        string actualUsername = actualProfile.Account.Username;
        bool actualHasImage = actualProfile.HasProfileImage;
        int actualMatches = actualProfile.Statistics.MatchesPlayed;
        int actualWins = actualProfile.Statistics.Wins;
        int actualLosses = actualProfile.Statistics.Losses;
        int actualDraws = actualProfile.Statistics.Draws;
        double actualWinRate = actualProfile.Statistics.WinRate;

        Assert.AreEqual(expectedAccountId, actualAccountId);
        Assert.AreEqual(expectedUsername, actualUsername);
        Assert.IsTrue(actualHasImage);
        Assert.AreEqual(expectedMatches, actualMatches);
        Assert.AreEqual(expectedWins, actualWins);
        Assert.AreEqual(expectedLosses, actualLosses);
        Assert.AreEqual(expectedDraws, actualDraws);
        Assert.AreEqual(expectedWinRate, actualWinRate);
    }

    [TestMethod]
    [DataRow(null, SessionFailure.MissingSessionToken)]
    [DataRow("invalid-token", SessionFailure.InvalidSession)]
    public void GetProfileImage_RejectedSession_ThrowsSessionFault(
        string? sessionToken,
        SessionFailure expectedFailure)
    {
        int expectedReadCount = 0;
        Action action = () => Service.GetProfileImage(sessionToken!, null);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;
        int actualReadCount = Images.ReadCount;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedReadCount, actualReadCount);
    }

    [TestMethod]
    public void GetProfileImage_AbsentImage_ReturnsNull()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.ImageReferenceResult = new ProfileResult<string>(string.Empty);

        ImageContent? actualImage = Service.GetProfileImage(token, null);

        Assert.IsNull(actualImage);
    }

    [TestMethod]
    public void GetProfileImage_PresentImage_ReturnsMappedImageContent()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        string expectedMediaType = Images.StoredImage.MediaType;
        byte[] expectedContent = Images.StoredImage.Content;

        ImageContent? actualImage = Service.GetProfileImage(token, null);

        Assert.IsNotNull(actualImage);
        string actualMediaType = actualImage.MediaType;
        byte[] actualContent = actualImage.Content;
        Assert.AreEqual(expectedMediaType, actualMediaType);
        CollectionAssert.AreEqual(expectedContent, actualContent);
    }
}
