using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.VerificationCodeProtectorTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ConstructorTests
{
    [TestMethod]
    public void Constructor_NullKey_ThrowsArgumentException()
    {
        Action action = () => _ = new VerificationCodeProtector(null!);

        Assert.Throws<ArgumentException>(action);
    }

    [TestMethod]
    public void Constructor_ShortKey_ThrowsArgumentException()
    {
        Action action = () => _ = new VerificationCodeProtector(new byte[31]);

        Assert.Throws<ArgumentException>(action);
    }

    [TestMethod]
    public void Constructor_ValidKey_ClonesInputKeyArray()
    {
        byte[] key = new byte[32];
        VerificationCodeProtector protector = new VerificationCodeProtector(key);
        byte[] expectedDigest = protector.ComputeCodeDigest(
            "login:42:PLAYER@EXAMPLE.COM",
            "123456");

        key[0] = 0xFF;
        byte[] actualDigest = protector.ComputeCodeDigest(
            "login:42:PLAYER@EXAMPLE.COM",
            "123456");

        CollectionAssert.AreEqual(expectedDigest, actualDigest);
    }
}
