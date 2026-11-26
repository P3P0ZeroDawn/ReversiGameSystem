using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class EmailPolicyTests
{
    [TestMethod]
    [DataRow("player@example.com")]
    [DataRow("first.last+tag@example.co.uk")]
    [DataRow("player@bücher.de")]
    public void Validate_ValidAddress_ReturnsSuccess(string email)
    {
        EmailValidationStatus expectedResult = EmailValidationStatus.Success;

        EmailValidationStatus actualResult = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_AddressAtMaximumLength_ReturnsSuccess()
    {
        string label = new string('a', 61);
        string email = new string('b', 64) + "@" + label + "." + label + "." + label + ".com";
        EmailValidationStatus expectedResult = EmailValidationStatus.Success;

        EmailValidationStatus actualResult = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_AddressAboveMaximumLength_ReturnsTooLong()
    {
        string email = new string('a', 250) + "@example.com";
        EmailValidationStatus expectedResult = EmailValidationStatus.TooLong;

        EmailValidationStatus actualResult = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [DataRow("player.example.com")]
    [DataRow("player@@example.com")]
    public void Validate_InvalidStructure_ReturnsInvalidStructure(string email)
    {
        EmailValidationStatus expectedResult = EmailValidationStatus.InvalidStructure;

        EmailValidationStatus actualResult = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [DataRow("first..last@example.com")]
    [DataRow("Player <player@example.com>")]
    [DataRow("josé@example.com")]
    public void Validate_InvalidLocalPart_ReturnsInvalidLocalPart(string email)
    {
        EmailValidationStatus expectedResult = EmailValidationStatus.InvalidLocalPart;

        EmailValidationStatus actualResult = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_LocalPartAboveMaximum_ReturnsInvalidLocalPart()
    {
        string email = new string('a', 65) + "@example.com";
        EmailValidationStatus expectedResult = EmailValidationStatus.InvalidLocalPart;

        EmailValidationStatus actualResult = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [DataRow("player@localhost")]
    [DataRow("player@-example.com")]
    [DataRow("player@example..com")]
    [DataRow("player@example.com.")]
    public void Validate_InvalidDomain_ReturnsInvalidDomain(string email)
    {
        EmailValidationStatus expectedResult = EmailValidationStatus.InvalidDomain;

        EmailValidationStatus actualResult = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_DomainLabelAboveMaximum_ReturnsInvalidDomain()
    {
        string email = "player@" + new string('a', 64) + ".com";
        EmailValidationStatus expectedResult = EmailValidationStatus.InvalidDomain;

        EmailValidationStatus actualResult = EmailPolicy.Validate(email);

        Assert.AreEqual(expectedResult, actualResult);
    }
}
