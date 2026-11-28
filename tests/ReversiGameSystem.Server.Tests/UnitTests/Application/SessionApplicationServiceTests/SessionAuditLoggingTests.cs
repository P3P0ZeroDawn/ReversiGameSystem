using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class SessionAuditLoggingTests : SessionApplicationServiceTestBase
{
    [TestMethod]
    public void RequestLoginCode_ValidCredentials_LogsInformationWithAccountId()
    {
        const string password = "SecretPassword1!";
        LoginAccount account = CreateLoginAccount(accountId: 77, password: password);
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(account);
        RecordingLogger<SessionApplicationService> logger =
            new RecordingLogger<SessionApplicationService>();
        SessionApplicationService auditedService = CreateAuditedService(logger);
        bool expectedLogged = true;

        auditedService.RequestLoginCode(account.EmailAddress, password);
        bool actualLogged = logger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("77")
            && entry.Message.Contains("Login verification code issued"));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void RequestLoginCode_ValidCredentials_OmitsPasswordAndRawEmailFromLogs()
    {
        const string password = "SecretPassword1!";
        LoginAccount account = CreateLoginAccount(accountId: 77, password: password);
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(account);
        RecordingLogger<SessionApplicationService> logger =
            new RecordingLogger<SessionApplicationService>();
        SessionApplicationService auditedService = CreateAuditedService(logger);
        bool expectedContainsSecrets = false;

        auditedService.RequestLoginCode(account.EmailAddress, password);
        bool actualContainsSecrets = logger.Entries.Any(entry =>
            entry.Message.Contains(password)
            || entry.Message.Contains(account.EmailAddress));

        Assert.AreEqual(expectedContainsSecrets, actualContainsSecrets);
    }

    [TestMethod]
    public void RequestLoginCode_InvalidCredentials_LogsWarningWithMaskedEmail()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        RecordingLogger<SessionApplicationService> logger =
            new RecordingLogger<SessionApplicationService>();
        SessionApplicationService auditedService = CreateAuditedService(logger);
        bool expectedLogged = true;

        auditedService.RequestLoginCode("unknown.user@example.com", "WrongPass1!");
        bool actualLogged = logger.Entries.Any(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("u***@example.com"));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void RequestLoginCode_InvalidCredentials_OmitsRawEmailAndPasswordFromLogs()
    {
        const string rawEmail = "unknown.user@example.com";
        const string password = "WrongPass1!";
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        RecordingLogger<SessionApplicationService> logger =
            new RecordingLogger<SessionApplicationService>();
        SessionApplicationService auditedService = CreateAuditedService(logger);
        bool expectedContainsSecrets = false;

        auditedService.RequestLoginCode(rawEmail, password);
        bool actualContainsSecrets = logger.Entries.Any(entry =>
            entry.Message.Contains(rawEmail)
            || entry.Message.Contains(password));

        Assert.AreEqual(expectedContainsSecrets, actualContainsSecrets);
    }

    [TestMethod]
    public void CompleteLogin_ValidCode_LogsInformationWithAccountId()
    {
        LoginAccount account = CreateLoginAccount(accountId: 77);
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(account);
        VerificationCodes.VerificationResult = VerificationStatus.Accepted;
        RecordingLogger<SessionApplicationService> logger =
            new RecordingLogger<SessionApplicationService>();
        SessionApplicationService auditedService = CreateAuditedService(logger);
        bool expectedLogged = true;

        auditedService.CompleteLogin(account.EmailAddress, "654321");
        bool actualLogged = logger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("77")
            && entry.Message.Contains("Authenticated session opened"));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void CompleteLogin_ValidCode_OmitsCodeTokenAndEmailFromLogs()
    {
        const string verificationCode = "654321";
        LoginAccount account = CreateLoginAccount(accountId: 77);
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(account);
        VerificationCodes.VerificationResult = VerificationStatus.Accepted;
        RecordingLogger<SessionApplicationService> logger =
            new RecordingLogger<SessionApplicationService>();
        SessionApplicationService auditedService = CreateAuditedService(logger);
        bool expectedContainsSecrets = false;

        SessionResult<AuthenticatedSession> result = auditedService.CompleteLogin(
            account.EmailAddress,
            verificationCode);
        string sessionToken = result.Value!.SessionToken;
        bool actualContainsSecrets = logger.Entries.Any(entry =>
            entry.Message.Contains(verificationCode)
            || entry.Message.Contains(sessionToken)
            || entry.Message.Contains(account.EmailAddress));

        Assert.AreEqual(expectedContainsSecrets, actualContainsSecrets);
    }

    [TestMethod]
    public void CloseSession_AuthenticatedSession_LogsInformationWithAccountId()
    {
        const string sessionToken = "auth-token-77";
        RecordingLogger<SessionStore> storeLogger = new RecordingLogger<SessionStore>();
        SessionStore auditedStore = new SessionStore(Clock, storeLogger);
        auditedStore.TryAddAuthenticated(new AuthenticatedSession(
            sessionToken,
            77,
            "PlayerOne",
            "player@example.com",
            "en-US",
            Clock.UtcNow.AddHours(1)));
        SessionApplicationService auditedService = CreateAuditedService(
            new RecordingLogger<SessionApplicationService>(),
            auditedStore);
        bool expectedLogged = true;

        auditedService.CloseSession(sessionToken);
        bool actualLogged = storeLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("77")
            && entry.Message.Contains("Authenticated session closed")
            && !entry.Message.Contains(sessionToken));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    [TestMethod]
    public void RemoveExpiredSessions_BeforeExpiration_LogsNothing()
    {
        RecordingLogger<SessionStore> storeLogger = new RecordingLogger<SessionStore>();
        SessionStore auditedStore = new SessionStore(
            Clock,
            TimeSpan.FromMinutes(10),
            storeLogger);
        auditedStore.TryReserveGuest(new GuestSession(
            "guest-token-1",
            "Guest-GuestOne",
            Clock.UtcNow.AddMinutes(10)));
        int expectedLogCount = 0;

        auditedStore.RemoveExpiredSessions();
        int actualLogCount = storeLogger.Entries.Count;

        Assert.AreEqual(expectedLogCount, actualLogCount);
    }

    [TestMethod]
    public void RemoveExpiredSessions_AfterExpiration_LogsInformationWithGuestNameWithoutToken()
    {
        const string guestToken = "guest-token-1";
        RecordingLogger<SessionStore> storeLogger = new RecordingLogger<SessionStore>();
        SessionStore auditedStore = new SessionStore(
            Clock,
            TimeSpan.FromMinutes(10),
            storeLogger);
        auditedStore.TryReserveGuest(new GuestSession(
            guestToken,
            "Guest-GuestOne",
            Clock.UtcNow.AddMinutes(10)));
        Clock.UtcNow = Clock.UtcNow.AddMinutes(11);
        bool expectedLogged = true;

        auditedStore.RemoveExpiredSessions();
        bool actualLogged = storeLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Guest-GuestOne")
            && !entry.Message.Contains(guestToken));

        Assert.AreEqual(expectedLogged, actualLogged);
    }

    private SessionApplicationService CreateAuditedService(
        RecordingLogger<SessionApplicationService> serviceLogger,
        SessionStore? store = null)
    {
        return new SessionApplicationService(
            Accounts,
            VerificationService,
            store ?? Sessions,
            serviceLogger);
    }
}
