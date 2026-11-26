using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class VerificationCodeServiceTests
{
    private static VerificationIdentity Identity =>
        VerificationIdentity.ForLogin(42, "player@example.com");

    [TestMethod]
    public void IssueVerificationCode_ValidIdentity_DeliversSixDigitsWithFiveMinuteExpiry()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        DateTime expiration = context.Codes.IssueVerificationCode(Identity);
        // Assert
        EmailMessage email = TestAssert.IsNotNull(context.Email);
        Assert.AreEqual(context.UtcNow.AddMinutes(5), expiration);
        Assert.AreEqual("player@example.com", email.RecipientAddress);
        Assert.IsTrue(email.IsHtml);
        Match codeMatch = Regex.Match(email.Body, @"(?<!#)\b(\d{6})\b");
        Assert.IsTrue(codeMatch.Success, "A verification code must contain exactly six ASCII digits.");
        string code = codeMatch.Groups[1].Value;
        PendingVerificationCode pending = TestAssert.IsNotNull(context.Pending);
        Assert.AreEqual(expiration, pending.ExpiresAtUtc);
        Assert.IsTrue(pending.Digest.SequenceEqual(
            context.Protector.ComputeCodeDigest(Identity.Key, code)),
            "The persisted digest must authenticate the delivered code and its identity.");
    }

    [TestMethod]
    public void IssueVerificationCode_DeliveryTimeout_DoesNotReplacePendingCode()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        context.Codes.IssueVerificationCode(Identity);
        PendingVerificationCode? previous = context.Pending;
        context.FailDelivery = true;
        // Act
        try
        {
            context.Codes.IssueVerificationCode(Identity);
            throw new InvalidOperationException("Expected delivery failure.");
        }
        catch (TimeoutException) { }
        // Assert
        Assert.IsTrue(ReferenceEquals(previous, context.Pending),
            "Failed delivery must preserve pending state.");
    }

    [TestMethod]
    public void VerifyCode_ExactExpiry_RejectsExpiredCode()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        context.Codes.IssueVerificationCode(Identity);
        string deliveredCode = ExtractDeliveredCode(context);
        context.UtcNow = context.UtcNow.AddMinutes(5);
        // Act
        VerificationStatus result = context.Codes.VerifyCode(Identity, deliveredCode);
        // Assert
        Assert.AreEqual(VerificationStatus.Expired, result);
    }

    [TestMethod]
    public void VerifyCode_FiveFailures_InvalidatesCodeOnFifthAttempt()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        context.Codes.IssueVerificationCode(Identity);
        string deliveredCode = ExtractDeliveredCode(context);
        // Act
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Assert.AreEqual(
                VerificationStatus.Invalid,
                context.Codes.VerifyCode(Identity, "999999"));
        }
        // Assert
        StoredVerificationCode stored = TestAssert.IsNotNull(context.Stored);
        Assert.AreEqual(5, stored.FailedAttempts);
        Assert.IsNotNull(stored.InvalidatedAtUtc);
        Assert.AreEqual(
            VerificationStatus.Invalid,
            context.Codes.VerifyCode(Identity, deliveredCode));
    }

    [TestMethod]
    public void VerifyCode_ValidCodeUsedOnce_RejectsReuse()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        context.Codes.IssueVerificationCode(Identity);
        string deliveredCode = ExtractDeliveredCode(context);
        // Act
        VerificationStatus firstAttempt = context.Codes.VerifyCode(Identity, deliveredCode);
        VerificationStatus secondAttempt = context.Codes.VerifyCode(Identity, deliveredCode);
        // Assert
        Assert.AreEqual(VerificationStatus.Accepted, firstAttempt);
        Assert.AreEqual(VerificationStatus.Invalid, secondAttempt);
    }

    [TestMethod]
    public void ComputeCodeDigest_DifferentIdentity_BindsCodeToAccountAndPurpose()
    {
        // Arrange
        VerificationCodeProtector protector = new VerificationCodeProtector(new byte[32]);
        // Act
        byte[] login = protector.ComputeCodeDigest(Identity.Key, "000012");
        byte[] other = protector.ComputeCodeDigest(
            VerificationIdentity.ForLogin(43, "other@example.com").Key, "000012");
        byte[] registration = protector.ComputeCodeDigest(
            VerificationIdentity.ForRegistration("PlayerOne", "player@example.com").Key, "000012");
        // Assert
        Assert.IsTrue(!login.SequenceEqual(other) && !login.SequenceEqual(registration),
            "A code digest must not transfer between accounts or purposes.");
    }

    [TestMethod]
    public void IssueVerificationCode_SpanishAndDefaultLocales_RenderSingleLanguageEmails()
    {
        // Arrange
        SessionTestContext spanishContext = new SessionTestContext();
        SessionTestContext fallbackContext = new SessionTestContext();
        // Act
        spanishContext.Codes.IssueVerificationCode(
            VerificationIdentity.ForLogin(42, "player@example.com", "es-MX"));
        fallbackContext.Codes.IssueVerificationCode(
            VerificationIdentity.ForLogin(42, "player@example.com", "fr-FR"));
        // Assert
        EmailMessage spanishEmail = TestAssert.IsNotNull(spanishContext.Email);
        EmailMessage fallbackEmail = TestAssert.IsNotNull(fallbackContext.Email);
        Assert.AreEqual("Reversi: Verificación de inicio de sesión", spanishEmail.Subject);
        Assert.IsTrue(spanishEmail.Body.Contains("Tu código de verificación"));
        Assert.AreEqual("Reversi: Sign-in verification", fallbackEmail.Subject);
        Assert.IsTrue(fallbackEmail.Body.Contains("Your sign-in verification code"));
    }

    private static string ExtractDeliveredCode(SessionTestContext context)
    {
        EmailMessage email = TestAssert.IsNotNull(context.Email);
        Match codeMatch = Regex.Match(email.Body, @"(?<!#)\b(\d{6})\b");
        Assert.IsTrue(codeMatch.Success);
        return codeMatch.Groups[1].Value;
    }
}
