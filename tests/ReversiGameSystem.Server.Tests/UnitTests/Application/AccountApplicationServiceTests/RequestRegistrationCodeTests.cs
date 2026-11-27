using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class RequestRegistrationCodeTests : AccountApplicationServiceTestBase
{
    [TestMethod]
    [DataRow("")]
    [DataRow("ab")]
    public void RequestRegistrationCode_InvalidUsername_ReturnsInvalidUsernameAndSkipsRepository(
        string username)
    {
        AccountStatus expectedStatus = AccountStatus.InvalidUsername;
        int expectedCallCount = 0;

        AccountResult<DateTime> actualResult = Service.RequestRegistrationCode(
            username,
            "player@example.com");

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("invalid-email")]
    public void RequestRegistrationCode_InvalidEmail_ReturnsInvalidEmailAndSkipsRepository(
        string emailAddress)
    {
        AccountStatus expectedStatus = AccountStatus.InvalidEmailAddress;
        int expectedCallCount = 0;

        AccountResult<DateTime> actualResult = Service.RequestRegistrationCode(
            "PlayerOne",
            emailAddress);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    [DataRow(AccountStatus.UsernameUnavailable)]
    [DataRow(AccountStatus.EmailUnavailable)]
    public void RequestRegistrationCode_UnavailableIdentity_ReturnsFailureAndSkipsEmail(
        AccountStatus expectedStatus)
    {
        Accounts.CheckRegistrationAvailabilityResult = expectedStatus;
        int expectedEmailCount = 0;

        AccountResult<DateTime> actualResult = Service.RequestRegistrationCode(
            "PlayerOne",
            "player@example.com");

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedEmailCount, EmailSender.SendCallCount);
    }

    [TestMethod]
    public void RequestRegistrationCode_ValidIdentity_TrimsUsernameBeforeAvailabilityCheck()
    {
        string expectedUsername = "PlayerOne";

        Service.RequestRegistrationCode("  PlayerOne  ", "player@example.com");

        Assert.AreEqual(expectedUsername, Accounts.LastCheckedUsername);
    }

    [TestMethod]
    public void RequestRegistrationCode_ValidIdentity_TrimsEmailBeforeAvailabilityCheck()
    {
        string expectedEmail = "player@example.com";

        Service.RequestRegistrationCode("PlayerOne", "  player@example.com  ");

        Assert.AreEqual(expectedEmail, Accounts.LastCheckedEmailAddress);
    }

    [TestMethod]
    public void RequestRegistrationCode_ValidIdentity_ReturnsFiveMinuteExpiration()
    {
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);

        AccountResult<DateTime> actualResult = Service.RequestRegistrationCode(
            "PlayerOne",
            "player@example.com");

        Assert.AreEqual(expectedExpirationUtc, actualResult.Value);
    }

    [TestMethod]
    public void RequestRegistrationCode_ValidIdentity_SendsRegistrationEmailTemplate()
    {
        string expectedTemplateName = EmailTemplateNames.VerificationRegistration;

        Service.RequestRegistrationCode("PlayerOne", "player@example.com");

        Assert.AreEqual(expectedTemplateName, EmailSender.LastTemplateName);
    }
}
