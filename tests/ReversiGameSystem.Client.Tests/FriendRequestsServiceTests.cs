using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class FriendRequestsServiceTests
{
    [TestMethod]
    public void TryGetPending_MixedRequests_FiltersAndSorts()
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
        Assert.AreEqual(FriendRequestsRetrievalResult.Success, result);
        Assert.AreEqual(2, requests.Count);
        Assert.AreEqual("newer", requests[0].Id);
        Assert.AreEqual("Luis", requests[0].SenderUsername);
        Assert.AreEqual("older", requests[1].Id);
    }

    [TestMethod]
    public void TryGetPending_NoReceivedRequests_ReturnsEmpty()
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
        Assert.AreEqual(FriendRequestsRetrievalResult.Success, result);
        Assert.AreEqual(0, requests.Count);
    }
}
