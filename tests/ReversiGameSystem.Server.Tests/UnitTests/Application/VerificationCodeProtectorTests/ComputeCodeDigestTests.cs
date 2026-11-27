using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.VerificationCodeProtectorTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ComputeCodeDigestTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ComputeCodeDigest_BlankIdentityKey_ThrowsArgumentException(string? identityKey)
    {
        VerificationCodeProtector protector = new VerificationCodeProtector(new byte[32]);
        Action action = () => protector.ComputeCodeDigest(identityKey!, "123456");

        Assert.Throws<ArgumentException>(action);
    }

    [TestMethod]
    public void ComputeCodeDigest_SameInputs_ReturnsDeterministicDigest()
    {
        VerificationCodeProtector protector = new VerificationCodeProtector(new byte[32]);
        byte[] expectedDigest = protector.ComputeCodeDigest(
            "login:42:PLAYER@EXAMPLE.COM",
            "000012");

        byte[] actualDigest = protector.ComputeCodeDigest(
            "login:42:PLAYER@EXAMPLE.COM",
            "000012");

        CollectionAssert.AreEqual(expectedDigest, actualDigest);
    }

    [TestMethod]
    public void ComputeCodeDigest_DifferentAccounts_ProducesDistinctDigests()
    {
        VerificationCodeProtector protector = new VerificationCodeProtector(new byte[32]);
        byte[] firstDigest = protector.ComputeCodeDigest(
            VerificationIdentity.ForLogin(42, "player@example.com").Key,
            "000012");

        byte[] secondDigest = protector.ComputeCodeDigest(
            VerificationIdentity.ForLogin(43, "other@example.com").Key,
            "000012");

        Assert.IsFalse(firstDigest.SequenceEqual(secondDigest));
    }

    [TestMethod]
    public void ComputeCodeDigest_DifferentPurposes_ProducesDistinctDigests()
    {
        VerificationCodeProtector protector = new VerificationCodeProtector(new byte[32]);
        byte[] loginDigest = protector.ComputeCodeDigest(
            VerificationIdentity.ForLogin(42, "player@example.com").Key,
            "000012");

        byte[] registrationDigest = protector.ComputeCodeDigest(
            VerificationIdentity.ForRegistration("PlayerOne", "player@example.com").Key,
            "000012");

        Assert.IsFalse(loginDigest.SequenceEqual(registrationDigest));
    }
}
