using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.FriendshipApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipApplicationServiceReadConfirmedFriendsTests
    : FriendshipApplicationServiceTestBase
{
    [TestMethod]
    public void ReadConfirmedFriends_MissingToken_ReturnsMissingSessionTokenWithoutRepositoryCall()
    {
        FriendshipStatus expectedStatus = FriendshipStatus.MissingSessionToken;
        int expectedCallCount = 0;

        FriendshipResult<FriendAccount[]> actualResult = Service.ReadConfirmedFriends(" ");
        FriendshipStatus actualStatus = actualResult.Status;
        int actualCallCount = Friendships.GetFriendsCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void ReadConfirmedFriends_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        FriendshipStatus expectedStatus = FriendshipStatus.InvalidSession;
        int expectedCallCount = 0;

        FriendshipResult<FriendAccount[]> actualResult =
            Service.ReadConfirmedFriends("invalid-token");
        FriendshipStatus actualStatus = actualResult.Status;
        int actualCallCount = Friendships.GetFriendsCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void ReadConfirmedFriends_InactiveAccount_ReturnsInactiveAccountWithoutRepositoryCall()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Friendships.IsAccountActiveResult = false;
        FriendshipStatus expectedStatus = FriendshipStatus.InactiveAccount;
        int expectedCallCount = 0;

        FriendshipResult<FriendAccount[]> actualResult = Service.ReadConfirmedFriends(token);
        FriendshipStatus actualStatus = actualResult.Status;
        int actualCallCount = Friendships.GetFriendsCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void ReadConfirmedFriends_ActiveSession_ReturnsRepositoryFriendsForAccount()
    {
        FriendshipStatus expectedStatus = FriendshipStatus.Success;
        long expectedAccountId = 10L;
        string token = AddAuthenticatedSession(expectedAccountId, "PlayerTen");
        FriendAccount[] expectedFriends = new FriendAccount[]
        {
            new FriendAccount(20L, "PlayerTwenty", true),
            new FriendAccount(30L, "PlayerThirty", false)
        };
        Friendships.Friends = expectedFriends;

        FriendshipResult<FriendAccount[]> actualResult = Service.ReadConfirmedFriends(token);
        FriendshipStatus actualStatus = actualResult.Status;
        FriendAccount[]? actualFriends = actualResult.Value;
        long actualAccountId = Friendships.LastAccountId;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedFriends, actualFriends);
        Assert.AreEqual(expectedAccountId, actualAccountId);
    }
}
