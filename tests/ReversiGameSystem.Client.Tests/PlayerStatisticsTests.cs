using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class PlayerStatisticsTests
{
    [TestMethod]
    public void FromCompletedMatches_Empty_ReturnsZeros()
    {
        // Arrange
        CompletedMatchOutcome[] outcomes = Array.Empty<CompletedMatchOutcome>();

        // Act
        PlayerStatistics actual = PlayerStatistics.FromCompletedMatches(outcomes);

        // Assert
        Assert.AreEqual(0, actual.MatchesPlayed);
        Assert.AreEqual(0, actual.Wins);
        Assert.AreEqual(0, actual.Losses);
        Assert.AreEqual(0, actual.Draws);
        Assert.AreEqual(0d, actual.WinRate);
    }

    [TestMethod]
    public void FromCompletedMatches_MixedResults_CountsEveryOutcome()
    {
        // Arrange
        CompletedMatchOutcome[] outcomes =
        {
            CompletedMatchOutcome.Win,
            CompletedMatchOutcome.Draw,
            CompletedMatchOutcome.Loss,
            CompletedMatchOutcome.Win
        };

        // Act
        PlayerStatistics actual = PlayerStatistics.FromCompletedMatches(outcomes);

        // Assert
        Assert.AreEqual(4, actual.MatchesPlayed);
        Assert.AreEqual(2, actual.Wins);
        Assert.AreEqual(1, actual.Losses);
        Assert.AreEqual(1, actual.Draws);
        Assert.AreEqual(0.5d, actual.WinRate);
    }

    [TestMethod]
    public void FromCompletedMatches_NoWins_ReturnsZeroWinRate()
    {
        // Arrange
        CompletedMatchOutcome[] outcomes =
        {
            CompletedMatchOutcome.Loss,
            CompletedMatchOutcome.Draw
        };

        // Act
        PlayerStatistics actual = PlayerStatistics.FromCompletedMatches(outcomes);

        // Assert
        Assert.AreEqual(2, actual.MatchesPlayed);
        Assert.AreEqual(0d, actual.WinRate);
    }
}
