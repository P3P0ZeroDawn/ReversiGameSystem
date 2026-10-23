using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class FriendRequestResponseServiceTests
{
    internal void Respond_AcceptedRequest_CreatesFriendship()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Pending);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Pedro", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", true);
        FriendRequestResult duplicateResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        TestAssert.AreEqual(FriendRequestResponseResult.Accepted, result);
        TestAssert.AreEqual(
            FriendRequestResult.FriendshipAlreadyExists,
            duplicateResult);
        TestAssert.AreEqual(0, store.GetPendingReceived("Pedro").Count);
        TestAssert.AreEqual(1, store.GetFriends("Pedro").Count);
    }

    internal void Respond_RejectedRequest_DoesNotCreateFriendship()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Pending);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Pedro", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", false);
        FriendRequestResult newRequestResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        TestAssert.AreEqual(FriendRequestResponseResult.Rejected, result);
        TestAssert.AreEqual(FriendRequestResult.Success, newRequestResult);
        TestAssert.AreEqual(0, store.GetPendingReceived("Pedro").Count);
    }

    internal void Respond_ProcessedRequest_PreservesState()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Rejected);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Pedro", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", true);
        FriendRequestResult newRequestResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        TestAssert.AreEqual(FriendRequestResponseResult.AlreadyProcessed, result);
        TestAssert.AreEqual(FriendRequestResult.Success, newRequestResult);
    }

    internal void Respond_DifferentRecipient_PreservesPendingRequest()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Pending);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Luis", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", true);

        // Assert
        TestAssert.AreEqual(FriendRequestResponseResult.Unauthorized, result);
        TestAssert.AreEqual(1, store.GetPendingReceived("Pedro").Count);
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
