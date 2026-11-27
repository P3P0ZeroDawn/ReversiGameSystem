using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.VerificationModelsTests;

[TestClass]
[TestCategory("Unit")]
public sealed class VerificationEvaluationResultTests
{
    [TestMethod]
    public void FailureConstructor_AcceptedStatus_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = new VerificationEvaluationResult(VerificationStatus.Accepted);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void AcceptedConstructor_ValidTimestamp_SetsAcceptedStatus()
    {
        DateTime usedAtUtc = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        VerificationStatus expectedStatus = VerificationStatus.Accepted;

        VerificationEvaluationResult actualResult = new VerificationEvaluationResult(usedAtUtc);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void AcceptedConstructor_ValidTimestamp_ExposesMarkUsedAtUtc()
    {
        DateTime expectedUsedAtUtc = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

        VerificationEvaluationResult actualResult = new VerificationEvaluationResult(
            expectedUsedAtUtc);

        Assert.AreEqual(expectedUsedAtUtc, actualResult.MarkUsedAtUtc);
    }

    [TestMethod]
    public void FailedAttemptConstructor_WithInvalidation_SetsInvalidStatusAndTimestamp()
    {
        DateTime expectedInvalidatedAtUtc = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        VerificationStatus expectedStatus = VerificationStatus.Invalid;

        VerificationEvaluationResult actualResult = new VerificationEvaluationResult(
            5,
            expectedInvalidatedAtUtc);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedInvalidatedAtUtc, actualResult.InvalidatedAtUtc);
    }
}
