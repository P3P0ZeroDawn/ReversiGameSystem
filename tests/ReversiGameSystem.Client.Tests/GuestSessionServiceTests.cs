using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class GuestSessionServiceTests
{
    [TestMethod]
    public void TryCreate_ValidName_CreatesActiveTemporarySession()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();

        // Act
        GuestSessionCreationResult result = store.TryCreate("Pedro", out GuestSession? session);

        // Assert
        Assert.AreEqual(GuestSessionCreationResult.Success, result);
        Assert.AreEqual("Pedro", session?.Name);
        Assert.AreEqual(true, session?.IsActive);
    }

    [TestMethod]
    public void TryCreate_DuplicateNameIgnoringCase_ReturnsAlreadyInUse()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();
        store.TryCreate("Pedro", out _);

        // Act
        GuestSessionCreationResult result = store.TryCreate("pedro", out GuestSession? duplicate);

        // Assert
        Assert.AreEqual(GuestSessionCreationResult.NameAlreadyInUse, result);
        Assert.AreEqual<GuestSession?>(null, duplicate);
    }

    [TestMethod]
    public void End_ExistingSession_ReleasesName()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();
        store.TryCreate("Pedro", out GuestSession? first);
        store.End(first);

        // Act
        GuestSessionCreationResult result = store.TryCreate("Pedro", out GuestSession? replacement);

        // Assert
        Assert.AreEqual(false, first?.IsActive);
        Assert.AreEqual(GuestSessionCreationResult.Success, result);
        Assert.AreEqual(true, replacement?.IsActive);
    }

    [TestMethod]
    public void TryCreate_InvalidName_DoesNotCreateSession()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();

        // Act
        GuestSessionCreationResult result = store.TryCreate(
            "Guest name",
            out GuestSession? session);

        // Assert
        Assert.AreEqual(GuestSessionCreationResult.InvalidNameFormat, result);
        Assert.AreEqual<GuestSession?>(null, session);
    }

    [TestMethod]
    public void TryCreate_UnavailableStore_ReturnsCreationError()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore(false);

        // Act
        GuestSessionCreationResult result = store.TryCreate("Pedro", out GuestSession? session);

        // Assert
        Assert.AreEqual(GuestSessionCreationResult.CreationError, result);
        Assert.AreEqual<GuestSession?>(null, session);
    }
}
