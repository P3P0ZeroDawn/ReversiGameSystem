using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.AccountServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class RequestPasswordResetCodeTests : AccountServiceTestBase
{
    [TestMethod]
    public void RequestPasswordResetCode_InvalidEmail_ThrowsAccountFault()
    {
        AccountFailure expectedReason = AccountFailure.InvalidEmailAddress;
        Action action = () => Service.RequestPasswordResetCode("invalid");

        FaultException<AccountFault> actualException =
            Assert.Throws<FaultException<AccountFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void RequestPasswordResetCode_ValidEmail_ReturnsExpirationUtc()
    {
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);

        DateTime actualExpirationUtc = Service.RequestPasswordResetCode("player@example.com");

        Assert.AreEqual(expectedExpirationUtc, actualExpirationUtc);
    }
}
