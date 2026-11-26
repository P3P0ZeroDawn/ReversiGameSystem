using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.AccountServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ChangePasswordTests : AccountServiceTestBase
{
    [TestMethod]
    public void ChangePassword_MissingSessionToken_ThrowsSessionFault()
    {
        SessionFailure expectedReason = SessionFailure.MissingSessionToken;
        Action action = () => Service.ChangePassword("", "Password1!", "NewPassword1!");

        FaultException<SessionFault> actualException =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void ChangePassword_InvalidSessionToken_ThrowsSessionFault()
    {
        SessionFailure expectedReason = SessionFailure.InvalidSession;
        Action action = () => Service.ChangePassword(
            "unknown-token",
            "Password1!",
            "NewPassword1!");

        FaultException<SessionFault> actualException =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void ChangePassword_IncorrectCurrentPassword_ThrowsAccountFault()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "Password1!"));
        AccountFailure expectedReason = AccountFailure.IncorrectCurrentPassword;
        Action action = () => Service.ChangePassword(
            session.SessionToken,
            "WrongPassword1!",
            "NewPassword1!");

        FaultException<AccountFault> actualException =
            Assert.Throws<FaultException<AccountFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void ChangePassword_ValidRequest_UpdatesPasswordHash()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "Password1!"));

        Service.ChangePassword(session.SessionToken, "Password1!", "NewPassword1!");

        bool actualVerified = PasswordHasher.Verify(
            "NewPassword1!",
            Accounts.LastUpdatedPasswordHash!);
        Assert.IsTrue(actualVerified);
    }
}
