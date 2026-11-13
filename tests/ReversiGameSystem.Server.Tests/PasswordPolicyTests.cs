using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class PasswordPolicyTests
{
    [TestMethod]
    public void Validate_SevenCharacters_ReturnsTooShort()
    {
        // Arrange
        string password = "Aa1234!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.TooShort, actual);
    }

    [TestMethod]
    public void Validate_EightCharacters_ReturnsNone()
    {
        // Arrange
        string password = "Aa12345!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    [TestMethod]
    public void Validate_AsteriskAsSymbol_ReturnsNone()
    {
        // Arrange
        string password = "Aa12345*";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    [TestMethod]
    public void Validate_UnicodeLettersAndDigits_ReturnsNone()
    {
        // Arrange
        string password = "Áb١٢٣٤٥!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    [TestMethod]
    public void Validate_OneHundredTwentyEightCharacters_ReturnsNone()
    {
        // Arrange
        string password = "Aa1!" + new string('x', 124);

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    [TestMethod]
    public void Validate_OneHundredTwentyNineCharacters_ReturnsTooLong()
    {
        // Arrange
        string password = "Aa1!" + new string('x', 125);

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.TooLong, actual);
    }

    [TestMethod]
    public void Validate_SupplementarySymbols_CountsCodePoints()
    {
        // Arrange
        string password = "Aa12345😀";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    [TestMethod]
    public void Validate_WithoutUppercase_ReturnsMissingUppercaseLetter()
    {
        // Arrange
        string password = "lower123!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingUppercaseLetter, actual);
    }

    [TestMethod]
    public void Validate_WithoutLowercase_ReturnsMissingLowercaseLetter()
    {
        // Arrange
        string password = "UPPER123!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingLowercaseLetter, actual);
    }

    [TestMethod]
    public void Validate_WithoutDigit_ReturnsMissingDigit()
    {
        // Arrange
        string password = "Uppercase!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingDigit, actual);
    }

    [TestMethod]
    public void Validate_WithoutSymbol_ReturnsMissingSymbol()
    {
        // Arrange
        string password = "Upper123";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingSymbol, actual);
    }

    [TestMethod]
    public void Validate_SpaceWithoutSymbol_ReturnsMissingSymbol()
    {
        // Arrange
        string password = "Aa12345 ";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingSymbol, actual);
    }

    [TestMethod]
    public void Validate_ControlCharacter_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\n";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }

    [TestMethod]
    public void Validate_InvisibleFormatCharacter_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\u200B";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }

    [TestMethod]
    public void Validate_LineSeparator_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\u2028";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }

    [TestMethod]
    public void Validate_UnpairedSurrogate_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\uD800";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }
}
