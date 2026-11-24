using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class MatchSettingsServiceTests
{
    private readonly MatchSettingsService _service = new MatchSettingsService();

    [TestMethod]
    public void ChangeTurnTime_WaitingCreator_UpdatesOnlyTime()
    {
        // Arrange
        MatchSession session = CreateSession();
        ReversiBoard originalBoard = session.Board;

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Pedro", 60);

        // Assert
        Assert.AreEqual(MatchSettingsModificationResult.Success, result);
        Assert.AreEqual(60, session.TurnTimeSeconds);
        Assert.AreEqual("ABC123", session.Code);
        Assert.AreEqual(MatchState.Waiting, session.State);
        Assert.AreEqual(true, ReferenceEquals(originalBoard, session.Board));
    }

    [TestMethod]
    public void ChangeTurnTime_StartedMatch_PreservesTime()
    {
        // Arrange
        MatchSession session = CreateSession();
        session.StartAgainstSystem("System");

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Pedro", 60);

        // Assert
        Assert.AreEqual(MatchSettingsModificationResult.AlreadyStarted, result);
        Assert.AreEqual(40, session.TurnTimeSeconds);
    }

    [TestMethod]
    public void ChangeTurnTime_DifferentPlayer_PreservesTime()
    {
        // Arrange
        MatchSession session = CreateSession();

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Cesar", 60);

        // Assert
        Assert.AreEqual(MatchSettingsModificationResult.Unauthorized, result);
        Assert.AreEqual(40, session.TurnTimeSeconds);
    }

    [TestMethod]
    public void ChangeTurnTime_InvalidValue_PreservesTime()
    {
        // Arrange
        MatchSession session = CreateSession();

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Pedro", 121);

        // Assert
        Assert.AreEqual(MatchSettingsModificationResult.InvalidTurnTime, result);
        Assert.AreEqual(40, session.TurnTimeSeconds);
    }

    private static MatchSession CreateSession()
    {
        return new MatchSession("ABC123", 40, "Pedro", null);
    }
}
