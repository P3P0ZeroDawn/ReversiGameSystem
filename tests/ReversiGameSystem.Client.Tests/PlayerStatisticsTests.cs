using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class PlayerStatisticsTests
{
    internal void FromCompletedMatches_Empty_ReturnsZeros()
    {
        // Arrange
        CompletedMatchOutcome[] outcomes = Array.Empty<CompletedMatchOutcome>();

        // Act
        PlayerStatistics actual = PlayerStatistics.FromCompletedMatches(outcomes);

        // Assert
        TestAssert.AreEqual(0, actual.MatchesPlayed);
        TestAssert.AreEqual(0, actual.Wins);
        TestAssert.AreEqual(0, actual.Losses);
        TestAssert.AreEqual(0, actual.Draws);
        TestAssert.AreEqual(0d, actual.WinRate);
    }

    internal void FromCompletedMatches_MixedResults_CountsEveryOutcome()
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
        TestAssert.AreEqual(4, actual.MatchesPlayed);
        TestAssert.AreEqual(2, actual.Wins);
        TestAssert.AreEqual(1, actual.Losses);
        TestAssert.AreEqual(1, actual.Draws);
        TestAssert.AreEqual(0.5d, actual.WinRate);
    }

    internal void FromCompletedMatches_NoWins_ReturnsZeroWinRate()
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
        TestAssert.AreEqual(2, actual.MatchesPlayed);
        TestAssert.AreEqual(0d, actual.WinRate);
    }
}
