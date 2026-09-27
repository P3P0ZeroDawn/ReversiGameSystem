using System;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

public interface IAccountLoginStore
{
    Task<AccountLoginLookupResult> FindByLoginAsync(string login);
}

public interface IAuthenticatedSessionStore
{
    Task<AuthenticatedSessionRecord> CreateAsync(
        long accountId,
        string username,
        string emailAddress);

    Task<AuthenticatedSessionRecord?> FindAsync(string sessionToken);
}

public sealed class AccountLoginLookupResult
{
    public AccountLoginLookupResult(
        AccountLoginLookupOutcome outcome,
        LoginAccount? account = null)
    {
        Outcome = outcome;
        Account = account;
    }

    public AccountLoginLookupOutcome Outcome { get; }

    public LoginAccount? Account { get; }
}

public enum AccountLoginLookupOutcome
{
    Found,
    NotFound,
    Unavailable
}

public sealed class LoginAccount
{
    public LoginAccount(
        long accountId,
        string username,
        string emailAddress,
        string passwordHash,
        string statusCode)
    {
        AccountId = accountId;
        Username = username;
        EmailAddress = emailAddress;
        PasswordHash = passwordHash;
        StatusCode = statusCode;
    }

    public long AccountId { get; }

    public string Username { get; }

    public string EmailAddress { get; }

    public string PasswordHash { get; }

    public string StatusCode { get; }
}

public sealed class AuthenticatedSessionRecord
{
    public AuthenticatedSessionRecord(
        string sessionToken,
        long accountId,
        string username,
        string emailAddress,
        DateTime expiresAtUtc)
    {
        SessionToken = sessionToken;
        AccountId = accountId;
        Username = username;
        EmailAddress = emailAddress;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string SessionToken { get; }

    public long AccountId { get; }

    public string Username { get; }

    public string EmailAddress { get; }

    public DateTime ExpiresAtUtc { get; }
}
