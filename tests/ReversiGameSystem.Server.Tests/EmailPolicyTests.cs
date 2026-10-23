using System;
using ReversiGameSystem.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests;

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
