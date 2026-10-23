using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class AvailableMatchesServiceTests
{
    internal void FilterJoinable_MixedMatches_ReturnsOnlyOpenHumanMatches()
    {
        // Arrange
        AvailableMatch[] matches =
        {
            new AvailableMatch(
                "open", "AB2C3D", "Ana", AvailableMatchState.Waiting, 1, false, 40),
            new AvailableMatch(
                "full", "EF4G5H", "Luis", AvailableMatchState.Waiting, 2, false, 40),
            new AvailableMatch(
                "system", "JK6L7M", "Gael", AvailableMatchState.Waiting, 1, true, 40),
            new AvailableMatch(
                "playing", "NP8Q9R", "Luna", AvailableMatchState.InProgress, 1, false, 40),
            new AvailableMatch(
                "own", "ST2U3V", "Pedro", AvailableMatchState.Waiting, 1, false, 40)
        };

        // Act
        IReadOnlyList<AvailableMatch> actual =
            AvailableMatchesService.FilterJoinable(matches, "Pedro");

        // Assert
        TestAssert.AreEqual(1, actual.Count);
        TestAssert.AreEqual("open", actual[0].Id);
    }
}
