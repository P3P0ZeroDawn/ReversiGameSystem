using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class TurnTimePolicyTests
{
    [TestMethod]
    public void TryParse_DefaultValue_ReturnsTrue()
    {
        // Arrange

        // Act
        bool valid = TurnTimePolicy.TryParse("40", out int seconds);

        // Assert
        Assert.AreEqual(true, valid);
        Assert.AreEqual(40, seconds);
    }

    [TestMethod]
    public void TryParse_MinimumAndMaximum_ReturnTrue()
    {
        // Arrange

        // Act

        // Assert
        Assert.AreEqual(true, TurnTimePolicy.TryParse("10", out _));
        Assert.AreEqual(true, TurnTimePolicy.TryParse("120", out _));
    }

    [TestMethod]
    public void TryParse_OutsideRange_ReturnsFalse()
    {
        // Arrange

        // Act

        // Assert
        Assert.AreEqual(false, TurnTimePolicy.TryParse("9", out _));
        Assert.AreEqual(false, TurnTimePolicy.TryParse("121", out _));
    }

    [TestMethod]
    public void TryParse_NonInteger_ReturnsFalse()
    {
        // Arrange

        // Act

        // Assert
        Assert.AreEqual(false, TurnTimePolicy.TryParse("40.5", out _));
        Assert.AreEqual(false, TurnTimePolicy.TryParse("forty", out _));
    }
}
