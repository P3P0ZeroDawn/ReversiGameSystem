using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Players;
using ApplicationService =
    ReversiGameSystem.Server.Application.Players.PlayerProfileApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PlayerProfileApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class PlayerProfileApplicationServiceGetProfileTests
    : PlayerProfileApplicationServiceTestBase
{
    [TestMethod]
    public void Constructor_NullSessions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(
            null!,
            Profiles,
            Images,
            NullLogger<ApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullProfiles_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(
            Sessions,
            null!,
            Images,
            NullLogger<ApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullImages_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(
            Sessions,
            Profiles,
            null!,
            NullLogger<ApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(
            Sessions,
            Profiles,
            Images,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GetProfile_MissingSessionToken_ReturnsMissingSessionTokenWithoutRepositoryCall(
        string? sessionToken)
    {
        ProfileStatus expectedStatus = ProfileStatus.MissingSessionToken;
        int expectedCallCount = 0;

        ProfileResult<PlayerProfile> actualResult = Service.GetProfile(sessionToken!, null);
        ProfileStatus actualStatus = actualResult.Status;
        int actualCallCount = Profiles.GetProfileCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetProfile_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ProfileStatus expectedStatus = ProfileStatus.InvalidSession;
        int expectedCallCount = 0;

        ProfileResult<PlayerProfile> actualResult = Service.GetProfile("invalid-token", null);
        ProfileStatus actualStatus = actualResult.Status;
        int actualCallCount = Profiles.GetProfileCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-5L)]
    public void GetProfile_NonPositiveTargetAccountId_ReturnsInvalidAccountIdWithoutRepositoryCall(
        long targetAccountId)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        ProfileStatus expectedStatus = ProfileStatus.InvalidAccountId;
        int expectedCallCount = 0;

        ProfileResult<PlayerProfile> actualResult = Service.GetProfile(token, targetAccountId);
        ProfileStatus actualStatus = actualResult.Status;
        int actualCallCount = Profiles.GetProfileCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetProfile_OwnProfileUnavailable_ReturnsInactiveAccount()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.ProfileResult = new ProfileResult<PlayerProfile>(ProfileStatus.AccountNotFound);
        ProfileStatus expectedStatus = ProfileStatus.InactiveAccount;

        ProfileResult<PlayerProfile> actualResult = Service.GetProfile(token, null);
        ProfileStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void GetProfile_OwnProfile_ReturnsSuccessWithProfile()
    {
        long expectedAccountId = 10L;
        string token = AddAuthenticatedSession(expectedAccountId, "PlayerTen");
        PlayerProfile expectedProfile = new PlayerProfile(
            expectedAccountId,
            "PlayerTen",
            "profiles/a.png",
            4,
            2,
            1);
        Profiles.ProfileResult = new ProfileResult<PlayerProfile>(expectedProfile);
        ProfileStatus expectedStatus = ProfileStatus.Success;

        ProfileResult<PlayerProfile> actualResult = Service.GetProfile(token, null);
        ProfileStatus actualStatus = actualResult.Status;
        PlayerProfile? actualProfile = actualResult.Value;
        long actualAccountId = Profiles.LastAccountId;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedProfile, actualProfile);
        Assert.AreEqual(expectedAccountId, actualAccountId);
    }

    [TestMethod]
    public void GetProfile_OtherAccountWithInactiveCaller_ReturnsInactiveAccountWithoutLookup()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.IsActiveResult = false;
        ProfileStatus expectedStatus = ProfileStatus.InactiveAccount;
        int expectedCallCount = 0;

        ProfileResult<PlayerProfile> actualResult = Service.GetProfile(token, 20L);
        ProfileStatus actualStatus = actualResult.Status;
        int actualCallCount = Profiles.GetProfileCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetProfile_OtherAccountWithActiveCaller_QueriesTargetProfile()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        long expectedTargetAccountId = 20L;
        PlayerProfile expectedProfile = new PlayerProfile(
            expectedTargetAccountId,
            "PlayerTwenty",
            null,
            1,
            0,
            0);
        Profiles.ProfileResult = new ProfileResult<PlayerProfile>(expectedProfile);
        ProfileStatus expectedStatus = ProfileStatus.Success;

        ProfileResult<PlayerProfile> actualResult =
            Service.GetProfile(token, expectedTargetAccountId);
        ProfileStatus actualStatus = actualResult.Status;
        PlayerProfile? actualProfile = actualResult.Value;
        long actualTargetAccountId = Profiles.LastAccountId;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedProfile, actualProfile);
        Assert.AreEqual(expectedTargetAccountId, actualTargetAccountId);
    }
}
