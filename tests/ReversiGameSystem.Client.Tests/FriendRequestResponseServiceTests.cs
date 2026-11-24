using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class FriendRequestResponseServiceTests
{
    [TestMethod]
    public void Respond_AcceptedRequest_CreatesFriendship()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Pending);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Pedro", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", true);
        FriendRequestResult duplicateResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        Assert.AreEqual(FriendRequestResponseResult.Accepted, result);
        Assert.AreEqual(
            FriendRequestResult.FriendshipAlreadyExists,
            duplicateResult);
        Assert.AreEqual(0, store.GetPendingReceived("Pedro").Count);
        Assert.AreEqual(1, store.GetFriends("Pedro").Count);
    }

    [TestMethod]
    public void Respond_RejectedRequest_DoesNotCreateFriendship()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Pending);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Pedro", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", false);
        FriendRequestResult newRequestResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        Assert.AreEqual(FriendRequestResponseResult.Rejected, result);
        Assert.AreEqual(FriendRequestResult.Success, newRequestResult);
        Assert.AreEqual(0, store.GetPendingReceived("Pedro").Count);
    }

    [TestMethod]
    public void Respond_ProcessedRequest_PreservesState()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Rejected);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Pedro", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", true);
        FriendRequestResult newRequestResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        Assert.AreEqual(FriendRequestResponseResult.AlreadyProcessed, result);
        Assert.AreEqual(FriendRequestResult.Success, newRequestResult);
    }

    [TestMethod]
    public void Respond_DifferentRecipient_PreservesPendingRequest()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Pending);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Luis", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", true);

        // Assert
        Assert.AreEqual(FriendRequestResponseResult.Unauthorized, result);
        Assert.AreEqual(1, store.GetPendingReceived("Pedro").Count);
    }

    private static FriendshipStore CreateStore(FriendshipRequestState state)
    {
        return new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana"),
                new FriendCandidate("Luis")
            },
            friendRequests: new[]
            {
                new FriendshipRequest(
                    "request",
                    "Ana",
                    "Pedro",
                    new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc),
                    state)
            });
    }
}
