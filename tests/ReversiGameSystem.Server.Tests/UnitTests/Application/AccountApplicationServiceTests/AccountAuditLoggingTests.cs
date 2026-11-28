using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
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
    public void CompleteRegistration_ValidCode_LogsInformationWithAccountIdAndMaskedEmail()
    {
        Accounts.CreateAccountResult = new AccountResult<long>(88);
        VerificationCodes.VerificationResult = VerificationStatus.Accepted;
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = CreateAuditedAccountService(logger);
        NewAccountRegistration registration = new NewAccountRegistration(
            "123456",
            "NewPlayer",
            "new.player@example.com",
            "StrongPassword1!",
            "en-US",
            null);
        bool expectedLogged = true;

        auditedService.CompleteRegistration(registration);
        bool actualLogged = logger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("88")
            && entry.Message.Contains("n***@example.com"));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void CompleteRegistration_ValidCode_OmitsRawEmailPasswordAndCodeFromLogs()
    {
        const string password = "StrongPassword1!";
        const string code = "123456";
        const string rawEmail = "new.player@example.com";
        Accounts.CreateAccountResult = new AccountResult<long>(88);
        VerificationCodes.VerificationResult = VerificationStatus.Accepted;
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = CreateAuditedAccountService(logger);
        NewAccountRegistration registration = new NewAccountRegistration(
            code,
            "NewPlayer",
            rawEmail,
            password,
            "en-US",
            null);
        bool expectedContainsSecrets = false;

        auditedService.CompleteRegistration(registration);
        bool actualContainsSecrets = logger.Entries.Any(entry =>
            entry.Message.Contains(rawEmail)
            || entry.Message.Contains(password)
            || entry.Message.Contains(code));

        Assert.AreEqual(expectedContainsSecrets, actualContainsSecrets);
    }

    [TestMethod]
    public void ChangePassword_IncorrectCurrentPassword_LogsWarningWithAccountIdAndReason()
    {
        const string token = "auth-token-99";
        LoginAccount account = CreateLoginAccount(accountId: 99, password: "CurrentPassword1!");
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(account);
        AddAuthenticatedSession(token, 99, "player@example.com");
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = CreateAuditedAccountService(logger);
        bool expectedLogged = true;

        auditedService.ChangePassword(token, "WrongPassword1!", "NewPassword1!");
        bool actualLogged = logger.Entries.Any(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("99")
            && entry.Message.Contains("IncorrectCurrentPassword"));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void ChangePassword_IncorrectCurrentPassword_OmitsPasswordsAndTokenFromLogs()
    {
        const string wrongPassword = "WrongPassword1!";
        const string newPassword = "NewPassword1!";
        const string token = "auth-token-99";
        LoginAccount account = CreateLoginAccount(accountId: 99, password: "CurrentPassword1!");
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(account);
        AddAuthenticatedSession(token, 99, "player@example.com");
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = CreateAuditedAccountService(logger);
        bool expectedContainsSecrets = false;

        auditedService.ChangePassword(token, wrongPassword, newPassword);
        bool actualContainsSecrets = logger.Entries.Any(entry =>
            entry.Message.Contains(wrongPassword)
            || entry.Message.Contains(newPassword)
            || entry.Message.Contains(token));

        Assert.AreEqual(expectedContainsSecrets, actualContainsSecrets);
    }

    [TestMethod]
    public void RequestPasswordResetCode_ValidEmail_LogsInformationWithoutRawEmail()
    {
        LoginAccount account = CreateLoginAccount(accountId: 55);
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(account);
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = CreateAuditedAccountService(logger);
        bool expectedLogged = true;

        auditedService.RequestPasswordResetCode(account.EmailAddress);
        bool actualLogged = logger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Password reset code issued for account 55")
            && !entry.Message.Contains(account.EmailAddress));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void ResetPassword_ValidCode_LogsInformationWithoutSecrets()
    {
        const string newPassword = "UpdatedPassword1!";
        const string code = "654321";
        LoginAccount account = CreateLoginAccount(accountId: 55);
        Accounts.FindByEmailResult = new AccountResult<LoginAccount>(account);
        VerificationCodes.VerificationResult = VerificationStatus.Accepted;
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = CreateAuditedAccountService(logger);
        bool expectedLogged = true;

        auditedService.ResetPassword(account.EmailAddress, code, newPassword);
        bool actualLogged = logger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Password reset completed for account 55")
            && !entry.Message.Contains(newPassword)
            && !entry.Message.Contains(code)
            && !entry.Message.Contains(account.EmailAddress));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void DeactivateAccount_ValidPassword_LogsInformationWithoutPasswordOrToken()
    {
        const string password = "CurrentPassword1!";
        const string token = "auth-token-55";
        LoginAccount account = CreateLoginAccount(accountId: 55, password: password);
        Accounts.FindByIdResult = new AccountResult<LoginAccount>(account);
        AddAuthenticatedSession(token, 55, account.EmailAddress);
        RecordingLogger<AccountApplicationService> logger =
            new RecordingLogger<AccountApplicationService>();
        AccountApplicationService auditedService = CreateAuditedAccountService(logger);
        bool expectedLogged = true;

        auditedService.DeactivateAccount(token, password);
        bool actualLogged = logger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Account 55 deactivated")
            && !entry.Message.Contains(password)
            && !entry.Message.Contains(token));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void ChangeUsername_ValidName_LogsInformationWithAccountIdAndUsernameWithoutToken()
    {
        const string token = "profile-token-1";
        AddAuthenticatedSession(token, 33, "old@example.com");
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        RecordingLogger<AppPlayerProfileService> logger =
            new RecordingLogger<AppPlayerProfileService>();
        AppPlayerProfileService profileService = new AppPlayerProfileService(
            Sessions,
            profiles,
            Images,
            logger);
        bool expectedLogged = true;

        profileService.ChangeUsername(token, "NewName33");
        bool actualLogged = logger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("33")
            && entry.Message.Contains("NewName33")
            && !entry.Message.Contains(token));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    private AccountApplicationService CreateAuditedAccountService(
        RecordingLogger<AccountApplicationService> logger)
    {
        return new AccountApplicationService(
            Accounts,
            Images,
            VerificationService,
            Sessions,
            logger);
    }

    private void AddAuthenticatedSession(string token, long accountId, string emailAddress)
    {
        Sessions.TryAddAuthenticated(new AuthenticatedSession(
            token,
            accountId,
            "PlayerOne",
            emailAddress,
            "en-US",
            Clock.UtcNow.AddHours(1)));
    }
}
