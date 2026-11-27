using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Contracts.Faults;
using ContractSession = ReversiGameSystem.Server.Contracts.Accounts.AuthenticatedSession;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.SessionServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CompleteLoginTests : SessionServiceTestBase
{
    [TestMethod]
    public void CompleteLogin_ExpiredCode_ThrowsSessionFault()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        VerificationCodes.VerificationResult = VerificationStatus.Expired;
        SessionFailure expectedReason = SessionFailure.ExpiredVerificationCode;
        Action action = () => Service.CompleteLogin("PlayerOne", "000001");

        FaultException<SessionFault> actualException =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void CompleteLogin_ValidCode_MapsAccountId()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(accountId: 42));
        long expectedAccountId = 42;

        ContractSession actualSession = Service.CompleteLogin("PlayerOne", "000001");

        Assert.AreEqual(expectedAccountId, actualSession.Account.AccountId);
    }

    [TestMethod]
    public void CompleteLogin_ValidCode_MapsPreferredLocaleCode()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        string expectedLocaleCode = "en-US";

        ContractSession actualSession = Service.CompleteLogin("PlayerOne", "000001");

        Assert.AreEqual(expectedLocaleCode, actualSession.PreferredLocaleCode);
    }
}
