using System;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Tests;

internal static class ValidationPolicyTestRunner
{
    private static int Main()
    {
        EmailPolicyTests emailTests = new EmailPolicyTests();
        PasswordPolicyTests passwordTests = new PasswordPolicyTests();
        Action[] tests =
        {
            emailTests.Validate_OrdinaryAddress_ReturnsNone,
            emailTests.Validate_TaggedAddress_ReturnsNone,
            emailTests.Validate_InternationalizedDomain_ReturnsNone,
            emailTests.Validate_AddressAtMaximumLength_ReturnsNone,
            emailTests.Validate_AddressAboveMaximumLength_ReturnsTooLong,
            emailTests.Validate_MissingAtSign_ReturnsInvalidStructure,
            emailTests.Validate_MultipleAtSigns_ReturnsInvalidStructure,
            emailTests.Validate_ConsecutiveLocalDots_ReturnsInvalidLocalPart,
            emailTests.Validate_LocalPartAboveMaximum_ReturnsInvalidLocalPart,
            emailTests.Validate_AccentedLocalPart_ReturnsInvalidLocalPart,
            emailTests.Validate_DisplayName_ReturnsInvalidLocalPart,
            emailTests.Validate_SingleLabelDomain_ReturnsInvalidDomain,
            emailTests.Validate_HyphenAtDomainStart_ReturnsInvalidDomain,
            emailTests.Validate_ConsecutiveDomainDots_ReturnsInvalidDomain,
            emailTests.Validate_TrailingDomainDot_ReturnsInvalidDomain,
            emailTests.Validate_DomainLabelAboveMaximum_ReturnsInvalidDomain,
            passwordTests.Validate_SevenCharacters_ReturnsTooShort,
            passwordTests.Validate_EightCharacters_ReturnsNone,
            passwordTests.Validate_AsteriskAsSymbol_ReturnsNone,
            passwordTests.Validate_UnicodeLettersAndDigits_ReturnsNone,
            passwordTests.Validate_OneHundredTwentyEightCharacters_ReturnsNone,
            passwordTests.Validate_OneHundredTwentyNineCharacters_ReturnsTooLong,
            passwordTests.Validate_SupplementarySymbols_CountsCodePoints,
            passwordTests.Validate_WithoutUppercase_ReturnsMissingUppercaseLetter,
            passwordTests.Validate_WithoutLowercase_ReturnsMissingLowercaseLetter,
            passwordTests.Validate_WithoutDigit_ReturnsMissingDigit,
            passwordTests.Validate_WithoutSymbol_ReturnsMissingSymbol,
            passwordTests.Validate_SpaceWithoutSymbol_ReturnsMissingSymbol,
            passwordTests.Validate_ControlCharacter_ReturnsUnsupportedCharacter,
            passwordTests.Validate_LineSeparator_ReturnsUnsupportedCharacter,
            passwordTests.Validate_InvisibleFormatCharacter_ReturnsUnsupportedCharacter,
            passwordTests.Validate_UnpairedSurrogate_ReturnsUnsupportedCharacter
        };

        try
        {
            foreach (Action test in tests)
            {
                test();
            }

            Console.WriteLine($"Passed {tests.Length} validation policy tests.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}

internal sealed class EmailPolicyTests
{
    internal void Validate_OrdinaryAddress_ReturnsNone()
    {
        // Arrange
        string email = "player@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.None, actual);
    }

    internal void Validate_TaggedAddress_ReturnsNone()
    {
        // Arrange
        string email = "first.last+tag@example.co.uk";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.None, actual);
    }

    internal void Validate_InternationalizedDomain_ReturnsNone()
    {
        // Arrange
        string email = "player@bücher.de";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.None, actual);
    }

    internal void Validate_AddressAtMaximumLength_ReturnsNone()
    {
        // Arrange
        string label = new string('a', 61);
        string email = new string('b', 64) + "@" + label + "." + label + "." + label + ".com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.None, actual);
    }

    internal void Validate_AddressAboveMaximumLength_ReturnsTooLong()
    {
        // Arrange
        string email = new string('a', 250) + "@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.TooLong, actual);
    }

    internal void Validate_MissingAtSign_ReturnsInvalidStructure()
    {
        // Arrange
        string email = "player.example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidStructure, actual);
    }

    internal void Validate_MultipleAtSigns_ReturnsInvalidStructure()
    {
        // Arrange
        string email = "player@@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidStructure, actual);
    }

    internal void Validate_ConsecutiveLocalDots_ReturnsInvalidLocalPart()
    {
        // Arrange
        string email = "first..last@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidLocalPart, actual);
    }

    internal void Validate_DisplayName_ReturnsInvalidLocalPart()
    {
        // Arrange
        string email = "Player <player@example.com>";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidLocalPart, actual);
    }

    internal void Validate_LocalPartAboveMaximum_ReturnsInvalidLocalPart()
    {
        // Arrange
        string email = new string('a', 65) + "@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidLocalPart, actual);
    }

    internal void Validate_AccentedLocalPart_ReturnsInvalidLocalPart()
    {
        // Arrange
        string email = "josé@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidLocalPart, actual);
    }

    internal void Validate_SingleLabelDomain_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@localhost";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }

    internal void Validate_HyphenAtDomainStart_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@-example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }

    internal void Validate_ConsecutiveDomainDots_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@example..com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }

    internal void Validate_TrailingDomainDot_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@example.com.";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }

    internal void Validate_DomainLabelAboveMaximum_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@" + new string('a', 64) + ".com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }
}

internal sealed class PasswordPolicyTests
{
    internal void Validate_SevenCharacters_ReturnsTooShort()
    {
        // Arrange
        string password = "Aa1234!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.TooShort, actual);
    }

    internal void Validate_EightCharacters_ReturnsNone()
    {
        // Arrange
        string password = "Aa12345!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_AsteriskAsSymbol_ReturnsNone()
    {
        // Arrange
        string password = "Aa12345*";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_UnicodeLettersAndDigits_ReturnsNone()
    {
        // Arrange
        string password = "Áb١٢٣٤٥!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_OneHundredTwentyEightCharacters_ReturnsNone()
    {
        // Arrange
        string password = "Aa1!" + new string('x', 124);

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_OneHundredTwentyNineCharacters_ReturnsTooLong()
    {
        // Arrange
        string password = "Aa1!" + new string('x', 125);

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.TooLong, actual);
    }

    internal void Validate_SupplementarySymbols_CountsCodePoints()
    {
        // Arrange
        string password = "Aa12345😀";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_WithoutUppercase_ReturnsMissingUppercaseLetter()
    {
        // Arrange
        string password = "lower123!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingUppercaseLetter, actual);
    }

    internal void Validate_WithoutLowercase_ReturnsMissingLowercaseLetter()
    {
        // Arrange
        string password = "UPPER123!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingLowercaseLetter, actual);
    }

    internal void Validate_WithoutDigit_ReturnsMissingDigit()
    {
        // Arrange
        string password = "Uppercase!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingDigit, actual);
    }

    internal void Validate_WithoutSymbol_ReturnsMissingSymbol()
    {
        // Arrange
        string password = "Upper123";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingSymbol, actual);
    }

    internal void Validate_SpaceWithoutSymbol_ReturnsMissingSymbol()
    {
        // Arrange
        string password = "Aa12345 ";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingSymbol, actual);
    }

    internal void Validate_ControlCharacter_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\n";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }

    internal void Validate_InvisibleFormatCharacter_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\u200B";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }

    internal void Validate_LineSeparator_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\u2028";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }

    internal void Validate_UnpairedSurrogate_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\uD800";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }
}

internal static class TestAssert
{
    internal static void AreEqual<TValue>(TValue expected, TValue actual)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, but received {actual}.");
        }
    }
}
