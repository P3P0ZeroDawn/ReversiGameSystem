using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.VerificationCodeServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class VerifyCodeTests : VerificationCodeServiceTestBase
{
    [TestMethod]
    public void VerifyCode_NullIdentity_ThrowsArgumentNullException()
    {
        Action action = () => Service.VerifyCode(null!, "000012");

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void VerifyCode_NonExistentCode_ReturnsInvalid()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        Repository.StoredCode = new StoredVerificationCode();
        VerificationStatus expectedStatus = VerificationStatus.Invalid;

        VerificationStatus actualStatus = Service.VerifyCode(identity, "000012");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void VerifyCode_AlreadyUsedCode_ReturnsInvalid()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        Repository.StoredCode = CreateStoredCode(
            identity,
            usedAtUtc: Clock.UtcNow.AddMinutes(-1));
        VerificationStatus expectedStatus = VerificationStatus.Invalid;

        VerificationStatus actualStatus = Service.VerifyCode(identity, "000012");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void VerifyCode_AlreadyInvalidatedCode_ReturnsInvalid()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        Repository.StoredCode = CreateStoredCode(
            identity,
            invalidatedAtUtc: Clock.UtcNow.AddMinutes(-1));
        VerificationStatus expectedStatus = VerificationStatus.Invalid;

        VerificationStatus actualStatus = Service.VerifyCode(identity, "000012");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void VerifyCode_MaxFailedAttemptsReached_ReturnsInvalid()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        Repository.StoredCode = CreateStoredCode(identity, failedAttempts: 5);
        VerificationStatus expectedStatus = VerificationStatus.Invalid;

        VerificationStatus actualStatus = Service.VerifyCode(identity, "000012");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void VerifyCode_ExactExpiration_ReturnsExpired()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        Repository.StoredCode = CreateStoredCode(identity, expiresAtUtc: Clock.UtcNow);
        VerificationStatus expectedStatus = VerificationStatus.Expired;

        VerificationStatus actualStatus = Service.VerifyCode(identity, "000012");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void VerifyCode_SubmittedCodeLongerThanSixChars_ReturnsInvalid()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        Repository.StoredCode = CreateStoredCode(identity, code: "000012");
        VerificationStatus expectedStatus = VerificationStatus.Invalid;

        VerificationStatus actualStatus = Service.VerifyCode(identity, "0000129");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void VerifyCode_WrongCodeBeforeLimit_IncrementsFailedAttemptsWithoutInvalidation()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        Repository.StoredCode = CreateStoredCode(identity, failedAttempts: 2);
        int? expectedFailedAttempts = 3;
        DateTime? expectedInvalidatedAtUtc = null;

        Service.VerifyCode(identity, "999999");

        Assert.AreEqual(
            expectedFailedAttempts,
            Repository.LastEvaluationResult?.UpdatedFailedAttempts);
        Assert.AreEqual(
            expectedInvalidatedAtUtc,
            Repository.LastEvaluationResult?.InvalidatedAtUtc);
    }

    [TestMethod]
    public void VerifyCode_FifthWrongCode_InvalidatesAtEvaluationTime()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        Repository.StoredCode = CreateStoredCode(identity, failedAttempts: 4);
        DateTime? expectedInvalidatedAtUtc = Clock.UtcNow;

        Service.VerifyCode(identity, "999999");

        Assert.AreEqual(
            expectedInvalidatedAtUtc,
            Repository.LastEvaluationResult?.InvalidatedAtUtc);
    }

    [TestMethod]
    public void VerifyCode_MatchingCode_AcceptsAndMarksUsedAtEvaluationTime()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        Repository.StoredCode = CreateStoredCode(identity, code: "000012");
        VerificationStatus expectedStatus = VerificationStatus.Accepted;
        DateTime? expectedUsedAtUtc = Clock.UtcNow;

        VerificationStatus actualStatus = Service.VerifyCode(identity, "000012");

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedUsedAtUtc, Repository.LastEvaluationResult?.MarkUsedAtUtc);
    }
}
