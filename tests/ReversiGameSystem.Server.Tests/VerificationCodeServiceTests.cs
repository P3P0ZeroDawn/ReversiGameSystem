using System;
using System.Linq;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests;

internal sealed class VerificationCodeServiceTests
{
    private static VerificationIdentity Identity =>
        VerificationIdentity.ForLogin(42, "player@example.com");

    internal void Request_ValidIdentity_DeliversSixDigitsWithFiveMinuteExpiry()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        DateTime expiration = context.Codes.Request(Identity);
        // Assert
        VerificationEmail email = TestAssert.IsNotNull(context.Email);
        TestAssert.AreEqual(context.UtcNow.AddMinutes(5), expiration);
        TestAssert.AreEqual(expiration, email.ExpiresAtUtc);
        TestAssert.AreEqual("player@example.com", email.RecipientAddress);
        TestAssert.IsTrue(email.Code.Length == 6 && email.Code.All(c => c >= '0' && c <= '9'),
            "A verification code must contain exactly six ASCII digits.");
        PendingVerificationCode pending = TestAssert.IsNotNull(context.Pending);
        TestAssert.IsTrue(pending.Digest.SequenceEqual(
            context.Protector.Protect(Identity.Key, email.Code)),
            "The persisted digest must authenticate the delivered code and its identity.");
    }

    internal void Request_DeliveryTimeout_DoesNotReplacePendingCode()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        context.Codes.Request(Identity);
        PendingVerificationCode? previous = context.Pending;
        context.FailDelivery = true;
        // Act
        try
        {
            context.Codes.Request(Identity);
            throw new InvalidOperationException("Expected delivery failure.");
        }
        catch (TimeoutException) { }
        // Assert
        TestAssert.IsTrue(ReferenceEquals(previous, context.Pending),
            "Failed delivery must preserve pending state.");
    }

    internal void Consume_ExpiredRepositoryResult_PreservesRejectionAndServerTime()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext
        {
            ConsumeStatus = VerificationStatus.Expired
        };
        context.UtcNow = context.UtcNow.AddMinutes(5);
        // Act
        VerificationStatus result = context.Codes.Consume(Identity, "000012");
        // Assert
        TestAssert.AreEqual(VerificationStatus.Expired, result);
        VerificationAttempt attempt = TestAssert.IsNotNull(context.Attempt);
        TestAssert.AreEqual(context.UtcNow, attempt.NowUtc);
        TestAssert.IsTrue(attempt.Digest.SequenceEqual(
            context.Protector.Protect(Identity.Key, "000012")),
            "Leading zeroes must survive verification.");
    }

    internal void Protect_DifferentIdentity_BindsCodeToAccountAndPurpose()
    {
        // Arrange
        VerificationCodeProtector protector = new VerificationCodeProtector(new byte[32]);
        // Act
        byte[] login = protector.Protect(Identity.Key, "000012");
        byte[] other = protector.Protect(
            VerificationIdentity.ForLogin(43, "other@example.com").Key, "000012");
        byte[] registration = protector.Protect(
            VerificationIdentity.ForRegistration("PlayerOne", "player@example.com").Key, "000012");
        // Assert
        TestAssert.IsTrue(!login.SequenceEqual(other) && !login.SequenceEqual(registration),
            "A code digest must not transfer between accounts or purposes.");
    }
}
