using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.FriendshipService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipServiceRespondToFriendRequestTests : FriendshipServiceTestBase
{
    private const long RecipientId = 20L;
    private const long SenderId = 10L;
    private const string RecipientUsername = "Recipient";
    private const string SenderUsername = "Sender";
    private const string SampleRequestId = "req-10-20";

    [TestMethod]
    public void RespondToFriendRequest_ValidAccept_InvokesRepositoryWithAcceptance()
    {
        string token = AddAuthenticatedSession(RecipientId, RecipientUsername);
        PendingFriendRequest pending = new PendingFriendRequest(
            new FriendAccount(SenderId, SenderUsername, false),
            new FriendAccount(RecipientId, RecipientUsername, false),
            DateTime.UtcNow);
        Friendships.PendingRequests = new[] { pending };
        int expectedCallCount = 1;

        Service.RespondToFriendRequest(token, pending.RequestId, true);

        Assert.AreEqual(expectedCallCount, Friendships.RespondToRequestCalls);
        Assert.IsTrue(Friendships.LastAcceptance);
    }

    [TestMethod]
    public void RespondToFriendRequest_MissingRequest_ThrowsFriendshipFault()
    {
        string token = AddAuthenticatedSession(RecipientId, RecipientUsername);
        FriendshipFailure expectedReason = FriendshipFailure.RequestUnavailable;
        Action action = () => Service.RespondToFriendRequest(token, SampleRequestId, true);

        FaultException<FriendshipFault> fault =
            Assert.Throws<FaultException<FriendshipFault>>(action);

        Assert.AreEqual(expectedReason, fault.Detail.Reason);
    }
}
