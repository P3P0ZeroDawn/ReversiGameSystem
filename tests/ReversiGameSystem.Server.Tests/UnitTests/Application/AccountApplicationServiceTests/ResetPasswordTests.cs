using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ResetPasswordTests : AccountApplicationServiceTestBase
{
    [TestMethod]
    public void ResetPassword_InvalidEmailAddress_ReturnsInvalidEmailAndSkipsRepository()
    {
        AccountStatus expectedStatus = AccountStatus.InvalidEmailAddress;
        int expectedCallCount = 0;

        AccountStatus actualStatus = Service.ResetPassword(
            "invalid",
            "000001",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void ResetPassword_InvalidNewPassword_ReturnsInvalidPasswordAndSkipsRepository()
    {
        AccountStatus expectedStatus = AccountStatus.InvalidPassword;
        int expectedCallCount = 0;

        AccountStatus actualStatus = Service.ResetPassword(
            "player@example.com",
            "000001",
            "weak");

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void ResetPassword_UnknownAccount_ReturnsInvalidVerificationCode()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(
            AccountStatus.AccountNotFound);
        AccountStatus expectedStatus = AccountStatus.InvalidVerificationCode;

        AccountStatus actualStatus = Service.ResetPassword(
            "unknown@example.com",
            "000001",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ResetPassword_InactiveAccount_ReturnsInactiveAccount()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(statusCode: "DISABLED"));
        AccountStatus expectedStatus = AccountStatus.InactiveAccount;

        AccountStatus actualStatus = Service.ResetPassword(
            "player@example.com",
            "000001",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ResetPassword_InvalidVerificationCode_ReturnsInvalidVerificationCode()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        VerificationCodes.VerificationResult = VerificationStatus.Invalid;
        AccountStatus expectedStatus = AccountStatus.InvalidVerificationCode;

        AccountStatus actualStatus = Service.ResetPassword(
            "player@example.com",
            "999999",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ResetPassword_ExpiredVerificationCode_ReturnsExpiredVerificationCode()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        VerificationCodes.VerificationResult = VerificationStatus.Expired;
        AccountStatus expectedStatus = AccountStatus.ExpiredVerificationCode;

        AccountStatus actualStatus = Service.ResetPassword(
            "player@example.com",
            "000001",
            "NewPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ResetPassword_UpdateFails_PreservesExistingSessions()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        Accounts.UpdatePasswordHashResult = AccountStatus.AccountNotFound;
        AccountStatus expectedStatus = AccountStatus.AccountNotFound;
        SessionStatus expectedSessionStatus = SessionStatus.Success;

        AccountStatus actualStatus = Service.ResetPassword(
            "player@example.com",
            "000001",
            "NewPassword1!");
        SessionResult<AuthenticatedSession> actualSession =
            Sessions.FindAuthenticated(session.SessionToken);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSessionStatus, actualSession.Status);
    }

    [TestMethod]
    public void ResetPassword_ValidRequest_UpdatesHashAndRevokesSessions()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        AccountStatus expectedStatus = AccountStatus.Success;
        SessionStatus expectedSessionStatus = SessionStatus.InvalidSession;

        AccountStatus actualStatus = Service.ResetPassword(
            "  player@example.com  ",
            "  000001  ",
            "NewPassword1!");
        SessionResult<AuthenticatedSession> actualSession =
            Sessions.FindAuthenticated(session.SessionToken);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSessionStatus, actualSession.Status);
    }

    [TestMethod]
    public void ResetPassword_ValidRequest_PersistsHashedNewPassword()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());

        Service.ResetPassword("player@example.com", "000001", "N\u00E9wPass1!");

        bool actualVerified = PasswordHasher.Verify(
            "Ne\u0301wPass1!",
            Accounts.LastUpdatedPasswordHash!);
        Assert.IsTrue(actualVerified);
    }
}
