using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class MatchCodePolicyTests
{
    internal void TryNormalize_LowercaseCode_ReturnsUppercaseCode()
    {
        // Arrange

        // Act
        bool valid = MatchCodePolicy.TryNormalize("an4b2c", out string code);

        // Assert
        TestAssert.AreEqual(true, valid);
        TestAssert.AreEqual("AN4B2C", code);
    }

    internal void TryNormalize_IncorrectLength_ReturnsFalse()
    {
        // Arrange

        // Act

        // Assert
        TestAssert.AreEqual(false, MatchCodePolicy.TryNormalize("ABC12", out _));
        TestAssert.AreEqual(false, MatchCodePolicy.TryNormalize("ABC2345", out _));
    }

    internal void TryNormalize_AmbiguousCharacters_ReturnsFalse()
    {
        // Arrange

        // Act

        // Assert
        TestAssert.AreEqual(false, MatchCodePolicy.TryNormalize("ABCI01", out _));
    }
}
