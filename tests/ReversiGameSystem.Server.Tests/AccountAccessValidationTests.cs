using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class AccountAccessValidationTests
{
    private static readonly string ActivePasswordHash = PasswordHasher.Hash("Áb12345!");

    [TestMethod]
    public void ValidateCredentials_ValidCredentials_NormalizesAndReturnsActiveAccount()
    {
        // Arrange
        AccountLoginRepositoryStub accountStore = new AccountLoginRepositoryStub
        {
            Result = FoundAccount("ACTIVE")
        };
        SessionApplicationService service = CreateApplicationService(accountStore);

        // Act
        SessionResult<DateTime> result = service.RequestLoginCode(
            "  player@example.com  ", "A\u0301b12345!");

        // Assert
        TestAssert.AreEqual("player@example.com", accountStore.Login);
        TestAssert.AreEqual(SessionOperationStatus.Success, result.Status);
    }

    [TestMethod]
    public void ValidateCredentials_MissingCredentials_RejectsBeforeLookup()
    {
        // Arrange
        AccountLoginRepositoryStub accountStore = new AccountLoginRepositoryStub();
        SessionApplicationService service = CreateApplicationService(accountStore);

        // Act
        SessionResult<DateTime> result = service.RequestLoginCode("", "");

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InvalidCredentials, result.Status);
        TestAssert.AreEqual<string?>(null, accountStore.Login);
    }

    [TestMethod]
    public void ValidateCredentials_UnknownAccount_ReturnsInvalidCredentials()
    {
        // Arrange
        SessionApplicationService service = CreateApplicationService(
            new AccountLoginRepositoryStub
            {
                Result = new AccountResult<LoginAccount>(AccountStatus.AccountNotFound)
            });

        // Act
        SessionResult<DateTime> result = service.RequestLoginCode(
            "UnknownPlayer",
            "Password1!");

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InvalidCredentials, result.Status);
    }

    [TestMethod]
    public void ValidateCredentials_WrongPassword_ReturnsInvalidCredentials()
    {
        // Arrange
        SessionApplicationService service = CreateApplicationService(
            new AccountLoginRepositoryStub { Result = FoundAccount("ACTIVE") });

        // Act
        SessionResult<DateTime> result = service.RequestLoginCode(
            "PlayerOne",
            "WrongPassword1!");

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InvalidCredentials, result.Status);
    }

    [TestMethod]
    public void ValidateCredentials_InactiveAccount_ReturnsInactiveAccount()
    {
        // Arrange
        SessionApplicationService service = CreateApplicationService(
            new AccountLoginRepositoryStub { Result = FoundAccount("DISABLED") });

        // Act
        SessionResult<DateTime> result = service.RequestLoginCode(
            "PlayerOne",
            "Áb12345!");

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InactiveAccount, result.Status);
    }

    [TestMethod]
    public void ValidateCredentials_UnavailableStore_PropagatesTechnicalFailure()
    {
        // Arrange
        AccountLoginRepositoryStub accountStore = new AccountLoginRepositoryStub
        {
            Failure = new InvalidOperationException("Account lookup is unavailable.")
        };
        SessionApplicationService service = CreateApplicationService(accountStore);

        // Act
        InvalidOperationException exception = TestAssert.Throws<InvalidOperationException>(
            () => service.RequestLoginCode("PlayerOne", "Password1!"));

        // Assert
        TestAssert.AreEqual("Account lookup is unavailable.", exception.Message);
    }

    [TestMethod]
    public void SessionStore_CreateFindAndExpire_EnforcesLifetime()
    {
        // Arrange
        MutableUtcClock clock = new MutableUtcClock(
            new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
        SessionStore store = new SessionStore(
            clock,
            TimeSpan.FromMinutes(30));

        // Act
        LoginAccount account = FoundAccount("ACTIVE").Value!;
        AuthenticatedSession first = new AuthenticatedSession(
            "token-1", account.AccountId, account.Username, account.EmailAddress,
            account.PreferredLocaleCode, store.CalculateExpirationUtc());
        AuthenticatedSession second = new AuthenticatedSession(
            "token-2", account.AccountId, account.Username, account.EmailAddress,
            account.PreferredLocaleCode, store.CalculateExpirationUtc());
        store.TryAddAuthenticated(first);
        store.TryAddAuthenticated(second);

        SessionResult<AuthenticatedSession> found = store.FindAuthenticated(first.SessionToken);
        SessionResult<AuthenticatedSession> missing = store.FindAuthenticated("unknown");
        SessionResult<AuthenticatedSession> empty = store.FindAuthenticated(string.Empty);
        store.RevokeSession(second.SessionToken);
        SessionResult<AuthenticatedSession> revoked = store.FindAuthenticated(second.SessionToken);
        clock.UtcNow = first.ExpiresAtUtc;
        SessionResult<AuthenticatedSession> expired = store.FindAuthenticated(first.SessionToken);
        clock.UtcNow = clock.UtcNow.AddMinutes(-1);
        SessionResult<AuthenticatedSession> removed = store.FindAuthenticated(first.SessionToken);

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.Success, found.Status);
        TestAssert.AreEqual<long?>(42, found.Value?.AccountId);
        TestAssert.AreEqual(SessionOperationStatus.InvalidSession, missing.Status);
        TestAssert.AreEqual(SessionOperationStatus.MissingSessionToken, empty.Status);
        TestAssert.AreEqual(SessionOperationStatus.InvalidSession, revoked.Status);
        TestAssert.AreEqual(SessionOperationStatus.InvalidSession, expired.Status);
        TestAssert.AreEqual(SessionOperationStatus.InvalidSession, removed.Status);
    }

    private static AccountResult<LoginAccount> FoundAccount(string statusCode)
    {
        return new AccountResult<LoginAccount>(
            new LoginAccount(
                42,
                "PlayerOne",
                "player@example.com",
                ActivePasswordHash,
                statusCode,
                "en-US"));
    }

    private static SessionApplicationService CreateApplicationService(
        IAccountRepository accounts)
    {
        MutableUtcClock clock = new MutableUtcClock(
            new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
        VerificationCodeIssuer issuer = new VerificationCodeIssuer(
            new VerificationEmailSenderStub(),
            new VerificationCodeProtector(new byte[32]),
            clock);
        VerificationCodeService verificationCodes = new VerificationCodeService(
            issuer,
            new VerificationCodeRepositoryStub(),
            clock);
        return new SessionApplicationService(
            accounts,
            verificationCodes,
            new SessionStore(clock));
    }
}

internal sealed class AccountLoginRepositoryStub : IAccountRepository
{
    internal string? Login { get; private set; }

    internal AccountResult<LoginAccount> Result { get; set; } =
        new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);

    internal Exception? Failure { get; set; }

    public AccountResult<LoginAccount> FindByLogin(string login)
    {
        if (Failure != null)
        {
            throw Failure;
        }

        Login = login;
        return Result;
    }

    public AccountResult<LoginAccount> FindByEmail(string emailAddress)
    {
        throw new NotSupportedException();
    }

    public AccountResult<LoginAccount> FindById(long accountId)
    {
        throw new NotSupportedException();
    }

    public AccountStatus CheckRegistrationAvailability(string username, string emailAddress)
    {
        throw new NotSupportedException();
    }

    public AccountResult<long> CreateAccount(AccountRegistrationData registration)
    {
        throw new NotSupportedException();
    }

    public AccountStatus UpdatePasswordHash(long accountId, string newPasswordHash)
    {
        throw new NotSupportedException();
    }

    public AccountStatus DeactivateAccount(long accountId)
    {
        throw new NotSupportedException();
    }
}

internal sealed class VerificationEmailSenderStub : IEmailSender
{
    public void Send(EmailMessage email)
    {
    }
}

internal sealed class VerificationCodeRepositoryStub : IVerificationCodeRepository
{
    public void Replace(PendingVerificationCode code)
    {
    }

    public VerificationStatus Consume(VerificationAttempt attempt)
    {
        return VerificationStatus.Accepted;
    }
}

internal sealed class MutableUtcClock : IUtcClock
{
    public MutableUtcClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }
}
