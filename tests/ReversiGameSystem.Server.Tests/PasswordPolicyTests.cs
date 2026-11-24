using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class PasswordPolicyTests
{
    [TestMethod]
    public void Validate_SevenCharacters_ReturnsTooShort()
    {
        string password = "Aa1234!";
        PasswordValidationStatus expectedResult = PasswordValidationStatus.TooShort;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [DataRow("Aa12345!")]
    [DataRow("Aa12345*")]
    [DataRow("Áb١٢٣٤٥!")]
    [DataRow("Aa12345😀")]
    public void Validate_ValidPassword_ReturnsSuccess(string password)
    {
        PasswordValidationStatus expectedResult = PasswordValidationStatus.Success;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_OneHundredTwentyEightCharacters_ReturnsSuccess()
    {
        string password = "Aa1!" + new string('x', 124);
        PasswordValidationStatus expectedResult = PasswordValidationStatus.Success;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_OneHundredTwentyNineCharacters_ReturnsTooLong()
    {
        string password = "Aa1!" + new string('x', 125);
        PasswordValidationStatus expectedResult = PasswordValidationStatus.TooLong;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_WithoutUppercase_ReturnsMissingUppercaseLetter()
    {
        string password = "lower123!";
        PasswordValidationStatus expectedResult = PasswordValidationStatus.MissingUppercaseLetter;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_WithoutLowercase_ReturnsMissingLowercaseLetter()
    {
        string password = "UPPER123!";
        PasswordValidationStatus expectedResult = PasswordValidationStatus.MissingLowercaseLetter;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_WithoutDigit_ReturnsMissingDigit()
    {
        string password = "Uppercase!";
        PasswordValidationStatus expectedResult = PasswordValidationStatus.MissingDigit;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [DataRow("Upper123")]
    [DataRow("Aa12345 ")]
    public void Validate_WithoutSymbol_ReturnsMissingSymbol(string password)
    {
        PasswordValidationStatus expectedResult = PasswordValidationStatus.MissingSymbol;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [DataRow("Aa1234!\n")]
    [DataRow("Aa1234!\u200B")]
    [DataRow("Aa1234!\u2028")]
    public void Validate_UnsupportedCharacter_ReturnsUnsupportedCharacter(string password)
    {
        PasswordValidationStatus expectedResult = PasswordValidationStatus.UnsupportedCharacter;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_UnpairedSurrogate_ReturnsUnsupportedCharacter()
    {
        string password = "Aa1234!\uD800";
        PasswordValidationStatus expectedResult = PasswordValidationStatus.UnsupportedCharacter;

        PasswordValidationStatus actualResult = PasswordPolicy.Validate(password);

        Assert.AreEqual(expectedResult, actualResult);
    }
}
