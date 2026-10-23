using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class FriendRequestsServiceTests
{
    internal void TryGetPending_MixedRequests_FiltersAndSorts()
    {
        // Arrange
        DateTime olderDate = new DateTime(
            2026,
            9,
            17,
            12,
            0,
            0,
            DateTimeKind.Utc);
        DateTime newerDate = olderDate.AddHours(3);
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana"),
                new FriendCandidate("Luis"),
                new FriendCandidate("Luna")
            },
            friendRequests: new[]
            {
                new FriendshipRequest("older", "Ana", "Pedro", olderDate),
                new FriendshipRequest("newer", "Luis", "pedro", newerDate),
                new FriendshipRequest("outgoing", "Pedro", "Luna", newerDate),
                new FriendshipRequest(
                    "accepted",
                    "Luna",
                    "Pedro",
                    newerDate,
                    FriendshipRequestState.Accepted)
            });
        FriendRequestsService service = new FriendRequestsService("Pedro", store);

        // Act
        FriendRequestsRetrievalResult result = service.TryGetPending(
            out IReadOnlyList<FriendshipRequest> requests);

        // Assert
        TestAssert.AreEqual(FriendRequestsRetrievalResult.Success, result);
        TestAssert.AreEqual(2, requests.Count);
        TestAssert.AreEqual("newer", requests[0].Id);
        TestAssert.AreEqual("Luis", requests[0].SenderUsername);
        TestAssert.AreEqual("older", requests[1].Id);
    }

    internal void TryGetPending_NoReceivedRequests_ReturnsEmpty()
    {
        // Arrange
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana")
            },
            friendRequests: new[]
            {
                new FriendshipRequest(
                    "outgoing",
                    "Pedro",
                    "Ana",
                    new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc))
            });
        FriendRequestsService service = new FriendRequestsService("Pedro", store);

        // Act
        FriendRequestsRetrievalResult result = service.TryGetPending(
            out IReadOnlyList<FriendshipRequest> requests);

        // Assert
        TestAssert.AreEqual(FriendRequestsRetrievalResult.Success, result);
        TestAssert.AreEqual(0, requests.Count);
    }
}
