using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.FriendshipApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipOperationsTests : FriendshipApplicationServiceTestBase
{
    private const long SenderId = 10;
    private const long RecipientId = 20;

    [TestMethod]
    public void FindPlayers_SearchWithSpaces_TrimsSearchBeforeRepository()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");

        Service.FindPlayers(token, "  Recipient  ");

        Assert.AreEqual("Recipient", Friendships.LastSearchText);
    }

    [TestMethod]
    public void FindPlayers_EmptySearch_ReturnsEmptyPlayers()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");
        Friendships.SearchPlayers = new[] { new FriendAccount(RecipientId, "Recipient", false) };

        FriendshipResult<FriendAccount[]> result = Service.FindPlayers(token, " ");

        Assert.AreEqual(0, result.Value!.Length);
    }

    [TestMethod]
    [DataRow("search")]
    [DataRow("send")]
    [DataRow("pending")]
    [DataRow("respond")]
    [DataRow("remove")]
    public void FriendshipOperation_InvalidSession_ReturnsInvalidSession(string operation)
    {
        const string invalidToken = "invalid-session";

        FriendshipStatus status = operation switch
        {
            "search" => Service.FindPlayers(invalidToken, "Recipient").Status,
            "send" => Service.SendFriendRequest(invalidToken, RecipientId).Status,
            "pending" => Service.ReadPendingRequests(invalidToken).Status,
            "respond" => Service.RespondToFriendRequest(invalidToken, "request", true).Status,
            "remove" => Service.RemoveFriendship(invalidToken, RecipientId).Status,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        Assert.AreEqual(FriendshipStatus.InvalidSession, status);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    public void SendFriendRequest_InvalidRecipient_ReturnsInvalidAccountId(long recipientId)
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");

        FriendshipResult<PendingFriendRequest> result = Service.SendFriendRequest(
            token, recipientId);

        Assert.AreEqual(FriendshipStatus.InvalidAccountId, result.Status);
    }

    [TestMethod]
    public void SendFriendRequest_SelfRequest_DoesNotCallRepository()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");

        Service.SendFriendRequest(token, SenderId);

        Assert.AreEqual(0, Friendships.CreateRequestCalls);
    }

    [TestMethod]
    [DataRow(FriendshipStatus.FriendshipAlreadyExists)]
    [DataRow(FriendshipStatus.RequestAlreadyPending)]
    [DataRow(FriendshipStatus.AccountNotFound)]
    public void SendFriendRequest_ConflictingStoredState_PreservesRejection(
        FriendshipStatus rejection)
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");
        Friendships.CreatedRequest = new FriendshipResult<PendingFriendRequest>(rejection);

        FriendshipResult<PendingFriendRequest> result = Service.SendFriendRequest(
            token, RecipientId);

        Assert.AreEqual(rejection, result.Status);
    }

    [TestMethod]
    public void ReadPendingRequests_AuthorizedSession_PreservesRepositoryRequests()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");
        PendingFriendRequest request = CreateRequest();
        Friendships.PendingRequests = new[] { request };

        FriendshipResult<PendingFriendRequest[]> result = Service.ReadPendingRequests(token);

        Assert.AreSame(Friendships.PendingRequests, result.Value);
    }

    [TestMethod]
    public void RespondToFriendRequest_SenderAttemptsResponse_ReturnsUnauthorizedRequest()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");
        PendingFriendRequest request = CreateRequest();
        Friendships.PendingRequests = new[] { request };

        FriendshipResult<PendingFriendRequest> result = Service.RespondToFriendRequest(
            token, request.RequestId, true);

        Assert.AreEqual(FriendshipStatus.UnauthorizedRequest, result.Status);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void RespondToFriendRequest_MissingRequestId_ReturnsMissingRequestId(string? requestId)
    {
        string token = AddAuthenticatedSession(RecipientId, "Recipient");

        FriendshipResult<PendingFriendRequest> result = Service.RespondToFriendRequest(
            token, requestId!, true);

        Assert.AreEqual(FriendshipStatus.MissingRequestId, result.Status);
    }

    [TestMethod]
    public void RespondToFriendRequest_UnknownRequest_DoesNotPersistResponse()
    {
        string token = AddAuthenticatedSession(RecipientId, "Recipient");
        Friendships.PendingRequests = new[] { CreateRequest() };

        Service.RespondToFriendRequest(token, "another-request", true);

        Assert.AreEqual(0, Friendships.RespondToRequestCalls);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void RespondToFriendRequest_RecipientDecision_PreservesAcceptance(bool accept)
    {
        string token = AddAuthenticatedSession(RecipientId, "Recipient");
        PendingFriendRequest request = CreateRequest();
        Friendships.PendingRequests = new[] { request };

        Service.RespondToFriendRequest(token, request.RequestId, accept);

        Assert.AreEqual(accept, Friendships.LastAcceptance);
    }

    [TestMethod]
    public void RespondToFriendRequest_ConcurrentResolution_ReturnsRequestUnavailable()
    {
        string token = AddAuthenticatedSession(RecipientId, "Recipient");
        PendingFriendRequest request = CreateRequest();
        Friendships.PendingRequests = new[] { request };
        Friendships.ResponseStatus = FriendshipStatus.RequestUnavailable;

        FriendshipResult<PendingFriendRequest> result = Service.RespondToFriendRequest(
            token, request.RequestId, true);

        Assert.AreEqual(FriendshipStatus.RequestUnavailable, result.Status);
    }

    [TestMethod]
    public void RemoveFriendship_SelfReference_DoesNotCallRepository()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");

        Service.RemoveFriendship(token, SenderId);

        Assert.AreEqual(0, Friendships.RemoveFriendshipCalls);
    }

    [TestMethod]
    public void RemoveFriendship_ConfirmedRemoval_ReturnsCallerForNotifications()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");

        FriendshipResult<long> result = Service.RemoveFriendship(token, RecipientId);

        Assert.AreEqual(SenderId, result.Value);
    }

    private static PendingFriendRequest CreateRequest()
    {
        return new PendingFriendRequest(new FriendAccount(SenderId, "Sender", false),
            new FriendAccount(RecipientId, "Recipient", true), DateTime.UtcNow);
    }
}
