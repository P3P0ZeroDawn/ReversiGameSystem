using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ChangePasswordTests : AccountApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ChangePassword_MissingSessionToken_ReturnsMissingSessionTokenAndSkipsRepository(
        string? sessionToken)
    {
        AccountStatus expectedStatus = AccountStatus.MissingSessionToken;
        int expectedCallCount = 0;

        AccountStatus actualStatus = Service.ChangePassword(
            sessionToken!,
            "Password1!",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void ChangePassword_InvalidSessionToken_ReturnsInvalidSessionAndSkipsRepository()
    {
        AccountStatus expectedStatus = AccountStatus.InvalidSession;
        int expectedCallCount = 0;

        AccountStatus actualStatus = Service.ChangePassword(
            "unknown-token",
            "Password1!",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void ChangePassword_InvalidNewPassword_ReturnsInvalidPasswordAndSkipsRepository()
    {
        AuthenticatedSession session = CreateSession();
        AccountStatus expectedStatus = AccountStatus.InvalidPassword;
        int expectedCallCount = 0;

        AccountStatus actualStatus = Service.ChangePassword(
            session.SessionToken,
            "Password1!",
            "weak");

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void ChangePassword_MissingAccount_ReturnsInactiveAccount()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        AccountStatus expectedStatus = AccountStatus.InactiveAccount;

        AccountStatus actualStatus = Service.ChangePassword(
            session.SessionToken,
            "Password1!",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ChangePassword_DisabledAccount_ReturnsInactiveAccount()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(statusCode: "DISABLED"));
        AccountStatus expectedStatus = AccountStatus.InactiveAccount;

        AccountStatus actualStatus = Service.ChangePassword(
            session.SessionToken,
            "Password1!",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ChangePassword_IncorrectCurrentPassword_ReturnsIncorrectPasswordAndSkipsUpdate()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "Password1!"));
        AccountStatus expectedStatus = AccountStatus.IncorrectCurrentPassword;
        string? expectedUpdatedHash = null;

        AccountStatus actualStatus = Service.ChangePassword(
            session.SessionToken,
            "WrongPassword1!",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedUpdatedHash, Accounts.LastUpdatedPasswordHash);
    }

    [TestMethod]
    public void ChangePassword_ValidRequest_ReturnsSuccess()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "Password1!"));
        AccountStatus expectedStatus = AccountStatus.Success;

        AccountStatus actualStatus = Service.ChangePassword(
            session.SessionToken,
            "Password1!",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ChangePassword_ValidRequest_PersistsHashedNewPassword()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "\u00C1b12345!"));

        Service.ChangePassword(
            session.SessionToken,
            "A\u0301b12345!",
            "N\u00E9wPass1!");

        bool actualVerified = PasswordHasher.Verify(
            "Ne\u0301wPass1!",
            Accounts.LastUpdatedPasswordHash!);
        Assert.IsTrue(actualVerified);
    }
}
