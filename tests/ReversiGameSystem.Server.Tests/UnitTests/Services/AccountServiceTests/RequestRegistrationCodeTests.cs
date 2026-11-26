using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.AccountServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class RequestRegistrationCodeTests : AccountServiceTestBase
{
    [TestMethod]
    public void RequestRegistrationCode_ValidIdentity_ReturnsExpirationUtc()
    {
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);

        DateTime actualExpirationUtc = Service.RequestRegistrationCode(
            "PlayerOne",
            "player@example.com");

        Assert.AreEqual(expectedExpirationUtc, actualExpirationUtc);
    }

    [TestMethod]
    public void RequestRegistrationCode_InvalidUsername_ThrowsAccountFault()
    {
        AccountFailure expectedReason = AccountFailure.InvalidUsername;
        Action action = () => Service.RequestRegistrationCode("ab", "player@example.com");

        FaultException<AccountFault> actualException =
            Assert.Throws<FaultException<AccountFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void RequestRegistrationCode_EmailUnavailable_ThrowsAccountFault()
    {
        Accounts.CheckRegistrationAvailabilityResult = AccountStatus.EmailUnavailable;
        AccountFailure expectedReason = AccountFailure.EmailUnavailable;
        Action action = () => Service.RequestRegistrationCode(
            "PlayerOne",
            "player@example.com");

        FaultException<AccountFault> actualException =
            Assert.Throws<FaultException<AccountFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }
}
