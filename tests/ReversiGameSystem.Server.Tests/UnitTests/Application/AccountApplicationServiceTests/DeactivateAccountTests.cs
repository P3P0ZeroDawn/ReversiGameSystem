using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class DeactivateAccountTests : AccountApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void DeactivateAccount_MissingSessionToken_ReturnsMissingTokenAndSkipsRepository(
        string? sessionToken)
    {
        AccountStatus expectedStatus = AccountStatus.MissingSessionToken;
        int expectedCallCount = 0;

        AccountStatus actualStatus = Service.DeactivateAccount(sessionToken!, "Password1!");

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void DeactivateAccount_InvalidSessionToken_ReturnsInvalidSessionAndSkipsRepository()
    {
        AccountStatus expectedStatus = AccountStatus.InvalidSession;
        int expectedCallCount = 0;

        AccountStatus actualStatus = Service.DeactivateAccount("unknown-token", "Password1!");

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void DeactivateAccount_MissingAccount_ReturnsInactiveAccount()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        AccountStatus expectedStatus = AccountStatus.InactiveAccount;

        AccountStatus actualStatus = Service.DeactivateAccount(
            session.SessionToken,
            "Password1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void DeactivateAccount_DisabledAccount_ReturnsInactiveAccount()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(statusCode: "DISABLED"));
        AccountStatus expectedStatus = AccountStatus.InactiveAccount;

        AccountStatus actualStatus = Service.DeactivateAccount(
            session.SessionToken,
            "Password1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void DeactivateAccount_IncorrectCurrentPassword_ReturnsIncorrectCurrentPassword()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "Password1!"));
        AccountStatus expectedStatus = AccountStatus.IncorrectCurrentPassword;

        AccountStatus actualStatus = Service.DeactivateAccount(
            session.SessionToken,
            "WrongPassword1!");

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void DeactivateAccount_ActiveMatchInProgress_PreservesExistingSessions()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "Password1!"));
        Accounts.DeactivateAccountResult = AccountStatus.ActiveMatchInProgress;
        AccountStatus expectedStatus = AccountStatus.ActiveMatchInProgress;
        SessionStatus expectedSessionStatus = SessionStatus.Success;

        AccountStatus actualStatus = Service.DeactivateAccount(
            session.SessionToken,
            "Password1!");
        SessionResult<AuthenticatedSession> actualSession =
            Sessions.FindAuthenticated(session.SessionToken);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSessionStatus, actualSession.Status);
    }

    [TestMethod]
    public void DeactivateAccount_ValidRequest_DeactivatesAccountAndRevokesSessions()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "Password1!"));
        AccountStatus expectedStatus = AccountStatus.Success;
        SessionStatus expectedSessionStatus = SessionStatus.InvalidSession;

        AccountStatus actualStatus = Service.DeactivateAccount(
            session.SessionToken,
            "Password1!");
        SessionResult<AuthenticatedSession> actualSession =
            Sessions.FindAuthenticated(session.SessionToken);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSessionStatus, actualSession.Status);
    }
}
