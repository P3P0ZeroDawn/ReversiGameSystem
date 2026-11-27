using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Tests.TestDoubles;
using AppPlayerProfileService =
    ReversiGameSystem.Server.Application.Players.PlayerProfileApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class AccountAuditLoggingTests : AccountApplicationServiceTestBase
{
    [TestMethod]
    public void CompleteRegistration_ValidCode_LogsAccountIdAndMaskedEmailWithoutSecrets()
    {
        const string password = "StrongPassword1!";
        const string code = "123456";
        Accounts.CreateAccountResult = new AccountResult<long>(88);
        VerificationCodes.VerificationResult = VerificationStatus.Accepted;
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = new AccountApplicationService(
            Accounts,
            Images,
            VerificationService,
            Sessions,
            logger);

        NewAccountRegistration registration = new NewAccountRegistration(
            code,
            "NewPlayer",
            "new.player@example.com",
            password,
            "en-US",
            null);
        AccountResult<AccountSummary> result = auditedService.CompleteRegistration(registration);

        Assert.AreEqual(AccountStatus.Success, result.Status);
        RecordedLogEntry entry = logger.Entries.Single();
        Assert.AreEqual(LogLevel.Information, entry.Level);
        Assert.IsTrue(entry.Message.Contains("88"));
        Assert.IsTrue(entry.Message.Contains("n***@example.com"));
        Assert.IsFalse(entry.Message.Contains("new.player@example.com"));
        Assert.IsFalse(entry.Message.Contains(password));
        Assert.IsFalse(entry.Message.Contains(code));
    }

    [TestMethod]
    public void ChangePassword_IncorrectCurrentPassword_LogsWarningWithoutSecrets()
    {
        const string currentPassword = "CurrentPassword1!";
        const string wrongPassword = "WrongPassword1!";
        const string newPassword = "NewPassword1!";
        const string token = "auth-token-99";
        LoginAccount account = CreateLoginAccount(accountId: 99, password: currentPassword);
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(account);
        Sessions.TryAddAuthenticated(new AuthenticatedSession(
            token,
            99,
            "PlayerOne",
            "player@example.com",
            "en-US",
            Clock.UtcNow.AddHours(1)));
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = new AccountApplicationService(
            Accounts,
            Images,
            VerificationService,
            Sessions,
            logger);

        AccountStatus status = auditedService.ChangePassword(token, wrongPassword, newPassword);

        Assert.AreEqual(AccountStatus.IncorrectCurrentPassword, status);
        RecordedLogEntry entry = logger.Entries.Single();
        Assert.AreEqual(LogLevel.Warning, entry.Level);
        Assert.IsTrue(entry.Message.Contains("99"));
        Assert.IsTrue(entry.Message.Contains("IncorrectCurrentPassword"));
        Assert.IsFalse(entry.Message.Contains(wrongPassword));
        Assert.IsFalse(entry.Message.Contains(newPassword));
        Assert.IsFalse(entry.Message.Contains(token));
    }

    [TestMethod]
    public void RequestResetAndResetAndDeactivate_LogsAuditTrailWithoutSecrets()
    {
        const string password = "CurrentPassword1!";
        const string newPassword = "UpdatedPassword1!";
        const string code = "654321";
        const string token = "auth-token-55";
        LoginAccount account = CreateLoginAccount(accountId: 55, password: password);
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(account);
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(account);
        VerificationCodes.VerificationResult = VerificationStatus.Accepted;
        Sessions.TryAddAuthenticated(new AuthenticatedSession(
            token,
            55,
            "PlayerOne",
            account.EmailAddress,
            "en-US",
            Clock.UtcNow.AddHours(1)));
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = new AccountApplicationService(
            Accounts,
            Images,
            VerificationService,
            Sessions,
            logger);

        AccountResult<DateTime> resetCodeResult = auditedService.RequestPasswordResetCode(
            account.EmailAddress);
        Assert.AreEqual(AccountStatus.Success, resetCodeResult.Status);

        AccountStatus resetStatus = auditedService.ResetPassword(
            account.EmailAddress,
            code,
            newPassword);
        Assert.AreEqual(AccountStatus.Success, resetStatus);

        Sessions.TryAddAuthenticated(new AuthenticatedSession(
            token,
            55,
            "PlayerOne",
            account.EmailAddress,
            "en-US",
            Clock.UtcNow.AddHours(1)));
        AccountStatus deactivateStatus = auditedService.DeactivateAccount(token, password);
        Assert.AreEqual(AccountStatus.Success, deactivateStatus);

        Assert.IsTrue(logger.Entries.Any(e =>
            e.Level == LogLevel.Information && e.Message.Contains("Password reset code issued for account 55")));
        Assert.IsTrue(logger.Entries.Any(e =>
            e.Level == LogLevel.Information && e.Message.Contains("Password reset completed for account 55")));
        Assert.IsTrue(logger.Entries.Any(e =>
            e.Level == LogLevel.Information && e.Message.Contains("Account 55 deactivated")));

        string allLogs = string.Join("\n", logger.Entries.Select(e => e.Message));
        Assert.IsFalse(allLogs.Contains(password));
        Assert.IsFalse(allLogs.Contains(newPassword));
        Assert.IsFalse(allLogs.Contains(code));
        Assert.IsFalse(allLogs.Contains(token));
        Assert.IsFalse(allLogs.Contains(account.EmailAddress));
    }

    [TestMethod]
    public void ChangeUsername_Success_LogsAccountIdAndSanitizedUsername()
    {
        const string token = "profile-token-1";
        Sessions.TryAddAuthenticated(new AuthenticatedSession(
            token,
            33,
            "OldName",
            "old@example.com",
            "en-US",
            Clock.UtcNow.AddHours(1)));
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        RecordingLogger<AppPlayerProfileService> logger =
            new RecordingLogger<AppPlayerProfileService>();
        AppPlayerProfileService profileService = new AppPlayerProfileService(
            Sessions,
            profiles,
            Images,
            logger);

        ProfileStatus status = profileService.ChangeUsername(token, "NewName33");

        Assert.AreEqual(ProfileStatus.Success, status);
        RecordedLogEntry entry = logger.Entries.Single();
        Assert.AreEqual(LogLevel.Information, entry.Level);
        Assert.IsTrue(entry.Message.Contains("33"));
        Assert.IsTrue(entry.Message.Contains("NewName33"));
        Assert.IsFalse(entry.Message.Contains(token));
    }
}
