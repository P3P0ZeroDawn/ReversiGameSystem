using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class JoinMatchServiceTests
{
    internal void TryJoinByCode_InvalidCode_DoesNotCreateSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinByCode("bad", out MatchSession? session);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.InvalidMatchCode, result);
        TestAssert.AreEqual<MatchSession?>(null, session);
    }

    internal void TryJoinById_UnavailableMatch_DoesNotCreateSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinById("full-1", out MatchSession? session);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.NoLongerAvailable, result);
        TestAssert.AreEqual<MatchSession?>(null, session);
    }

    internal void TryJoinById_OwnMatch_DoesNotCreateSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinById("own-1", out MatchSession? session);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.CannotJoinOwnMatch, result);
        TestAssert.AreEqual<MatchSession?>(null, session);
    }

    internal void TryJoinById_AvailableMatch_StartsSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinById("waiting-1", out MatchSession? session);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.Success, result);
        TestAssert.AreEqual(true, session != null);
        MatchSession actualSession = TestAssert.IsNotNull(session);
        TestAssert.AreEqual(MatchState.InProgress, actualSession.State);
        TestAssert.AreEqual("Ana", actualSession.CreatorName);
        TestAssert.AreEqual("Pedro", actualSession.OpponentName);
        TestAssert.AreEqual("Pedro", actualSession.LocalPlayerName);
        TestAssert.AreEqual(DiscColor.White, actualSession.LocalPlayerColor);
        TestAssert.AreEqual(2, actualSession.Board.Count(DiscColor.Black));
        TestAssert.AreEqual(2, actualSession.Board.Count(DiscColor.White));
    }

    internal void TryJoinByCode_AvailableMatch_PreservesGuestSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Invitado", "Invitado");

        // Act
        JoinMatchResult result = service.TryJoinByCode("lu5n7p", out MatchSession? session);
        JoinMatchResult secondAttempt = service.TryJoinByCode("LU5N7P", out _);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.Success, result);
        TestAssert.AreEqual(true, session != null);
        MatchSession actualSession = TestAssert.IsNotNull(session);
        TestAssert.AreEqual("Invitado", actualSession.GuestName);
        TestAssert.AreEqual("Invitado", actualSession.LocalPlayerName);
        TestAssert.AreEqual(MatchState.InProgress, actualSession.State);
        TestAssert.AreEqual(JoinMatchResult.MatchCodeNotAvailable, secondAttempt);
    }
}
