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
    public void LoginAndCloseSession_SuccessfulFlow_LogsAuditEventsWithoutSecrets()
    {
        const string password = "SecretPassword1!";
        const string verificationCode = "654321";
        LoginAccount account = CreateLoginAccount(accountId: 77, password: password);
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(account);
        VerificationCodes.VerificationResult = VerificationStatus.Accepted;
        RecordingLogger<SessionStore> storeLogger = new RecordingLogger<SessionStore>();
        RecordingLogger<SessionApplicationService> serviceLogger =
            new RecordingLogger<SessionApplicationService>();
        SessionStore auditedStore = new SessionStore(Clock, storeLogger);
        SessionApplicationService auditedService = new SessionApplicationService(
            Accounts,
            VerificationService,
            auditedStore,
            serviceLogger);

        SessionResult<DateTime> codeResult = auditedService.RequestLoginCode(
            account.EmailAddress,
            password);
        Assert.AreEqual(SessionStatus.Success, codeResult.Status);

        SessionResult<AuthenticatedSession> loginResult = auditedService.CompleteLogin(
            account.EmailAddress,
            verificationCode);
        Assert.AreEqual(SessionStatus.Success, loginResult.Status);
        string sessionToken = loginResult.Value!.SessionToken;

        SessionStatus closeStatus = auditedService.CloseSession(sessionToken);
        Assert.AreEqual(SessionStatus.Success, closeStatus);

        Assert.IsTrue(serviceLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("77")
            && entry.Message.Contains("Login verification code issued")));
        Assert.IsTrue(serviceLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("77")
            && entry.Message.Contains("Authenticated session opened")));
        Assert.IsTrue(storeLogger.Entries.Any(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("77")
            && entry.Message.Contains("Authenticated session closed")));

        string allMessages = string.Join(
            "\n",
            serviceLogger.Entries.Select(e => e.Message)
                .Concat(storeLogger.Entries.Select(e => e.Message)));
        Assert.IsFalse(allMessages.Contains(password));
        Assert.IsFalse(allMessages.Contains(verificationCode));
        Assert.IsFalse(allMessages.Contains(sessionToken));
        Assert.IsFalse(allMessages.Contains(account.EmailAddress));
    }

    [TestMethod]
    public void RequestLoginCode_InvalidCredentials_LogsWarningWithMaskedEmail()
    {
        Accounts.FindByLoginResult = new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        RecordingLogger<SessionApplicationService> serviceLogger =
            new RecordingLogger<SessionApplicationService>();
        SessionApplicationService auditedService = new SessionApplicationService(
            Accounts,
            VerificationService,
            Sessions,
            serviceLogger);

        SessionResult<DateTime> result = auditedService.RequestLoginCode(
            "unknown.user@example.com",
            "WrongPass1!");

        Assert.AreEqual(SessionStatus.InvalidCredentials, result.Status);
        RecordedLogEntry entry = serviceLogger.Entries.Single();
        Assert.AreEqual(LogLevel.Warning, entry.Level);
        Assert.IsTrue(entry.Message.Contains("u***@example.com"));
        Assert.IsFalse(entry.Message.Contains("unknown.user@example.com"));
        Assert.IsFalse(entry.Message.Contains("WrongPass1!"));
    }

    [TestMethod]
    public void GuestSession_OpenAndExpire_LogsSanitizedDisplayNameOnlyWhenExpired()
    {
        RecordingLogger<SessionStore> storeLogger = new RecordingLogger<SessionStore>();
        RecordingLogger<SessionApplicationService> serviceLogger =
            new RecordingLogger<SessionApplicationService>();
        SessionStore auditedStore = new SessionStore(
            Clock,
            TimeSpan.FromMinutes(10),
            storeLogger);
        SessionApplicationService auditedService = new SessionApplicationService(
            Accounts,
            VerificationService,
            auditedStore,
            serviceLogger);

        SessionResult<GuestSession> guestResult = auditedService.OpenGuestSession("GuestOne");
        Assert.AreEqual(SessionStatus.Success, guestResult.Status);
        string guestToken = guestResult.Value!.SessionToken;

        auditedStore.RemoveExpiredSessions();
        Assert.AreEqual(0, storeLogger.Entries.Count);

        Clock.UtcNow = Clock.UtcNow.AddMinutes(11);
        auditedStore.RemoveExpiredSessions();

        Assert.AreEqual(1, storeLogger.Entries.Count);
        Assert.AreEqual(LogLevel.Information, storeLogger.Entries[0].Level);
        Assert.IsTrue(storeLogger.Entries[0].Message.Contains("Guest-GuestOne"));
        Assert.IsFalse(storeLogger.Entries[0].Message.Contains(guestToken));
        Assert.IsFalse(serviceLogger.Entries.Any(e => e.Message.Contains(guestToken)));
    }
}
