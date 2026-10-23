using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class GuestSessionServiceTests
{
    internal void TryCreate_ValidName_CreatesActiveTemporarySession()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();

        // Act
        GuestSessionCreationResult result = store.TryCreate("Pedro", out GuestSession? session);

        // Assert
        TestAssert.AreEqual(GuestSessionCreationResult.Success, result);
        TestAssert.AreEqual("Pedro", session?.Name);
        TestAssert.AreEqual(true, session?.IsActive);
    }

    internal void TryCreate_DuplicateNameIgnoringCase_ReturnsAlreadyInUse()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();
        store.TryCreate("Pedro", out _);

        // Act
        GuestSessionCreationResult result = store.TryCreate("pedro", out GuestSession? duplicate);

        // Assert
        TestAssert.AreEqual(GuestSessionCreationResult.NameAlreadyInUse, result);
        TestAssert.AreEqual<GuestSession?>(null, duplicate);
    }

    internal void End_ExistingSession_ReleasesName()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();
        store.TryCreate("Pedro", out GuestSession? first);
        store.End(first);

        // Act
        GuestSessionCreationResult result = store.TryCreate("Pedro", out GuestSession? replacement);

        // Assert
        TestAssert.AreEqual(false, first?.IsActive);
        TestAssert.AreEqual(GuestSessionCreationResult.Success, result);
        TestAssert.AreEqual(true, replacement?.IsActive);
    }

    internal void TryCreate_InvalidName_DoesNotCreateSession()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();

        // Act
        GuestSessionCreationResult result = store.TryCreate(
            "Guest name",
            out GuestSession? session);

        // Assert
        TestAssert.AreEqual(GuestSessionCreationResult.InvalidNameFormat, result);
        TestAssert.AreEqual<GuestSession?>(null, session);
    }

    internal void TryCreate_UnavailableStore_ReturnsCreationError()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore(false);

        // Act
        GuestSessionCreationResult result = store.TryCreate("Pedro", out GuestSession? session);

        // Assert
        TestAssert.AreEqual(GuestSessionCreationResult.CreationError, result);
        TestAssert.AreEqual<GuestSession?>(null, session);
    }
}
