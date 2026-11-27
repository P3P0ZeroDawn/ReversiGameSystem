using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.AccountServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class DeactivateAccountTests : AccountServiceTestBase
{
    [TestMethod]
    public void DeactivateAccount_MissingSessionToken_ThrowsSessionFault()
    {
        SessionFailure expectedReason = SessionFailure.MissingSessionToken;
        Action action = () => Service.DeactivateAccount("", "Password1!");

        FaultException<SessionFault> actualException =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void DeactivateAccount_ActiveMatchInProgress_ThrowsAccountFault()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(password: "Password1!"));
        Accounts.DeactivateAccountResult = AccountStatus.ActiveMatchInProgress;
        AccountFailure expectedReason = AccountFailure.ActiveMatchInProgress;
        Action action = () => Service.DeactivateAccount(session.SessionToken, "Password1!");

        FaultException<AccountFault> actualException =
            Assert.Throws<FaultException<AccountFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void DeactivateAccount_ValidRequest_DeactivatesAccount()
    {
        AuthenticatedSession session = CreateSession();
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(accountId: 42, password: "Password1!"));
        long? expectedAccountId = 42;

        Service.DeactivateAccount(session.SessionToken, "Password1!");

        Assert.AreEqual(expectedAccountId, Accounts.LastDeactivatedAccountId);
    }
}
