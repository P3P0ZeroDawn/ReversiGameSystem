using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Sessions;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class AccountAccessValidationTests
{
    [TestMethod]
    public void ValidateCredentials_ValidCredentials_NormalizesAndReturnsActiveAccount()
    {
        // Arrange
        AccountLoginRepositoryStub accountStore = new AccountLoginRepositoryStub
        {
            Result = FoundAccount("ACTIVE")
        };
        LoginPasswordHasherStub passwordHasher = new LoginPasswordHasherStub
        {
            VerifyResult = true
        };
        SessionApplicationService service = CreateApplicationService(
            accountStore,
            passwordHasher);

        // Act
        SessionOperationResult<DateTime> result = service.RequestLoginCode(
            "  player@example.com  ", "A\u0301b12345!");

        // Assert
        TestAssert.AreEqual("player@example.com", accountStore.Login);
        TestAssert.AreEqual("Áb12345!", passwordHasher.Password);
        TestAssert.AreEqual("stored-hash", passwordHasher.EncodedHash);
        TestAssert.AreEqual(SessionOperationStatus.Success, result.Status);
    }

    [TestMethod]
    public void ValidateCredentials_MissingCredentials_RejectsBeforeLookup()
    {
        // Arrange
        AccountLoginRepositoryStub accountStore = new AccountLoginRepositoryStub();
        SessionApplicationService service = CreateApplicationService(
            accountStore,
            new LoginPasswordHasherStub());

        // Act
        SessionOperationResult<DateTime> result = service.RequestLoginCode("", "");

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
                Result = AccountLoginLookupResult.CreateNotFound()
            }, new LoginPasswordHasherStub());

        // Act
        SessionOperationResult<DateTime> result = service.RequestLoginCode(
            "UnknownPlayer",
            "Password1!");

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InvalidCredentials, result.Status);
    }

    [TestMethod]
    public void ValidateCredentials_WrongPassword_ReturnsInvalidCredentials()
    {
        // Arrange
        LoginPasswordHasherStub passwordHasher = new LoginPasswordHasherStub
        {
            VerifyResult = false
        };
        SessionApplicationService service = CreateApplicationService(
            new AccountLoginRepositoryStub { Result = FoundAccount("ACTIVE") }, passwordHasher);

        // Act
        SessionOperationResult<DateTime> result = service.RequestLoginCode(
            "PlayerOne",
            "WrongPassword1!");

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InvalidCredentials, result.Status);
        TestAssert.AreEqual("WrongPassword1!", passwordHasher.Password);
    }

    [TestMethod]
    public void ValidateCredentials_InactiveAccount_ReturnsInactiveAccount()
    {
        // Arrange
        SessionApplicationService service = CreateApplicationService(
            new AccountLoginRepositoryStub { Result = FoundAccount("DISABLED") },
            new LoginPasswordHasherStub { VerifyResult = true });

        // Act
        SessionOperationResult<DateTime> result = service.RequestLoginCode(
            "PlayerOne",
            "Password1!");

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
        SessionApplicationService service = CreateApplicationService(
            accountStore,
            new LoginPasswordHasherStub());

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
        SessionRepository store = new SessionRepository(
            clock,
            TimeSpan.FromMinutes(30));

        // Act
        LoginAccount account = FoundAccount("ACTIVE").GetFoundAccount();
        AuthenticatedSession first = store.Create(account);
        AuthenticatedSession second = store.Create(account);
        bool found = store.TryGet(first.SessionToken, out AuthenticatedSession? active);
        bool missing = store.TryGet("unknown", out AuthenticatedSession? absent);
        bool empty = store.TryGet(string.Empty, out AuthenticatedSession? emptySession);
        store.Revoke(second.SessionToken);
        bool revoked = store.TryGet(second.SessionToken, out AuthenticatedSession? closed);
        clock.UtcNow = first.ExpiresAtUtc;
        bool expired = store.TryGet(first.SessionToken, out AuthenticatedSession? stale);
        clock.UtcNow = clock.UtcNow.AddMinutes(-1);
        bool removed = store.TryGet(first.SessionToken, out _);

        // Assert
        TestAssert.AreEqual(false, first.SessionToken == second.SessionToken);
        TestAssert.AreEqual(true, found);
        TestAssert.AreEqual<long?>(42, active?.AccountId);
        TestAssert.AreEqual(account.Username, first.Username);
        TestAssert.AreEqual(account.EmailAddress, first.EmailAddress);
        TestAssert.AreEqual(account.PreferredLocaleCode, first.PreferredLocaleCode);
        TestAssert.AreEqual(false, missing);
        TestAssert.AreEqual<AuthenticatedSession?>(null, absent);
        TestAssert.AreEqual(false, empty);
        TestAssert.AreEqual<AuthenticatedSession?>(null, emptySession);
        TestAssert.AreEqual(false, revoked);
        TestAssert.AreEqual<AuthenticatedSession?>(null, closed);
        TestAssert.AreEqual(false, expired);
        TestAssert.AreEqual<AuthenticatedSession?>(null, stale);
        TestAssert.AreEqual(false, removed);
    }

    private static AccountLoginLookupResult FoundAccount(string statusCode)
    {
        return AccountLoginLookupResult.CreateFound(
            new LoginAccount(
                42,
                "PlayerOne",
                "player@example.com",
                "stored-hash",
                statusCode,
                "en-US"));
    }

    private static SessionApplicationService CreateApplicationService(
        IAccountLoginRepository accounts,
        IPasswordHasher passwordHasher)
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
            passwordHasher,
            verificationCodes,
            new SessionRepository(clock));
    }
}

internal sealed class AccountLoginRepositoryStub : IAccountLoginRepository
{
    internal string? Login { get; private set; }

    internal AccountLoginLookupResult Result { get; set; } =
        AccountLoginLookupResult.CreateNotFound();

    internal Exception? Failure { get; set; }

    public Task<AccountLoginLookupResult> FindByLoginAsync(string login)
    {
        if (Failure != null)
        {
            throw Failure;
        }

        Login = login;
        return Task.FromResult(Result);
    }
}

internal sealed class VerificationEmailSenderStub : IVerificationEmailSender
{
    public void Send(VerificationEmail email)
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

internal sealed class LoginPasswordHasherStub : IPasswordHasher
{
    internal bool VerifyResult { get; set; }

    internal string? Password { get; private set; }

    internal string? EncodedHash { get; private set; }

    public string Hash(string password)
    {
        return "stored-hash";
    }

    public bool Verify(string password, string encodedHash)
    {
        Password = password;
        EncodedHash = encodedHash;
        return VerifyResult;
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
