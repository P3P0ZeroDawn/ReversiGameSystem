using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class UsernamePolicyTests
{
    [TestMethod]
    [DataRow("Pedro123")]
    [DataRow("Pedro_123")]
    public void Validate_ValidUsername_ReturnsSuccess(string username)
    {
        UsernameValidationStatus expectedResult = UsernameValidationStatus.Success;

        UsernameValidationStatus actualResult = UsernamePolicy.Validate(username);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void Validate_EmptyUsername_ReturnsMissing()
    {
        string username = " ";
        UsernameValidationStatus expectedResult = UsernameValidationStatus.Missing;

        UsernameValidationStatus actualResult = UsernamePolicy.Validate(username);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(UsernamePolicy.MaximumLength + 1)]
    public void Validate_LengthOutOfRange_ReturnsInvalidLength(int length)
    {
        string username = new string('a', length);
        UsernameValidationStatus expectedResult = UsernameValidationStatus.InvalidLength;

        UsernameValidationStatus actualResult = UsernamePolicy.Validate(username);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [DataRow("_pedro")]
    [DataRow("Pedro 123")]
    public void Validate_UnsupportedCharacters_ReturnsInvalidCharacters(string username)
    {
        UsernameValidationStatus expectedResult = UsernameValidationStatus.InvalidCharacters;

        UsernameValidationStatus actualResult = UsernamePolicy.Validate(username);

        Assert.AreEqual(expectedResult, actualResult);
    }
}
