using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class VerificationCodeServiceTests
{
    private static VerificationIdentity Identity =>
        VerificationIdentity.ForLogin(42, "player@example.com");

    [TestMethod]
    public void Request_ValidIdentity_DeliversSixDigitsWithFiveMinuteExpiry()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        DateTime expiration = context.Codes.Request(Identity);
        // Assert
        ReversiGameSystem.Server.Application.Common.EmailMessage email =
            TestAssert.IsNotNull(context.Email);
        Assert.AreEqual(context.UtcNow.AddMinutes(5), expiration);
        Assert.AreEqual("player@example.com", email.RecipientAddress);
        System.Text.RegularExpressions.Match codeMatch =
            System.Text.RegularExpressions.Regex.Match(email.Body, @"\b(\d{6})\b");
        Assert.IsTrue(codeMatch.Success, "A verification code must contain exactly six ASCII digits.");
        string code = codeMatch.Groups[1].Value;
        PendingVerificationCode pending = TestAssert.IsNotNull(context.Pending);
        Assert.AreEqual(expiration, pending.ExpiresAtUtc);
        Assert.IsTrue(pending.Digest.SequenceEqual(
            context.Protector.Protect(Identity.Key, code)),
            "The persisted digest must authenticate the delivered code and its identity.");
    }

    [TestMethod]
    public void Request_DeliveryTimeout_DoesNotReplacePendingCode()
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
        Assert.IsTrue(ReferenceEquals(previous, context.Pending),
            "Failed delivery must preserve pending state.");
    }

    [TestMethod]
    public void Consume_ExpiredRepositoryResult_PreservesRejectionAndServerTime()
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
        Assert.AreEqual(VerificationStatus.Expired, result);
        VerificationAttempt attempt = TestAssert.IsNotNull(context.Attempt);
        Assert.AreEqual(context.UtcNow, attempt.NowUtc);
        Assert.IsTrue(attempt.Digest.SequenceEqual(
            context.Protector.Protect(Identity.Key, "000012")),
            "Leading zeroes must survive verification.");
    }

    [TestMethod]
    public void Protect_DifferentIdentity_BindsCodeToAccountAndPurpose()
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
        Assert.IsTrue(!login.SequenceEqual(other) && !login.SequenceEqual(registration),
            "A code digest must not transfer between accounts or purposes.");
    }
}
