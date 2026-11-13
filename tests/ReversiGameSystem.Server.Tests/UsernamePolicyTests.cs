using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class UsernamePolicyTests
{
    [TestMethod]
    public void Validate_ValidUsername_ReturnsNone()
    {
        // Arrange
        string username = "Pedro123";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.None, actual);
    }

    [TestMethod]
    public void Validate_UsernameWithUnderscore_ReturnsNone()
    {
        // Arrange
        string username = "Pedro_123";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.None, actual);
    }

    [TestMethod]
    public void Validate_EmptyUsername_ReturnsMissing()
    {
        // Arrange
        string username = " ";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.Missing, actual);
    }

    [TestMethod]
    public void Validate_TooShortUsername_ReturnsInvalidLength()
    {
        // Arrange
        string username = "ab";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.InvalidLength, actual);
    }

    [TestMethod]
    public void Validate_TooLongUsername_ReturnsInvalidLength()
    {
        // Arrange
        string username = new string('a', UsernamePolicy.MaximumLength + 1);

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.InvalidLength, actual);
    }

    [TestMethod]
    public void Validate_LeadingUnderscore_ReturnsInvalidCharacters()
    {
        // Arrange
        string username = "_pedro";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.InvalidCharacters, actual);
    }

    [TestMethod]
    public void Validate_UsernameWithSpace_ReturnsInvalidCharacters()
    {
        // Arrange
        string username = "Pedro 123";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.InvalidCharacters, actual);
    }
}
