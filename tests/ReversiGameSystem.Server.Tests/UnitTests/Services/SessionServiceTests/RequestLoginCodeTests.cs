using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.SessionServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class RequestLoginCodeTests : SessionServiceTestBase
{
    [TestMethod]
    public void RequestLoginCode_ValidCredentials_ReturnsExpirationUtc()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);

        DateTime actualExpirationUtc = Service.RequestLoginCode("PlayerOne", "Password1!");

        Assert.AreEqual(expectedExpirationUtc, actualExpirationUtc);
    }

    [TestMethod]
    public void RequestLoginCode_InvalidCredentials_ThrowsSessionFault()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(CreateLoginAccount());
        SessionFailure expectedReason = SessionFailure.InvalidCredentials;
        Action action = () => Service.RequestLoginCode("PlayerOne", "WrongPassword1!");

        FaultException<SessionFault> actualException =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void RequestLoginCode_InactiveAccount_ThrowsSessionFault()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(
            CreateLoginAccount(statusCode: "DISABLED"));
        SessionFailure expectedReason = SessionFailure.InactiveAccount;
        Action action = () => Service.RequestLoginCode("PlayerOne", "Password1!");

        FaultException<SessionFault> actualException =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }
}
