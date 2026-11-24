using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class FriendRequestServiceTests
{
    [TestMethod]
    public void Search_Query_ExcludesRequesterAndInactiveAccounts()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendSearchResult result = service.Search(
            "Ana",
            out IReadOnlyList<FriendCandidate> candidates);

        // Assert
        Assert.AreEqual(FriendSearchResult.Success, result);
        Assert.AreEqual(1, candidates.Count);
        Assert.AreEqual("Ana", candidates[0].Username);
    }

    [TestMethod]
    public void Send_SameAccount_ReturnsCannotAddYourself()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("pedro");

        // Assert
        Assert.AreEqual(FriendRequestResult.CannotAddYourself, result);
    }

    [TestMethod]
    public void Send_ExistingFriend_ReturnsFriendshipAlreadyExists()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("EmiPan");

        // Assert
        Assert.AreEqual(FriendRequestResult.FriendshipAlreadyExists, result);
    }

    [TestMethod]
    public void Send_PendingPair_ReturnsAlreadyPending()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("Emilio");

        // Assert
        Assert.AreEqual(FriendRequestResult.FriendRequestAlreadyPending, result);
    }

    [TestMethod]
    public void Send_ActiveAccount_CreatesExactlyOnePendingRequest()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult firstResult = service.Send("Ana");
        FriendRequestResult secondResult = service.Send("Ana");

        // Assert
        Assert.AreEqual(FriendRequestResult.Success, firstResult);
        Assert.AreEqual(
            FriendRequestResult.FriendRequestAlreadyPending,
            secondResult);
    }

    [TestMethod]
    public void Send_InactiveAccount_ReturnsAccountNotAvailable()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("Luis");

        // Assert
        Assert.AreEqual(FriendRequestResult.AccountNotAvailable, result);
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
