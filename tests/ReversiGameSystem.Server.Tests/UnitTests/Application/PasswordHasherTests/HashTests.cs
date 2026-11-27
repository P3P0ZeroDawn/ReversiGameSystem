using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PasswordHasherTests;

[TestClass]
[TestCategory("Unit")]
public sealed class HashTests
{
    [TestMethod]
    public void Hash_NullPassword_ThrowsArgumentNullException()
    {
        Action action = () => PasswordHasher.Hash(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Hash_ValidPassword_UsesExpectedPbkdf2Sha256Prefix()
    {
        string expectedPrefix = "PBKDF2-SHA256$100000$";

        string actualHash = PasswordHasher.Hash("Password1!");

        Assert.IsTrue(actualHash.StartsWith(expectedPrefix, StringComparison.Ordinal));
    }

    [TestMethod]
    public void Hash_SamePasswordTwice_GeneratesDistinctSalts()
    {
        string firstHash = PasswordHasher.Hash("Password1!");
        string secondHash = PasswordHasher.Hash("Password1!");

        Assert.AreNotEqual(firstHash, secondHash);
    }
}
