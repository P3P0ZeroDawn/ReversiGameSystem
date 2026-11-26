using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain.EmailPolicyTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ValidateTests
{
    [TestMethod]
    [DataRow("player@example.com")]
    [DataRow("first.last+tag@example.co.uk")]
    [DataRow("player@bücher.de")]
    public void Validate_ValidAddress_ReturnsSuccess(string email)
    {
        EmailValidationStatus expectedStatus = EmailValidationStatus.Success;

        EmailValidationStatus actualStatus = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_AddressAtMaximumLength_ReturnsSuccess()
    {
        string label = new string('a', 61);
        string email = new string('b', 64) + "@" + label + "." + label + "." + label + ".com";
        EmailValidationStatus expectedStatus = EmailValidationStatus.Success;

        EmailValidationStatus actualStatus = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_AddressAboveMaximumLength_ReturnsTooLong()
    {
        string email = new string('a', 250) + "@example.com";
        EmailValidationStatus expectedStatus = EmailValidationStatus.TooLong;

        EmailValidationStatus actualStatus = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_IdnExpansionExceedingMaximumLength_ReturnsTooLong()
    {
        string idnLabel = new string('ü', 40);
        string email = new string('a', 64) + "@" + idnLabel + "." + idnLabel + "."
            + idnLabel + "." + idnLabel + ".com";
        EmailValidationStatus expectedStatus = EmailValidationStatus.TooLong;

        EmailValidationStatus actualStatus = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow("player.example.com")]
    [DataRow("player@@example.com")]
    [DataRow("@example.com")]
    public void Validate_InvalidStructure_ReturnsInvalidStructure(string email)
    {
        EmailValidationStatus expectedStatus = EmailValidationStatus.InvalidStructure;

        EmailValidationStatus actualStatus = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow(".first@example.com")]
    [DataRow("first.@example.com")]
    [DataRow("first..last@example.com")]
    [DataRow("Player <player@example.com>")]
    [DataRow("josé@example.com")]
    public void Validate_InvalidLocalPart_ReturnsInvalidLocalPart(string email)
    {
        EmailValidationStatus expectedStatus = EmailValidationStatus.InvalidLocalPart;

        EmailValidationStatus actualStatus = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_LocalPartAboveMaximum_ReturnsInvalidLocalPart()
    {
        string email = new string('a', 65) + "@example.com";
        EmailValidationStatus expectedStatus = EmailValidationStatus.InvalidLocalPart;

        EmailValidationStatus actualStatus = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow("player@")]
    [DataRow("player@localhost")]
    [DataRow("player@example.c")]
    [DataRow("player@-example.com")]
    [DataRow("player@example-.com")]
    [DataRow("player@example..com")]
    [DataRow("player@example.com.")]
    [DataRow("player@exa_mple.com")]
    public void Validate_InvalidDomain_ReturnsInvalidDomain(string email)
    {
        EmailValidationStatus expectedStatus = EmailValidationStatus.InvalidDomain;

        EmailValidationStatus actualStatus = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_DomainLabelAboveMaximum_ReturnsInvalidDomain()
    {
        string email = "player@" + new string('a', 64) + ".com";
        EmailValidationStatus expectedStatus = EmailValidationStatus.InvalidDomain;

        EmailValidationStatus actualStatus = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedStatus, actualStatus);
    }
}
