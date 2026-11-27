using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class RequestLoginCodeTests : SessionApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void RequestLoginCode_MissingIdentifier_ReturnsInvalidCredentialsAndSkipsRepository(
        string? loginIdentifier)
    {
        SessionStatus expectedStatus = SessionStatus.InvalidCredentials;
        int expectedCallCount = 0;

        SessionResult<DateTime> actualResult = Service.RequestLoginCode(
            loginIdentifier!,
            "Password1!");

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void RequestLoginCode_TooLongIdentifier_ReturnsInvalidCredentialsAndSkipsRepository()
    {
        string tooLongIdentifier = new string('a', 255);
        SessionStatus expectedStatus = SessionStatus.InvalidCredentials;
        int expectedCallCount = 0;

        SessionResult<DateTime> actualResult = Service.RequestLoginCode(
            tooLongIdentifier,
            "Password1!");

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void RequestLoginCode_UnknownAccount_ReturnsInvalidCredentials()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        SessionStatus expectedStatus = SessionStatus.InvalidCredentials;

        SessionResult<DateTime> actualResult = Service.RequestLoginCode(
            "UnknownPlayer",
            "Password1!");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void RequestLoginCode_WrongPassword_ReturnsInvalidCredentials()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "Password1!"));
        SessionStatus expectedStatus = SessionStatus.InvalidCredentials;

        SessionResult<DateTime> actualResult = Service.RequestLoginCode(
            "PlayerOne",
            "WrongPassword1!");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void RequestLoginCode_InactiveAccount_ReturnsInactiveAccount()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(statusCode: "DISABLED"));
        SessionStatus expectedStatus = SessionStatus.InactiveAccount;

        SessionResult<DateTime> actualResult = Service.RequestLoginCode(
            "PlayerOne",
            "Password1!");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void RequestLoginCode_ValidCredentials_TrimsIdentifierForLookup()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "\u00C1b12345!"));
        string expectedIdentifier = "player@example.com";

        Service.RequestLoginCode("  player@example.com  ", "A\u0301b12345!");

        Assert.AreEqual(expectedIdentifier, Accounts.LastLoginIdentifier);
    }

    [TestMethod]
    public void RequestLoginCode_ValidCredentials_ReturnsFiveMinuteExpiration()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "\u00C1b12345!"));
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);

        SessionResult<DateTime> actualResult = Service.RequestLoginCode(
            "player@example.com",
            "A\u0301b12345!");

        Assert.AreEqual(expectedExpirationUtc, actualResult.Value);
    }

    [TestMethod]
    public void RequestLoginCode_ValidCredentials_SendsLoginEmailTemplate()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        string expectedTemplateName = EmailTemplateNames.VerificationLogin;

        Service.RequestLoginCode("PlayerOne", "Password1!");

        Assert.AreEqual(expectedTemplateName, EmailSender.LastTemplateName);
    }
}
