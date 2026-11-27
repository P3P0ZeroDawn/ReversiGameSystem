using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.FriendshipService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipServiceRemoveFriendTests : FriendshipServiceTestBase
{
    private const long CallerId = 10L;
    private const long FriendId = 20L;
    private const string CallerUsername = "Caller";

    [TestMethod]
    public void RemoveFriend_ExistingFriendship_InvokesRepositoryRemoval()
    {
        string token = AddAuthenticatedSession(CallerId, CallerUsername);
        int expectedCallCount = 1;

        Service.RemoveFriend(token, FriendId);

        Assert.AreEqual(expectedCallCount, Friendships.RemoveFriendshipCalls);
        Assert.AreEqual(CallerId, Friendships.LastAccountId);
        Assert.AreEqual(FriendId, Friendships.LastFriendAccountId);
    }

    [TestMethod]
    public void RemoveFriend_NotFriends_ThrowsFriendshipFault()
    {
        string token = AddAuthenticatedSession(CallerId, CallerUsername);
        Friendships.RemovalStatus = FriendshipStatus.FriendshipNotFound;
        FriendshipFailure expectedReason = FriendshipFailure.FriendshipNotFound;
        Action action = () => Service.RemoveFriend(token, FriendId);

        FaultException<FriendshipFault> fault =
            Assert.Throws<FaultException<FriendshipFault>>(action);

        Assert.AreEqual(expectedReason, fault.Detail.Reason);
    }
}
