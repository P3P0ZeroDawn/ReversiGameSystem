using System;
using System.Threading.Tasks;
using ReversiGameSystem.LocalRuntime.Sessions;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Tests;

internal sealed class LoginHandlerTests
{
    internal void ExecuteAsync_ValidCredentials_CreatesSession()
    {
        AccountLoginStoreStub accountStore = new AccountLoginStoreStub
        {
            Result = FoundAccount("ACTIVE")
        };
        LoginPasswordHasherStub passwordHasher = new LoginPasswordHasherStub
        {
            VerifyResult = true
        };
        AuthenticatedSessionStoreStub sessionStore = new AuthenticatedSessionStoreStub();
        LoginHandler handler = new LoginHandler(accountStore, passwordHasher, sessionStore);

        LoginResult result = handler.ExecuteAsync(
            new LoginCommand("  player@example.com  ", "A\u0301b12345!"))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LoginOutcome.Success, result.Outcome);
        TestAssert.AreEqual("player@example.com", accountStore.Login);
        TestAssert.AreEqual("Áb12345!", passwordHasher.Password);
        TestAssert.AreEqual("stored-hash", passwordHasher.EncodedHash);
        TestAssert.AreEqual<long?>(42, sessionStore.AccountId);
        TestAssert.AreEqual("session-token", result.Session?.SessionToken);
        TestAssert.AreEqual("PlayerOne", result.Session?.Username);
    }

    internal void ExecuteAsync_MissingCredentials_ReturnsInvalidRequest()
    {
        AccountLoginStoreStub accountStore = new AccountLoginStoreStub();
        LoginHandler handler = new LoginHandler(
            accountStore,
            new LoginPasswordHasherStub(),
            new AuthenticatedSessionStoreStub());

        LoginResult result = handler.ExecuteAsync(new LoginCommand("", ""))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LoginOutcome.InvalidRequest, result.Outcome);
        TestAssert.AreEqual<string?>(null, accountStore.Login);
    }

    internal void ExecuteAsync_UnknownAccount_ReturnsInvalidCredentials()
    {
        AccountLoginStoreStub accountStore = new AccountLoginStoreStub
        {
            Result = new AccountLoginLookupResult(AccountLoginLookupOutcome.NotFound)
        };
        LoginHandler handler = new LoginHandler(
            accountStore,
            new LoginPasswordHasherStub(),
            new AuthenticatedSessionStoreStub());

        LoginResult result = handler.ExecuteAsync(
            new LoginCommand("UnknownPlayer", "Password1!"))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LoginOutcome.InvalidCredentials, result.Outcome);
    }

    internal void ExecuteAsync_WrongPassword_ReturnsInvalidCredentials()
    {
        LoginPasswordHasherStub passwordHasher = new LoginPasswordHasherStub
        {
            VerifyResult = false
        };
        AuthenticatedSessionStoreStub sessionStore = new AuthenticatedSessionStoreStub();
        LoginHandler handler = new LoginHandler(
            new AccountLoginStoreStub { Result = FoundAccount("ACTIVE") },
            passwordHasher,
            sessionStore);

        LoginResult result = handler.ExecuteAsync(
            new LoginCommand("PlayerOne", "WrongPassword1!"))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LoginOutcome.InvalidCredentials, result.Outcome);
        TestAssert.AreEqual<long?>(null, sessionStore.AccountId);
    }

    internal void ExecuteAsync_InactiveAccount_ReturnsAccountInactive()
    {
        AuthenticatedSessionStoreStub sessionStore = new AuthenticatedSessionStoreStub();
        LoginHandler handler = new LoginHandler(
            new AccountLoginStoreStub { Result = FoundAccount("DISABLED") },
            new LoginPasswordHasherStub { VerifyResult = true },
            sessionStore);

        LoginResult result = handler.ExecuteAsync(
            new LoginCommand("PlayerOne", "Password1!"))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LoginOutcome.AccountInactive, result.Outcome);
        TestAssert.AreEqual<long?>(null, sessionStore.AccountId);
    }

    internal void ExecuteAsync_UnavailableStore_ReturnsServiceUnavailable()
    {
        LoginHandler handler = new LoginHandler(
            new AccountLoginStoreStub
            {
                Result = new AccountLoginLookupResult(AccountLoginLookupOutcome.Unavailable)
            },
            new LoginPasswordHasherStub(),
            new AuthenticatedSessionStoreStub());

        LoginResult result = handler.ExecuteAsync(
            new LoginCommand("PlayerOne", "Password1!"))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LoginOutcome.ServiceUnavailable, result.Outcome);
    }

    internal void SessionStore_CreateFindAndExpire_EnforcesLifetime()
    {
        MutableUtcClock clock = new MutableUtcClock(
            new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
        InMemoryAuthenticatedSessionStore store = new InMemoryAuthenticatedSessionStore(
            clock,
            TimeSpan.FromMinutes(30));

        AuthenticatedSessionRecord first = store.CreateAsync(
            42,
            "PlayerOne",
            "player@example.com").GetAwaiter().GetResult();
        AuthenticatedSessionRecord second = store.CreateAsync(
            42,
            "PlayerOne",
            "player@example.com").GetAwaiter().GetResult();
        AuthenticatedSessionRecord? active = store.FindAsync(first.SessionToken)
            .GetAwaiter()
            .GetResult();
        clock.UtcNow = clock.UtcNow.AddMinutes(31);
        AuthenticatedSessionRecord? expired = store.FindAsync(first.SessionToken)
            .GetAwaiter()
            .GetResult();

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
                statusCode));
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

internal sealed class AuthenticatedSessionStoreStub : IAuthenticatedSessionStore
{
    internal long? AccountId { get; private set; }

    public Task<AuthenticatedSessionRecord> CreateAsync(
        long accountId,
        string username,
        string emailAddress)
    {
        AccountId = accountId;
        return Task.FromResult(
            new AuthenticatedSessionRecord(
                "session-token",
                accountId,
                username,
                emailAddress,
                DateTime.UtcNow.AddHours(8)));
    }

    public Task<AuthenticatedSessionRecord?> FindAsync(string sessionToken)
    {
        return Task.FromResult<AuthenticatedSessionRecord?>(null);
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
