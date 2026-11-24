using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class GuestUsernamePolicyTests
{
    [TestMethod]
    public void Validate_ValidName_ReturnsNone()
    {
        // Arrange

        // Act

        // Assert
        Assert.AreEqual(
            UsernameValidationError.None,
            GuestUsernamePolicy.Validate("Pedro_27"));
    }

    [TestMethod]
    public void Validate_WhitespaceOnly_ReturnsMissing()
    {
        // Arrange

        // Act

        // Assert
        Assert.AreEqual(
            UsernameValidationError.Missing,
            GuestUsernamePolicy.Validate("   "));
    }

    [TestMethod]
    public void Normalize_NameWithOuterWhitespace_ReturnsBaseName()
    {
        // Arrange

        // Act

        // Assert
        Assert.AreEqual(
            "Pedro",
            GuestUsernamePolicy.Normalize("  Pedro  "));
    }
}
