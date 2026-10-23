using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class FriendRequestServiceTests
{
    internal void Search_Query_ExcludesRequesterAndInactiveAccounts()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendSearchResult result = service.Search(
            "Ana",
            out IReadOnlyList<FriendCandidate> candidates);

        // Assert
        TestAssert.AreEqual(FriendSearchResult.Success, result);
        TestAssert.AreEqual(1, candidates.Count);
        TestAssert.AreEqual("Ana", candidates[0].Username);
    }

    internal void Send_SameAccount_ReturnsCannotAddYourself()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("pedro");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.CannotAddYourself, result);
    }

    internal void Send_ExistingFriend_ReturnsFriendshipAlreadyExists()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("EmiPan");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.FriendshipAlreadyExists, result);
    }

    internal void Send_PendingPair_ReturnsAlreadyPending()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("Emilio");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.FriendRequestAlreadyPending, result);
    }

    internal void Send_ActiveAccount_CreatesExactlyOnePendingRequest()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult firstResult = service.Send("Ana");
        FriendRequestResult secondResult = service.Send("Ana");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.Success, firstResult);
        TestAssert.AreEqual(
            FriendRequestResult.FriendRequestAlreadyPending,
            secondResult);
    }

    internal void Send_InactiveAccount_ReturnsAccountNotAvailable()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("Luis");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.AccountNotAvailable, result);
    }

    private static FriendRequestService CreateService()
    {
        string friendship = FriendshipStore.CreateRelationshipKey("Pedro", "EmiPan");
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana"),
                new FriendCandidate("EmiPan"),
                new FriendCandidate("Emilio"),
                new FriendCandidate("Luis", false)
            },
            new[] { friendship },
            new[]
            {
                new FriendshipRequest(
                    "pending",
                    "Emilio",
                    "Pedro",
                    new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc))
            });
        return new FriendRequestService("Pedro", store);
    }
}
