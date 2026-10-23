using System;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests;

internal sealed class AccountAccessValidationTests
{
    internal void ValidateCredentials_ValidCredentials_NormalizesAndReturnsActiveAccount()
    {
        // Arrange
        AccountLoginStoreStub accountStore = new AccountLoginStoreStub
        {
            Result = FoundAccount("ACTIVE")
        };
        LoginPasswordHasherStub passwordHasher = new LoginPasswordHasherStub
        {
            VerifyResult = true
        };
        VerifiedLoginService service = new VerifiedLoginService(accountStore, passwordHasher);

        // Act
        LoginAccount account = service.ValidateCredentials(
            "  player@example.com  ", "A\u0301b12345!");

        // Assert
        TestAssert.AreEqual("player@example.com", accountStore.Login);
        TestAssert.AreEqual("Áb12345!", passwordHasher.Password);
        TestAssert.AreEqual("stored-hash", passwordHasher.EncodedHash);
        TestAssert.AreEqual(42L, account.AccountId);
        TestAssert.AreEqual("PlayerOne", account.Username);
    }

    internal void ValidateCredentials_MissingCredentials_RejectsBeforeLookup()
    {
        // Arrange
        AccountLoginStoreStub accountStore = new AccountLoginStoreStub();
        VerifiedLoginService service = new VerifiedLoginService(
            accountStore, new LoginPasswordHasherStub());

        // Act
        SessionRejectedException exception = TestAssert.Throws<SessionRejectedException>(
            () => service.ValidateCredentials("", ""));

        // Assert
        TestAssert.AreEqual(SessionRejection.InvalidCredentials, exception.Reason);
        TestAssert.AreEqual<string?>(null, accountStore.Login);
    }

    internal void ValidateCredentials_UnknownAccount_ReturnsInvalidCredentials()
    {
        // Arrange
        VerifiedLoginService service = new VerifiedLoginService(
            new AccountLoginStoreStub
            {
                Result = new AccountLoginLookupResult(AccountLoginLookupOutcome.NotFound)
            }, new LoginPasswordHasherStub());

        // Act
        SessionRejectedException exception = TestAssert.Throws<SessionRejectedException>(
            () => service.ValidateCredentials("UnknownPlayer", "Password1!"));

        // Assert
        TestAssert.AreEqual(SessionRejection.InvalidCredentials, exception.Reason);
    }

    internal void ValidateCredentials_WrongPassword_ReturnsInvalidCredentials()
    {
        // Arrange
        LoginPasswordHasherStub passwordHasher = new LoginPasswordHasherStub
        {
            VerifyResult = false
        };
        VerifiedLoginService service = new VerifiedLoginService(
            new AccountLoginStoreStub { Result = FoundAccount("ACTIVE") }, passwordHasher);

        // Act
        SessionRejectedException exception = TestAssert.Throws<SessionRejectedException>(
            () => service.ValidateCredentials("PlayerOne", "WrongPassword1!"));

        // Assert
        TestAssert.AreEqual(SessionRejection.InvalidCredentials, exception.Reason);
        TestAssert.AreEqual("WrongPassword1!", passwordHasher.Password);
    }

    internal void ValidateCredentials_InactiveAccount_ReturnsInactiveAccount()
    {
        // Arrange
        VerifiedLoginService service = new VerifiedLoginService(
            new AccountLoginStoreStub { Result = FoundAccount("DISABLED") },
            new LoginPasswordHasherStub { VerifyResult = true });

        // Act
        SessionRejectedException exception = TestAssert.Throws<SessionRejectedException>(
            () => service.ValidateCredentials("PlayerOne", "Password1!"));

        // Assert
        TestAssert.AreEqual(SessionRejection.InactiveAccount, exception.Reason);
    }

    internal void ValidateCredentials_UnavailableStore_PropagatesTechnicalFailure()
    {
        // Arrange
        VerifiedLoginService service = new VerifiedLoginService(
            new AccountLoginStoreStub
            {
                Result = new AccountLoginLookupResult(AccountLoginLookupOutcome.Unavailable)
            }, new LoginPasswordHasherStub());

        // Act
        InvalidOperationException exception = TestAssert.Throws<InvalidOperationException>(
            () => service.ValidateCredentials("PlayerOne", "Password1!"));

        // Assert
        TestAssert.AreEqual("Account lookup is unavailable.", exception.Message);
    }

    internal void SessionStore_CreateFindAndExpire_EnforcesLifetime()
    {
        // Arrange
        MutableUtcClock clock = new MutableUtcClock(
            new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
        InMemoryAuthenticatedSessionStore store = new InMemoryAuthenticatedSessionStore(
            clock,
            TimeSpan.FromMinutes(30));

        // Act
        AuthenticatedSessionRecord first = store.Create(
            42,
            "PlayerOne",
            "player@example.com",
            "en-US");
        AuthenticatedSessionRecord second = store.Create(
            42,
            "PlayerOne",
            "player@example.com",
            "en-US");
        AuthenticatedSessionRecord? active = store.Find(first.SessionToken);
        clock.UtcNow = clock.UtcNow.AddMinutes(31);
        AuthenticatedSessionRecord? expired = store.Find(first.SessionToken);

        // Assert
        TestAssert.AreEqual(false, first.SessionToken == second.SessionToken);
        TestAssert.AreEqual<long?>(42, active?.AccountId);
        TestAssert.AreEqual<AuthenticatedSessionRecord?>(null, expired);
    }

    private static AccountLoginLookupResult FoundAccount(string statusCode)
    {
        return new AccountLoginLookupResult(
            AccountLoginLookupOutcome.Found,
            new LoginAccount(
                42,
                "PlayerOne",
                "player@example.com",
                "stored-hash",
                statusCode,
                "en-US"));
    }
}

internal sealed class AccountLoginStoreStub : IAccountLoginStore
{
    internal string? Login { get; private set; }

    internal AccountLoginLookupResult Result { get; set; } =
        new AccountLoginLookupResult(AccountLoginLookupOutcome.NotFound);

    public Task<AccountLoginLookupResult> FindByLoginAsync(string login)
    {
        Login = login;
        return Task.FromResult(Result);
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
