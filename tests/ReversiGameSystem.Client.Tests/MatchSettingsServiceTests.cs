using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class MatchSettingsServiceTests
{
    private readonly MatchSettingsService _service = new MatchSettingsService();

    internal void ChangeTurnTime_WaitingCreator_UpdatesOnlyTime()
    {
        // Arrange
        MatchSession session = CreateSession();
        ReversiBoard originalBoard = session.Board;

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Pedro", 60);

        // Assert
        TestAssert.AreEqual(MatchSettingsModificationResult.Success, result);
        TestAssert.AreEqual(60, session.TurnTimeSeconds);
        TestAssert.AreEqual("ABC123", session.Code);
        TestAssert.AreEqual(MatchState.Waiting, session.State);
        TestAssert.AreEqual(true, ReferenceEquals(originalBoard, session.Board));
    }

    internal void ChangeTurnTime_StartedMatch_PreservesTime()
    {
        // Arrange
        MatchSession session = CreateSession();
        session.StartAgainstSystem("System");

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Pedro", 60);

        // Assert
        TestAssert.AreEqual(MatchSettingsModificationResult.AlreadyStarted, result);
        TestAssert.AreEqual(40, session.TurnTimeSeconds);
    }

    internal void ChangeTurnTime_DifferentPlayer_PreservesTime()
    {
        // Arrange
        MatchSession session = CreateSession();

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Cesar", 60);

        // Assert
        TestAssert.AreEqual(MatchSettingsModificationResult.Unauthorized, result);
        TestAssert.AreEqual(40, session.TurnTimeSeconds);
    }

    internal void ChangeTurnTime_InvalidValue_PreservesTime()
    {
        // Arrange
        MatchSession session = CreateSession();

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Pedro", 121);

        // Assert
        TestAssert.AreEqual(MatchSettingsModificationResult.InvalidTurnTime, result);
        TestAssert.AreEqual(40, session.TurnTimeSeconds);
    }

    private static MatchSession CreateSession()
    {
        return new MatchSession("ABC123", 40, "Pedro", null);
    }
}
