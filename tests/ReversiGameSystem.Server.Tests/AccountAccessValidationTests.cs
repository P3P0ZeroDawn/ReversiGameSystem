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
        Assert.AreEqual("player@example.com", accountStore.Login);
        Assert.AreEqual(SessionStatus.Success, result.Status);
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
        Assert.AreEqual(SessionStatus.InvalidCredentials, result.Status);
        Assert.AreEqual<string?>(null, accountStore.Login);
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
        Assert.AreEqual(SessionStatus.InvalidCredentials, result.Status);
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
        Assert.AreEqual(SessionStatus.InvalidCredentials, result.Status);
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
        Assert.AreEqual(SessionStatus.InactiveAccount, result.Status);
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
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => service.RequestLoginCode("PlayerOne", "Password1!"));

        // Assert
        Assert.AreEqual("Account lookup is unavailable.", exception.Message);
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
        Assert.AreEqual(SessionStatus.Success, found.Status);
        Assert.AreEqual<long?>(42, found.Value?.AccountId);
        Assert.AreEqual(SessionStatus.InvalidSession, missing.Status);
        Assert.AreEqual(SessionStatus.MissingSessionToken, empty.Status);
        Assert.AreEqual(SessionStatus.InvalidSession, revoked.Status);
        Assert.AreEqual(SessionStatus.InvalidSession, expired.Status);
        Assert.AreEqual(SessionStatus.InvalidSession, removed.Status);
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
