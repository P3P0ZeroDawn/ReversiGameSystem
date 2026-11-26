using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PasswordHasherTests;

[TestClass]
[TestCategory("Unit")]
public sealed class VerifyTests
{
    [TestMethod]
    public void Verify_MatchingPassword_ReturnsTrue()
    {
        string encodedHash = PasswordHasher.Hash("Password1!");

        bool actualVerified = PasswordHasher.Verify("Password1!", encodedHash);

        Assert.IsTrue(actualVerified);
    }

    [TestMethod]
    public void Verify_DecomposedUnicodePassword_MatchesNormalizedHash()
    {
        string encodedHash = PasswordHasher.Hash("\u00C1b12345!");

        bool actualVerified = PasswordHasher.Verify("A\u0301b12345!", encodedHash);

        Assert.IsTrue(actualVerified);
    }

    [TestMethod]
    public void Verify_DifferentPassword_ReturnsFalse()
    {
        string encodedHash = PasswordHasher.Hash("Password1!");

        bool actualVerified = PasswordHasher.Verify("Wrong1!", encodedHash);

        Assert.IsFalse(actualVerified);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Verify_NullOrWhitespaceHash_ReturnsFalse(string? encodedHash)
    {
        bool actualVerified = PasswordHasher.Verify("Password1!", encodedHash!);

        Assert.IsFalse(actualVerified);
    }

    [TestMethod]
    public void Verify_NullPassword_ReturnsFalse()
    {
        string encodedHash = PasswordHasher.Hash("Password1!");

        bool actualVerified = PasswordHasher.Verify(null, encodedHash);

        Assert.IsFalse(actualVerified);
    }

    [TestMethod]
    [DataRow("PBKDF2-SHA256$100000$onlythree")]
    [DataRow("PBKDF2-SHA512$100000$AAAAAAAAAAAAAAAAAAAAAA==$AAAA")]
    [DataRow("PBKDF2-SHA256$0$AAAAAAAAAAAAAAAAAAAAAA==$AAAA")]
    [DataRow("PBKDF2-SHA256$1000001$AAAAAAAAAAAAAAAAAAAAAA==$AAAA")]
    [DataRow("PBKDF2-SHA256$100000$not-base64!$AAAA")]
    [DataRow("PBKDF2-SHA256$100000$AAAA$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [DataRow("PBKDF2-SHA256$100000$AAAAAAAAAAAAAAAAAAAAAA==$AAAA")]
    public void Verify_MalformedHash_ReturnsFalse(string encodedHash)
    {
        bool actualVerified = PasswordHasher.Verify("Password1!", encodedHash);

        Assert.IsFalse(actualVerified);
    }
}
