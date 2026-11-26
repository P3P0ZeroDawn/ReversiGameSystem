using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CompleteLoginTests : SessionApplicationServiceTestBase
{
    [TestMethod]
    public void CompleteLogin_MissingIdentifier_ReturnsInvalidVerificationCode()
    {
        SessionStatus expectedStatus = SessionStatus.InvalidVerificationCode;

        SessionResult<AuthenticatedSession> actualResult = Service.CompleteLogin("", "000001");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteLogin_UnknownAccount_ReturnsInvalidVerificationCode()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        SessionStatus expectedStatus = SessionStatus.InvalidVerificationCode;

        SessionResult<AuthenticatedSession> actualResult = Service.CompleteLogin(
            "UnknownPlayer",
            "000001");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteLogin_InvalidVerificationCode_ReturnsInvalidVerificationCode()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        VerificationCodes.VerificationResult = VerificationStatus.Invalid;
        SessionStatus expectedStatus = SessionStatus.InvalidVerificationCode;

        SessionResult<AuthenticatedSession> actualResult = Service.CompleteLogin(
            "PlayerOne",
            "999999");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteLogin_ExpiredVerificationCode_ReturnsExpiredVerificationCode()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        VerificationCodes.VerificationResult = VerificationStatus.Expired;
        SessionStatus expectedStatus = SessionStatus.ExpiredVerificationCode;

        SessionResult<AuthenticatedSession> actualResult = Service.CompleteLogin(
            "PlayerOne",
            "000001");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteLogin_InactiveAccount_ReturnsInactiveAccount()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(statusCode: "DISABLED"));
        SessionStatus expectedStatus = SessionStatus.InactiveAccount;

        SessionResult<AuthenticatedSession> actualResult = Service.CompleteLogin(
            "PlayerOne",
            "000001");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteLogin_ValidCode_ReturnsAuthenticatedAccountId()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(accountId: 42));
        long? expectedAccountId = 42;

        SessionResult<AuthenticatedSession> actualResult = Service.CompleteLogin(
            "PlayerOne",
            "000001");

        Assert.AreEqual(expectedAccountId, actualResult.Value?.AccountId);
    }

    [TestMethod]
    public void CompleteLogin_ValidCode_RegistersSessionInStore()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        SessionStatus expectedStoreStatus = SessionStatus.Success;

        SessionResult<AuthenticatedSession> actualResult = Service.CompleteLogin(
            "PlayerOne",
            "000001");
        SessionResult<AuthenticatedSession> storedResult =
            Sessions.FindAuthenticated(actualResult.Value!.SessionToken);

        Assert.AreEqual(expectedStoreStatus, storedResult.Status);
    }
}
