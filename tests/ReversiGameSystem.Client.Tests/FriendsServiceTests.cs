using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class FriendsServiceTests
{
    [TestMethod]
    public void TryGetFriends_MixedRelationships_FiltersAndSorts()
    {
        // Arrange
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Zoe"),
                new FriendCandidate("Ana"),
                new FriendCandidate("Luis", false),
                new FriendCandidate("Mario")
            },
            new[]
            {
                FriendshipStore.CreateRelationshipKey("Pedro", "Zoe"),
                FriendshipStore.CreateRelationshipKey("Pedro", "Ana"),
                FriendshipStore.CreateRelationshipKey("Pedro", "Luis")
            });
        FriendsService service = new FriendsService("Pedro", store);

        // Act
        FriendsRetrievalResult result = service.TryGetFriends(
            out IReadOnlyList<FriendCandidate> friends);

        // Assert
        Assert.AreEqual(FriendsRetrievalResult.Success, result);
        Assert.AreEqual(2, friends.Count);
        Assert.AreEqual("Ana", friends[0].Username);
        Assert.AreEqual("Zoe", friends[1].Username);
    }

    [TestMethod]
    public void TryGetFriends_ReversedRelationship_ReturnsFriend()
    {
        // Arrange
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana")
            },
            new[] { FriendshipStore.CreateRelationshipKey("Ana", "Pedro") });
        FriendsService service = new FriendsService("Pedro", store);

        // Act
        FriendsRetrievalResult result = service.TryGetFriends(
            out IReadOnlyList<FriendCandidate> friends);

        // Assert
        Assert.AreEqual(FriendsRetrievalResult.Success, result);
        Assert.AreEqual(1, friends.Count);
        Assert.AreEqual("Ana", friends[0].Username);
    }

    [TestMethod]
    public void TryGetFriends_NoRelationships_ReturnsEmpty()
    {
        // Arrange
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana")
            });
        FriendsService service = new FriendsService("Pedro", store);

        // Act
        FriendsRetrievalResult result = service.TryGetFriends(
            out IReadOnlyList<FriendCandidate> friends);

        // Assert
        Assert.AreEqual(FriendsRetrievalResult.Success, result);
        Assert.AreEqual(0, friends.Count);
    }
}
