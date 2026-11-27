using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application;

[TestClass]
[TestCategory("Unit")]
public sealed class VerificationCodeWorkflowTests
{
    private const string SampleEmail = "player@example.com";

    private static VerificationIdentity Identity
    {
        get { return VerificationIdentity.ForLogin(42, SampleEmail); }
    }

    [TestMethod]
    public void IssueVerificationCode_ValidIdentity_DeliversSixDigitsWithFiveMinuteExpiry()
    {
        SessionTestContext context = new SessionTestContext();

        DateTime expiration = context.Codes.IssueVerificationCode(Identity);

        EmailMessage email = TestAssert.IsNotNull(context.Email);
        Assert.AreEqual(context.UtcNow.AddMinutes(5), expiration);
        Assert.AreEqual(SampleEmail, email.RecipientAddress);
        Assert.IsTrue(email.IsHtml);
        Match codeMatch = Regex.Match(email.Body, @"(?<!#)\b(\d{6})\b");
        Assert.IsTrue(
            codeMatch.Success,
            "A verification code must contain exactly six ASCII digits.");
        string code = codeMatch.Groups[1].Value;
        PendingVerificationCode pending = TestAssert.IsNotNull(context.Pending);
        Assert.AreEqual(expiration, pending.ExpiresAtUtc);
        Assert.IsTrue(
            pending.Digest.SequenceEqual(context.Protector.ComputeCodeDigest(Identity.Key, code)),
            "The persisted digest must authenticate the delivered code and its identity.");
    }

    [TestMethod]
    public void IssueVerificationCode_DeliveryTimeout_DoesNotReplacePendingCode()
    {
        SessionTestContext context = new SessionTestContext();
        context.Codes.IssueVerificationCode(Identity);
        PendingVerificationCode? previous = context.Pending;
        context.FailDelivery = true;

        Assert.Throws<TimeoutException>(() => context.Codes.IssueVerificationCode(Identity));

        Assert.IsTrue(
            ReferenceEquals(previous, context.Pending),
            "Failed delivery must preserve pending state.");
    }

    [TestMethod]
    public void VerifyCode_ExactExpiry_RejectsExpiredCode()
    {
        SessionTestContext context = new SessionTestContext();
        context.Codes.IssueVerificationCode(Identity);
        string deliveredCode = ExtractDeliveredCode(context);
        context.UtcNow = context.UtcNow.AddMinutes(5);
        VerificationStatus expectedStatus = VerificationStatus.Expired;

        VerificationStatus result = context.Codes.VerifyCode(Identity, deliveredCode);

        Assert.AreEqual(expectedStatus, result);
    }

    [TestMethod]
    public void VerifyCode_FiveFailures_InvalidatesCodeOnFifthAttempt()
    {
        SessionTestContext context = new SessionTestContext();
        context.Codes.IssueVerificationCode(Identity);
        string deliveredCode = ExtractDeliveredCode(context);
        VerificationStatus expectedInvalid = VerificationStatus.Invalid;
        int expectedFailures = 5;

        for (int attempt = 0; attempt < 5; attempt++)
        {
            VerificationStatus attemptStatus = context.Codes.VerifyCode(Identity, "999999");
            Assert.AreEqual(expectedInvalid, attemptStatus);
        }

        StoredVerificationCode stored = TestAssert.IsNotNull(context.Stored);
        VerificationStatus afterLockout = context.Codes.VerifyCode(Identity, deliveredCode);
        Assert.AreEqual(expectedFailures, stored.FailedAttempts);
        Assert.IsNotNull(stored.InvalidatedAtUtc);
        Assert.AreEqual(expectedInvalid, afterLockout);
    }

    [TestMethod]
    public void VerifyCode_ValidCodeUsedOnce_RejectsReuse()
    {
        SessionTestContext context = new SessionTestContext();
        context.Codes.IssueVerificationCode(Identity);
        string deliveredCode = ExtractDeliveredCode(context);
        VerificationStatus expectedFirst = VerificationStatus.Accepted;
        VerificationStatus expectedSecond = VerificationStatus.Invalid;

        VerificationStatus firstAttempt = context.Codes.VerifyCode(Identity, deliveredCode);
        VerificationStatus secondAttempt = context.Codes.VerifyCode(Identity, deliveredCode);

        Assert.AreEqual(expectedFirst, firstAttempt);
        Assert.AreEqual(expectedSecond, secondAttempt);
    }

    [TestMethod]
    public void ComputeCodeDigest_DifferentIdentity_BindsCodeToAccountAndPurpose()
    {
        VerificationCodeProtector protector = new VerificationCodeProtector(new byte[32]);

        byte[] login = protector.ComputeCodeDigest(Identity.Key, "000012");
        byte[] other = protector.ComputeCodeDigest(
            VerificationIdentity.ForLogin(43, "other@example.com").Key, "000012");
        byte[] registration = protector.ComputeCodeDigest(
            VerificationIdentity.ForRegistration("PlayerOne", SampleEmail).Key, "000012");

        Assert.IsTrue(
            !login.SequenceEqual(other) && !login.SequenceEqual(registration),
            "A code digest must not transfer between accounts or purposes.");
    }

    [TestMethod]
    public void IssueVerificationCode_SpanishAndDefaultLocales_RenderSingleLanguageEmails()
    {
        SessionTestContext spanishContext = new SessionTestContext();
        SessionTestContext fallbackContext = new SessionTestContext();
        string expectedSpanishSubject = "Reversi: Verificación de inicio de sesión";
        string expectedEnglishSubject = "Reversi: Sign-in verification";

        spanishContext.Codes.IssueVerificationCode(
            VerificationIdentity.ForLogin(42, SampleEmail, "es-MX"));
        fallbackContext.Codes.IssueVerificationCode(
            VerificationIdentity.ForLogin(42, SampleEmail, "fr-FR"));

        EmailMessage spanishEmail = TestAssert.IsNotNull(spanishContext.Email);
        EmailMessage fallbackEmail = TestAssert.IsNotNull(fallbackContext.Email);
        Assert.AreEqual(expectedSpanishSubject, spanishEmail.Subject);
        Assert.IsTrue(spanishEmail.Body.Contains("Tu código de verificación"));
        Assert.AreEqual(expectedEnglishSubject, fallbackEmail.Subject);
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
