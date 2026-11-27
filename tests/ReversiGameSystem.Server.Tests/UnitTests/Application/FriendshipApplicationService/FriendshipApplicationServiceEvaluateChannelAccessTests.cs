using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Friendships;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.FriendshipApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipApplicationServiceEvaluateChannelAccessTests
    : FriendshipApplicationServiceTestBase
{
    [TestMethod]
    public void Constructor_NullSessions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.Application.Friendships.FriendshipApplicationService(
            null!,
            Friendships);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullFriendships_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.Application.Friendships.FriendshipApplicationService(
            Sessions,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void EvaluateChannelAccess_MissingToken_ReturnsMissingSessionTokenWithoutRepositoryCall(
        string? sessionToken)
    {
        FriendshipStatus expectedStatus = FriendshipStatus.MissingSessionToken;
        int expectedCallCount = 0;

        FriendshipResult<AuthenticatedSession> actualResult =
            Service.EvaluateChannelAccess(sessionToken!);
        FriendshipStatus actualStatus = actualResult.Status;
        int actualCallCount = Friendships.IsAccountActiveCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void EvaluateChannelAccess_UnknownToken_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        FriendshipStatus expectedStatus = FriendshipStatus.InvalidSession;
        int expectedCallCount = 0;

        FriendshipResult<AuthenticatedSession> actualResult =
            Service.EvaluateChannelAccess("unknown-token");
        FriendshipStatus actualStatus = actualResult.Status;
        int actualCallCount = Friendships.IsAccountActiveCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void EvaluateChannelAccess_ExpiredSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Clock.UtcNow = Clock.UtcNow.AddHours(25);
        FriendshipStatus expectedStatus = FriendshipStatus.InvalidSession;
        int expectedCallCount = 0;

        FriendshipResult<AuthenticatedSession> actualResult =
            Service.EvaluateChannelAccess(token);
        FriendshipStatus actualStatus = actualResult.Status;
        int actualCallCount = Friendships.IsAccountActiveCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void EvaluateChannelAccess_InactiveAccount_ReturnsInactiveAccount()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Friendships.IsAccountActiveResult = false;
        FriendshipStatus expectedStatus = FriendshipStatus.InactiveAccount;

        FriendshipResult<AuthenticatedSession> actualResult =
            Service.EvaluateChannelAccess(token);
        FriendshipStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void EvaluateChannelAccess_ActiveSession_ReturnsSuccessWithAuthenticatedSession()
    {
        FriendshipStatus expectedStatus = FriendshipStatus.Success;
        long expectedAccountId = 10L;
        string token = AddAuthenticatedSession(expectedAccountId, "PlayerTen");

        FriendshipResult<AuthenticatedSession> actualResult =
            Service.EvaluateChannelAccess(token);
        FriendshipStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNotNull(actualResult.Value);
        long actualAccountId = actualResult.Value.AccountId;
        Assert.AreEqual(expectedAccountId, actualAccountId);
    }
}
