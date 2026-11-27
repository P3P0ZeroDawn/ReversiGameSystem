using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.VerificationCodeServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class IssueVerificationCodeTests : VerificationCodeServiceTestBase
{
    [TestMethod]
    public void IssueVerificationCode_NullIdentity_ThrowsArgumentNullException()
    {
        Action action = () => Service.IssueVerificationCode(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void IssueVerificationCode_ValidIdentity_ReturnsFiveMinuteExpiration()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);

        DateTime actualExpirationUtc = Service.IssueVerificationCode(identity);

        Assert.AreEqual(expectedExpirationUtc, actualExpirationUtc);
    }

    [TestMethod]
    public void IssueVerificationCode_LoginPurpose_SendsLoginTemplate()
    {
        VerificationIdentity identity = VerificationIdentity.ForLogin(42, "player@example.com");
        string expectedTemplateName = EmailTemplateNames.VerificationLogin;

        Service.IssueVerificationCode(identity);

        Assert.AreEqual(expectedTemplateName, EmailSender.LastTemplateName);
    }

    [TestMethod]
    public void IssueVerificationCode_RegistrationPurpose_SendsRegistrationTemplate()
    {
        VerificationIdentity identity = VerificationIdentity.ForRegistration(
            "PlayerOne",
            "player@example.com");
        string expectedTemplateName = EmailTemplateNames.VerificationRegistration;

        Service.IssueVerificationCode(identity);

        Assert.AreEqual(expectedTemplateName, EmailSender.LastTemplateName);
    }

    [TestMethod]
    public void IssueVerificationCode_PasswordResetPurpose_SendsPasswordResetTemplate()
    {
        VerificationIdentity identity = VerificationIdentity.ForPasswordReset(
            42,
            "player@example.com");
        string expectedTemplateName = EmailTemplateNames.VerificationPasswordReset;

        Service.IssueVerificationCode(identity);

        Assert.AreEqual(expectedTemplateName, EmailSender.LastTemplateName);
    }

    [TestMethod]
    public void IssueVerificationCode_ValidIdentity_DeliversSixDigitCodeMatchingSavedDigest()
    {
        VerificationIdentity identity = CreateLoginIdentity();

        Service.IssueVerificationCode(identity);
        string deliveredCode = EmailSender.LastTemplateValues!["VerificationCode"];
        byte[] expectedDigest = Protector.ComputeCodeDigest(identity.Key, deliveredCode);

        bool actualIsSixDigits = Regex.IsMatch(deliveredCode, @"^\d{6}$");
        bool actualDigestMatches = Repository.LastSavedCode!.Digest.SequenceEqual(expectedDigest);

        Assert.IsTrue(actualIsSixDigits);
        Assert.IsTrue(actualDigestMatches);
    }

    [TestMethod]
    public void IssueVerificationCode_EmailDeliveryFails_ThrowsAndSkipsRepositorySave()
    {
        VerificationIdentity identity = CreateLoginIdentity();
        EmailSender.Failure = new TimeoutException("SMTP timeout.");
        int expectedSaveCount = 0;
        Action action = () => Service.IssueVerificationCode(identity);

        Assert.Throws<TimeoutException>(action);
        Assert.AreEqual(expectedSaveCount, Repository.SaveCallCount);
    }
}
