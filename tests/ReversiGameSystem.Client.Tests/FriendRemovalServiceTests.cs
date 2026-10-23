using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class FriendRemovalServiceTests
{
    internal void Remove_ExistingFriendship_RemovesOnlyRelationship()
    {
        // Arrange
        FriendshipStore store = CreateStore(
            FriendshipStore.CreateRelationshipKey("Pedro", "Ana"));
        FriendRemovalService service = new FriendRemovalService("Pedro", store);

        // Act
        FriendRemovalResult result = service.Remove("Ana");
        FriendRemovalResult repeatedResult = service.Remove("Ana");
        FriendRequestResult newRequestResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        TestAssert.AreEqual(FriendRemovalResult.Success, result);
        TestAssert.AreEqual(FriendRemovalResult.NotFound, repeatedResult);
        TestAssert.AreEqual(FriendRequestResult.Success, newRequestResult);
        TestAssert.AreEqual(0, store.GetFriends("Pedro").Count);
    }

    internal void Remove_MissingFriendship_PreservesExistingFriendship()
    {
        // Arrange
        FriendshipStore store = CreateStore(
            FriendshipStore.CreateRelationshipKey("Pedro", "Ana"));
        FriendRemovalService service = new FriendRemovalService("Pedro", store);

        // Act
        FriendRemovalResult result = service.Remove("Luis");

        // Assert
        TestAssert.AreEqual(FriendRemovalResult.NotFound, result);
        TestAssert.AreEqual(1, store.GetFriends("Pedro").Count);
        TestAssert.AreEqual("Ana", store.GetFriends("Pedro")[0].Username);
    }

    internal void Remove_ThirdPartyFriendship_DoesNotModifyRelationship()
    {
        // Arrange
        FriendshipStore store = CreateStore(
            FriendshipStore.CreateRelationshipKey("Ana", "Luis"));
        FriendRemovalService service = new FriendRemovalService("Pedro", store);

        // Act
        FriendRemovalResult result = service.Remove("Ana");

        // Assert
        TestAssert.AreEqual(FriendRemovalResult.NotFound, result);
        TestAssert.AreEqual(1, store.GetFriends("Ana").Count);
        TestAssert.AreEqual("Luis", store.GetFriends("Ana")[0].Username);
    }

    private static FriendshipStore CreateStore(string relationship)
    {
        return new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana"),
                new FriendCandidate("Luis")
            },
            new[] { relationship });
    }
}
