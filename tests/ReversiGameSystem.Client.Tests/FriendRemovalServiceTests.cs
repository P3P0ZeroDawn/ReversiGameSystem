using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class FriendRemovalServiceTests
{
    [TestMethod]
    public void Remove_ExistingFriendship_RemovesOnlyRelationship()
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
        Assert.AreEqual(FriendRemovalResult.Success, result);
        Assert.AreEqual(FriendRemovalResult.NotFound, repeatedResult);
        Assert.AreEqual(FriendRequestResult.Success, newRequestResult);
        Assert.AreEqual(0, store.GetFriends("Pedro").Count);
    }

    [TestMethod]
    public void Remove_MissingFriendship_PreservesExistingFriendship()
    {
        // Arrange
        FriendshipStore store = CreateStore(
            FriendshipStore.CreateRelationshipKey("Pedro", "Ana"));
        FriendRemovalService service = new FriendRemovalService("Pedro", store);

        // Act
        FriendRemovalResult result = service.Remove("Luis");

        // Assert
        Assert.AreEqual(FriendRemovalResult.NotFound, result);
        Assert.AreEqual(1, store.GetFriends("Pedro").Count);
        Assert.AreEqual("Ana", store.GetFriends("Pedro")[0].Username);
    }

    [TestMethod]
    public void Remove_ThirdPartyFriendship_DoesNotModifyRelationship()
    {
        // Arrange
        FriendshipStore store = CreateStore(
            FriendshipStore.CreateRelationshipKey("Ana", "Luis"));
        FriendRemovalService service = new FriendRemovalService("Pedro", store);

        // Act
        FriendRemovalResult result = service.Remove("Ana");

        // Assert
        Assert.AreEqual(FriendRemovalResult.NotFound, result);
        Assert.AreEqual(1, store.GetFriends("Ana").Count);
        Assert.AreEqual("Luis", store.GetFriends("Ana")[0].Username);
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
