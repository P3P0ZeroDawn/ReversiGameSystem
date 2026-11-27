using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class VerificationCodePolicyTests
{
    [TestMethod]
    [DataRow("123456", true)]
    [DataRow("000000", true)]
    [DataRow("12345", false)]
    [DataRow("1234567", false)]
    [DataRow("12a456", false)]
    [DataRow("12 456", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void IsValid_CodeInput_RequiresSixAsciiDigits(string? code, bool expected)
    {
        bool actual = VerificationCodePolicy.IsValid(code);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void MaximumInputLength_ValidUnicodePassword_DoesNotTruncateSurrogatePairs()
    {
        string password = "Aa1!" + string.Concat(System.Linq.Enumerable.Repeat("😀", 124));

        PasswordValidationError status = PasswordPolicy.Validate(password);

        Assert.AreEqual(PasswordValidationError.None, status);
        Assert.IsTrue(password.Length <= PasswordPolicy.MaximumInputLength);
    }
}
