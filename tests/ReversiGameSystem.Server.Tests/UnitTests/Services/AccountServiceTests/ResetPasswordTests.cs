using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.AccountServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ResetPasswordTests : AccountServiceTestBase
{
    [TestMethod]
    public void ResetPassword_ExpiredVerificationCode_ThrowsAccountFault()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        VerificationCodes.VerificationResult = VerificationStatus.Expired;
        AccountFailure expectedReason = AccountFailure.ExpiredVerificationCode;
        Action action = () => Service.ResetPassword(
            "player@example.com",
            "000001",
            "NewPassword1!");

        FaultException<AccountFault> actualException =
            Assert.Throws<FaultException<AccountFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void ResetPassword_ValidRequest_UpdatesPasswordHash()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());

        Service.ResetPassword("player@example.com", "000001", "NewPassword1!");

        bool actualVerified = PasswordHasher.Verify(
            "NewPassword1!",
            Accounts.LastUpdatedPasswordHash!);
        Assert.IsTrue(actualVerified);
    }
}
