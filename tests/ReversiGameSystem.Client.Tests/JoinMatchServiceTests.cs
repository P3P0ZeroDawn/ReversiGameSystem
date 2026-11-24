using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class JoinMatchServiceTests
{
    [TestMethod]
    public void TryJoinByCode_InvalidCode_DoesNotCreateSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinByCode("bad", out MatchSession? session);

        // Assert
        Assert.AreEqual(JoinMatchResult.InvalidMatchCode, result);
        Assert.AreEqual<MatchSession?>(null, session);
    }

    [TestMethod]
    public void TryJoinById_UnavailableMatch_DoesNotCreateSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinById("full-1", out MatchSession? session);

        // Assert
        Assert.AreEqual(JoinMatchResult.NoLongerAvailable, result);
        Assert.AreEqual<MatchSession?>(null, session);
    }

    [TestMethod]
    public void TryJoinById_OwnMatch_DoesNotCreateSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinById("own-1", out MatchSession? session);

        // Assert
        Assert.AreEqual(JoinMatchResult.CannotJoinOwnMatch, result);
        Assert.AreEqual<MatchSession?>(null, session);
    }

    [TestMethod]
    public void TryJoinById_AvailableMatch_StartsSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinById("waiting-1", out MatchSession? session);

        // Assert
        Assert.AreEqual(JoinMatchResult.Success, result);
        Assert.AreEqual(true, session != null);
        MatchSession actualSession = TestAssert.IsNotNull(session);
        Assert.AreEqual(MatchState.InProgress, actualSession.State);
        Assert.AreEqual("Ana", actualSession.CreatorName);
        Assert.AreEqual("Pedro", actualSession.OpponentName);
        Assert.AreEqual("Pedro", actualSession.LocalPlayerName);
        Assert.AreEqual(DiscColor.White, actualSession.LocalPlayerColor);
        Assert.AreEqual(2, actualSession.Board.Count(DiscColor.Black));
        Assert.AreEqual(2, actualSession.Board.Count(DiscColor.White));
    }

    [TestMethod]
    public void TryJoinByCode_AvailableMatch_PreservesGuestSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Invitado", "Invitado");

        // Act
        JoinMatchResult result = service.TryJoinByCode("lu5n7p", out MatchSession? session);
        JoinMatchResult secondAttempt = service.TryJoinByCode("LU5N7P", out _);

        // Assert
        Assert.AreEqual(JoinMatchResult.Success, result);
        Assert.AreEqual(true, session != null);
        MatchSession actualSession = TestAssert.IsNotNull(session);
        Assert.AreEqual("Invitado", actualSession.GuestName);
        Assert.AreEqual("Invitado", actualSession.LocalPlayerName);
        Assert.AreEqual(MatchState.InProgress, actualSession.State);
        Assert.AreEqual(JoinMatchResult.MatchCodeNotAvailable, secondAttempt);
    }
}
