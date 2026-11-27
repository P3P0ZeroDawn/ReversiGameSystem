using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.FriendshipService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipServiceSendFriendRequestTests : FriendshipServiceTestBase
{
    private const long SenderId = 10L;
    private const long RecipientId = 20L;
    private const string SenderUsername = "Sender";
    private const string RecipientUsername = "Recipient";

    [TestMethod]
    public void SendFriendRequest_ValidTarget_ReturnsMappedRequest()
    {
        string token = AddAuthenticatedSession(SenderId, SenderUsername);
        DateTime createdAtUtc = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        PendingFriendRequest expectedRequest = new PendingFriendRequest(
            new FriendAccount(SenderId, SenderUsername, false),
            new FriendAccount(RecipientId, RecipientUsername, true),
            createdAtUtc);
        Friendships.CreatedRequest = new FriendshipResult<PendingFriendRequest>(expectedRequest);

        FriendRequest actualRequest = Service.SendFriendRequest(token, RecipientId);

        Assert.AreEqual(expectedRequest.RequestId, actualRequest.RequestId);
        Assert.AreEqual(SenderId, actualRequest.Sender.AccountId);
        Assert.AreEqual(RecipientId, actualRequest.Recipient.AccountId);
        Assert.AreEqual(createdAtUtc, actualRequest.CreatedAtUtc);
    }

    [TestMethod]
    public void SendFriendRequest_SelfTarget_ThrowsFriendshipFault()
    {
        string token = AddAuthenticatedSession(SenderId, SenderUsername);
        FriendshipFailure expectedReason = FriendshipFailure.CannotSendRequestToSelf;
        Action action = () => Service.SendFriendRequest(token, SenderId);

        FaultException<FriendshipFault> fault =
            Assert.Throws<FaultException<FriendshipFault>>(action);

        Assert.AreEqual(expectedReason, fault.Detail.Reason);
    }
}
