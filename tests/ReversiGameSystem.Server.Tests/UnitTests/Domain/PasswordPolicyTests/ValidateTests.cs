using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain.PasswordPolicyTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ValidateTests
{
    [TestMethod]
    [DataRow("Aa1234!")]
    [DataRow("Aa123!\U0001F600")]
    public void Validate_BelowMinimumLength_ReturnsTooShort(string password)
    {
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.TooShort;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow("Aa12345!")]
    [DataRow("Aa12345*")]
    [DataRow("Áb١٢٣٤٥!")]
    [DataRow("Aa12345\U0001F600")]
    public void Validate_ValidPassword_ReturnsSuccess(string password)
    {
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.Success;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_AtMaximumLength_ReturnsSuccess()
    {
        string password = "Aa1!" + new string('x', 124);
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.Success;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_AboveMaximumLength_ReturnsTooLong()
    {
        string password = "Aa1!" + new string('x', 125);
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.TooLong;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_WithoutUppercase_ReturnsMissingUppercaseLetter()
    {
        const string password = "lower123!";
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.MissingUppercaseLetter;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_WithoutLowercase_ReturnsMissingLowercaseLetter()
    {
        const string password = "UPPER123!";
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.MissingLowercaseLetter;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_WithoutDigit_ReturnsMissingDigit()
    {
        const string password = "Uppercase!";
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.MissingDigit;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow("Upper123")]
    [DataRow("Aa12345 ")]
    public void Validate_WithoutSymbol_ReturnsMissingSymbol(string password)
    {
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.MissingSymbol;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow("Aa1234!\n")]
    [DataRow("Aa1234!\u200B")]
    [DataRow("Aa1234!\u2028")]
    [DataRow("Aa1234!\u2029")]
    public void Validate_UnsupportedCharacter_ReturnsUnsupportedCharacter(string password)
    {
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.UnsupportedCharacter;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow('\uD800')]
    [DataRow('\uDC00')]
    public void Validate_UnpairedSurrogate_ReturnsUnsupportedCharacter(char surrogate)
    {
        string password = "Aa1234!" + surrogate;
        PasswordValidationStatus expectedStatus = PasswordValidationStatus.UnsupportedCharacter;

        PasswordValidationStatus actualStatus = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedStatus, actualStatus);
    }
}
