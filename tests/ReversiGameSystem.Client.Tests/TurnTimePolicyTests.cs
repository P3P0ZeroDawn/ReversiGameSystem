using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class TurnTimePolicyTests
{
    internal void TryParse_DefaultValue_ReturnsTrue()
    {
        // Arrange

        // Act
        bool valid = TurnTimePolicy.TryParse("40", out int seconds);

        // Assert
        TestAssert.AreEqual(true, valid);
        TestAssert.AreEqual(40, seconds);
    }

    internal void TryParse_MinimumAndMaximum_ReturnTrue()
    {
        // Arrange

        // Act

        // Assert
        TestAssert.AreEqual(true, TurnTimePolicy.TryParse("10", out _));
        TestAssert.AreEqual(true, TurnTimePolicy.TryParse("120", out _));
    }

    internal void TryParse_OutsideRange_ReturnsFalse()
    {
        // Arrange

        // Act

        // Assert
        TestAssert.AreEqual(false, TurnTimePolicy.TryParse("9", out _));
        TestAssert.AreEqual(false, TurnTimePolicy.TryParse("121", out _));
    }

    internal void TryParse_NonInteger_ReturnsFalse()
    {
        // Arrange

        // Act

        // Assert
        TestAssert.AreEqual(false, TurnTimePolicy.TryParse("40.5", out _));
        TestAssert.AreEqual(false, TurnTimePolicy.TryParse("forty", out _));
    }
}
