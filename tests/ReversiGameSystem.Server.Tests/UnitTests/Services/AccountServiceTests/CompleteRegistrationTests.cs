using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.AccountServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CompleteRegistrationTests : AccountServiceTestBase
{
    [TestMethod]
    public void CompleteRegistration_NullAccount_ThrowsAccountFault()
    {
        AccountFailure expectedReason = AccountFailure.MissingRegistrationData;
        Action action = () => Service.CompleteRegistration(null!);

        FaultException<AccountFault> actualException =
            Assert.Throws<FaultException<AccountFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void CompleteRegistration_UsernameUnavailable_ThrowsAccountFault()
    {
        Accounts.CreateAccountResult = new AccountResult<long>(AccountStatus.UsernameUnavailable);
        AccountFailure expectedReason = AccountFailure.UsernameUnavailable;
        NewAccount account = new NewAccount
        {
            VerificationCode = "000001",
            Username = "PlayerOne",
            EmailAddress = "player@example.com",
            Password = "Password1!",
            PreferredLocaleCode = "en-US"
        };
        Action action = () => Service.CompleteRegistration(account);

        FaultException<AccountFault> actualException =
            Assert.Throws<FaultException<AccountFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void CompleteRegistration_ValidAccount_MapsAccountId()
    {
        Accounts.CreateAccountResult = new AccountResult<long>(42);
        long expectedAccountId = 42;
        NewAccount account = new NewAccount
        {
            VerificationCode = "000001",
            Username = "PlayerOne",
            EmailAddress = "player@example.com",
            Password = "Password1!",
            PreferredLocaleCode = "en-US",
            ProfileImage = new ImageContent
            {
                Content = CreatePngBytes(),
                MediaType = "image/png"
            }
        };

        AccountReference actualReference = Service.CompleteRegistration(account);

        Assert.AreEqual(expectedAccountId, actualReference.AccountId);
    }

    [TestMethod]
    public void CompleteRegistration_ValidAccount_MapsUsername()
    {
        Accounts.CreateAccountResult = new AccountResult<long>(42);
        string expectedUsername = "PlayerOne";
        NewAccount account = new NewAccount
        {
            VerificationCode = "000001",
            Username = "  PlayerOne  ",
            EmailAddress = "player@example.com",
            Password = "Password1!",
            PreferredLocaleCode = "en-US"
        };

        AccountReference actualReference = Service.CompleteRegistration(account);

        Assert.AreEqual(expectedUsername, actualReference.Username);
    }
}
