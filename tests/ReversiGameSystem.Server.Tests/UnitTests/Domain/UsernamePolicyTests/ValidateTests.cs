using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain.UsernamePolicyTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ValidateTests
{
    [TestMethod]
    [DataRow("Pedro123")]
    [DataRow("Pedro_123")]
    [DataRow("abc")]
    public void Validate_ValidUsername_ReturnsSuccess(string username)
    {
        UsernameValidationStatus expectedStatus = UsernameValidationStatus.Success;

        UsernameValidationStatus actualStatus = UsernamePolicy.Validate(username);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void Validate_AtMaximumLength_ReturnsSuccess()
    {
        string username = new string('a', UsernamePolicy.MaximumLength);
        UsernameValidationStatus expectedStatus = UsernameValidationStatus.Success;

        UsernameValidationStatus actualStatus = UsernamePolicy.Validate(username);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void Validate_MissingOrWhitespace_ReturnsMissing(string username)
    {
        UsernameValidationStatus expectedStatus = UsernameValidationStatus.Missing;

        UsernameValidationStatus actualStatus = UsernamePolicy.Validate(username);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow(UsernamePolicy.MinimumLength - 1)]
    [DataRow(UsernamePolicy.MaximumLength + 1)]
    public void Validate_LengthOutOfRange_ReturnsInvalidLength(int length)
    {
        string username = new string('a', length);
        UsernameValidationStatus expectedStatus = UsernameValidationStatus.InvalidLength;

        UsernameValidationStatus actualStatus = UsernamePolicy.Validate(username);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    [DataRow("_pedro")]
    [DataRow("Pedro 123")]
    [DataRow("Pedro-123")]
    public void Validate_UnsupportedCharacters_ReturnsInvalidCharacters(string username)
    {
        UsernameValidationStatus expectedStatus = UsernameValidationStatus.InvalidCharacters;

        UsernameValidationStatus actualStatus = UsernamePolicy.Validate(username);

        Assert.AreEqual(expectedStatus, actualStatus);
    }
}
