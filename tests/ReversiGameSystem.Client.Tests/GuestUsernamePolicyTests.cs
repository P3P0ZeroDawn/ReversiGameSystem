using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class GuestUsernamePolicyTests
{
    internal void Validate_ValidName_ReturnsNone()
    {
        // Arrange

        // Act

        // Assert
        TestAssert.AreEqual(
            UsernameValidationError.None,
            GuestUsernamePolicy.Validate("Pedro_27"));
    }

    internal void Validate_WhitespaceOnly_ReturnsMissing()
    {
        // Arrange

        // Act

        // Assert
        TestAssert.AreEqual(
            UsernameValidationError.Missing,
            GuestUsernamePolicy.Validate("   "));
    }

    internal void Normalize_NameWithOuterWhitespace_ReturnsBaseName()
    {
        // Arrange

        // Act

        // Assert
        TestAssert.AreEqual(
            "Pedro",
            GuestUsernamePolicy.Normalize("  Pedro  "));
    }
}
