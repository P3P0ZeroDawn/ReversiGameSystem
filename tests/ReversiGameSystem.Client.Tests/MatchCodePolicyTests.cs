using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class MatchCodePolicyTests
{
    [TestMethod]
    public void TryNormalize_LowercaseCode_ReturnsUppercaseCode()
    {
        // Arrange

        // Act
        bool valid = MatchCodePolicy.TryNormalize("an4b2c", out string code);

        // Assert
        Assert.AreEqual(true, valid);
        Assert.AreEqual("AN4B2C", code);
    }

    [TestMethod]
    public void TryNormalize_IncorrectLength_ReturnsFalse()
    {
        // Arrange

        // Act

        // Assert
        Assert.AreEqual(false, MatchCodePolicy.TryNormalize("ABC12", out _));
        Assert.AreEqual(false, MatchCodePolicy.TryNormalize("ABC2345", out _));
    }

    [TestMethod]
    public void TryNormalize_AmbiguousCharacters_ReturnsFalse()
    {
        // Arrange

        // Act

        // Assert
        Assert.AreEqual(false, MatchCodePolicy.TryNormalize("ABCI01", out _));
    }
}
