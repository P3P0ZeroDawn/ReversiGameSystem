using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class FriendsServiceTests
{
    internal void TryGetFriends_MixedRelationships_FiltersAndSorts()
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
        TestAssert.AreEqual(FriendsRetrievalResult.Success, result);
        TestAssert.AreEqual(2, friends.Count);
        TestAssert.AreEqual("Ana", friends[0].Username);
        TestAssert.AreEqual("Zoe", friends[1].Username);
    }

    internal void TryGetFriends_ReversedRelationship_ReturnsFriend()
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
        TestAssert.AreEqual(FriendsRetrievalResult.Success, result);
        TestAssert.AreEqual(1, friends.Count);
        TestAssert.AreEqual("Ana", friends[0].Username);
    }

    internal void TryGetFriends_NoRelationships_ReturnsEmpty()
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
        TestAssert.AreEqual(FriendsRetrievalResult.Success, result);
        TestAssert.AreEqual(0, friends.Count);
    }
}
