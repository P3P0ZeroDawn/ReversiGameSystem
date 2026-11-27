using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.FriendshipService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipServiceGetPendingFriendRequestsTests : FriendshipServiceTestBase
{
    private const long CallerId = 10L;
    private const long SenderId = 30L;
    private const string CallerUsername = "Caller";
    private const string SenderUsername = "Requester";

    [TestMethod]
    public void GetPendingFriendRequests_ValidSession_ReturnsMappedRequests()
    {
        string token = AddAuthenticatedSession(CallerId, CallerUsername);
        DateTime sentAtUtc = new DateTime(2026, 11, 10, 14, 0, 0, DateTimeKind.Utc);
        PendingFriendRequest pending = new PendingFriendRequest(
            new FriendAccount(SenderId, SenderUsername, true),
            new FriendAccount(CallerId, CallerUsername, false),
            sentAtUtc);
        Friendships.PendingRequests = new[] { pending };
        int expectedCount = 1;

        FriendRequest[] actualRequests = Service.GetPendingFriendRequests(token);

        Assert.AreEqual(expectedCount, actualRequests.Length);
        Assert.AreEqual(pending.RequestId, actualRequests[0].RequestId);
        Assert.AreEqual(SenderId, actualRequests[0].Sender.AccountId);
        Assert.AreEqual(CallerId, actualRequests[0].Recipient.AccountId);
    }

    [TestMethod]
    public void GetPendingFriendRequests_InvalidSession_ThrowsSessionFault()
    {
        SessionFailure expectedReason = SessionFailure.InvalidSession;
        Action action = () => Service.GetPendingFriendRequests("missing-token");

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, fault.Detail.Reason);
    }
}
