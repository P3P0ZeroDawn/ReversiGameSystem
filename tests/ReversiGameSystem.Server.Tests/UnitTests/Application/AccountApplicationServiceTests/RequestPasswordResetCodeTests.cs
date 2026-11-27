using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class RequestPasswordResetCodeTests : AccountApplicationServiceTestBase
{
    [TestMethod]
    [DataRow("")]
    [DataRow("invalid-email")]
    public void RequestPasswordResetCode_InvalidEmail_ReturnsInvalidEmailAndSkipsRepository(
        string emailAddress)
    {
        AccountStatus expectedStatus = AccountStatus.InvalidEmailAddress;
        int expectedCallCount = 0;

        AccountResult<DateTime> actualResult = Service.RequestPasswordResetCode(emailAddress);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void RequestPasswordResetCode_ActiveAccount_ReturnsCodeExpirationUtc()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);

        AccountResult<DateTime> actualResult = Service.RequestPasswordResetCode(
            "  player@example.com  ");

        Assert.AreEqual(expectedExpirationUtc, actualResult.Value);
    }

    [TestMethod]
    public void RequestPasswordResetCode_ActiveAccount_SendsPasswordResetEmailTemplate()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        string expectedTemplateName = EmailTemplateNames.VerificationPasswordReset;

        Service.RequestPasswordResetCode("player@example.com");

        Assert.AreEqual(expectedTemplateName, EmailSender.LastTemplateName);
    }

    [TestMethod]
    public void RequestPasswordResetCode_UnknownAccount_ReturnsPlausibleExpiryWithoutEmail()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(
            AccountStatus.AccountNotFound);
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);
        int expectedEmailCount = 0;

        AccountResult<DateTime> actualResult = Service.RequestPasswordResetCode(
            "unknown@example.com");

        Assert.AreEqual(expectedExpirationUtc, actualResult.Value);
        Assert.AreEqual(expectedEmailCount, EmailSender.SendCallCount);
    }

    [TestMethod]
    public void RequestPasswordResetCode_InactiveAccount_ReturnsPlausibleExpiryWithoutEmail()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(statusCode: "DISABLED"));
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);
        int expectedEmailCount = 0;

        AccountResult<DateTime> actualResult = Service.RequestPasswordResetCode(
            "player@example.com");

        Assert.AreEqual(expectedExpirationUtc, actualResult.Value);
        Assert.AreEqual(expectedEmailCount, EmailSender.SendCallCount);
    }
}
