using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.FriendshipApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipApplicationServiceQueriesTests
    : FriendshipApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(0L)]
    [DataRow(-5L)]
    public void ReadConfirmedFriendIds_NonPositiveAccountId_ReturnsEmptyWithoutRepositoryCall(
        long accountId)
    {
        int expectedLength = 0;
        int expectedCallCount = 0;

        long[] actualIds = Service.ReadConfirmedFriendIds(accountId);
        int actualLength = actualIds.Length;
        int actualCallCount = Friendships.ReadConfirmedFriendIdsCalls;

        Assert.AreEqual(expectedLength, actualLength);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void ReadConfirmedFriendIds_PositiveAccountId_ReturnsRepositoryFriendIds()
    {
        long expectedAccountId = 15L;
        long[] expectedIds = new long[] { 21L, 22L };
        Friendships.ConfirmedFriendIds = expectedIds;

        long[] actualIds = Service.ReadConfirmedFriendIds(expectedAccountId);
        long actualAccountId = Friendships.LastAccountId;

        Assert.AreSame(expectedIds, actualIds);
        Assert.AreEqual(expectedAccountId, actualAccountId);
    }

    [TestMethod]
    [DataRow(0L, 2L)]
    [DataRow(-1L, 2L)]
    [DataRow(1L, 0L)]
    [DataRow(1L, -3L)]
    [DataRow(5L, 5L)]
    public void VerifyActiveFriendship_InvalidOrSameAccountIds_ReturnsFalseWithoutRepositoryCall(
        long accountId,
        long friendAccountId)
    {
        int expectedCallCount = 0;

        bool actualAreFriends = Service.VerifyActiveFriendship(accountId, friendAccountId);
        int actualCallCount = Friendships.AreFriendsCalls;

        Assert.IsFalse(actualAreFriends);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void VerifyActiveFriendship_ValidDistinctAccountIds_ReturnsRepositoryResult(
        bool expectedAreFriends)
    {
        Friendships.AreFriendsResult = expectedAreFriends;

        bool actualAreFriends = Service.VerifyActiveFriendship(10L, 20L);

        Assert.AreEqual(expectedAreFriends, actualAreFriends);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-2L)]
    public void IsAccountActive_NonPositiveAccountId_ReturnsFalseWithoutRepositoryCall(
        long accountId)
    {
        int expectedCallCount = 0;

        bool actualIsActive = Service.IsAccountActive(accountId);
        int actualCallCount = Friendships.IsAccountActiveCalls;

        Assert.IsFalse(actualIsActive);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void IsAccountActive_PositiveAccountId_ReturnsRepositoryResult(bool expectedIsActive)
    {
        Friendships.IsAccountActiveResult = expectedIsActive;

        bool actualIsActive = Service.IsAccountActive(10L);

        Assert.AreEqual(expectedIsActive, actualIsActive);
    }
}
