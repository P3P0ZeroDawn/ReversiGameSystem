using System;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Defines the account login store contract.
/// </summary>
public interface IAccountLoginStore
{
    /// <summary>
    /// Performs the find by login async operation.
    /// </summary>
    /// <param name="login">The login.</param>
    /// <returns>The result of the operation.</returns>
    Task<AccountLoginLookupResult> FindByLoginAsync(string login);
}

/// <summary>
/// Defines the authenticated session store contract.
/// </summary>
public interface IAuthenticatedSessionStore
{
    /// <summary>
    /// Performs the create async operation.
    /// </summary>
    /// <param name="accountId">The account id.</param>
    /// <param name="username">The username.</param>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="preferredLocaleCode">The preferred locale code.</param>
    /// <returns>The result of the operation.</returns>
    Task<AuthenticatedSessionRecord> CreateAsync(
        long accountId,
        string username,
        string emailAddress,
        string preferredLocaleCode);

    /// <summary>
    /// Performs the find async operation.
    /// </summary>
    /// <param name="sessionToken">The session token.</param>
    /// <returns>The result of the operation.</returns>
    Task<AuthenticatedSessionRecord?> FindAsync(string sessionToken);
}

/// <summary>
/// Represents account login lookup result.
/// </summary>
public sealed class AccountLoginLookupResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountLoginLookupResult"/> class.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="account">The account.</param>
    public AccountLoginLookupResult(
        AccountLoginLookupOutcome outcome,
        LoginAccount? account = null)
    {
        Outcome = outcome;
        Account = account;
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public AccountLoginLookupOutcome Outcome { get; }

    /// <summary>
    /// Gets the account.
    /// </summary>
    public LoginAccount? Account { get; }
}

/// <summary>
/// Defines the supported account login lookup outcome values.
/// </summary>
public enum AccountLoginLookupOutcome
{
    /// <summary>
    /// Represents found.
    /// </summary>
    Found,
    /// <summary>
    /// Represents not found.
    /// </summary>
    NotFound,
    /// <summary>
    /// Represents unavailable.
    /// </summary>
    Unavailable
}

/// <summary>
/// Represents login account.
/// </summary>
public sealed class LoginAccount
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginAccount"/> class.
    /// </summary>
    /// <param name="accountId">The account id.</param>
    /// <param name="username">The username.</param>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="passwordHash">The password hash.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="preferredLocaleCode">The preferred locale code.</param>
    public LoginAccount(
        long accountId,
        string username,
        string emailAddress,
        string passwordHash,
        string statusCode,
        string preferredLocaleCode)
    {
        AccountId = accountId;
        Username = username;
        EmailAddress = emailAddress;
        PasswordHash = passwordHash;
        StatusCode = statusCode;
        PreferredLocaleCode = preferredLocaleCode;
    }

    /// <summary>
    /// Gets the account id.
    /// </summary>
    public long AccountId { get; }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public string EmailAddress { get; }

    /// <summary>
    /// Gets the password hash.
    /// </summary>
    public string PasswordHash { get; }

    /// <summary>
    /// Gets the status code.
    /// </summary>
    public string StatusCode { get; }

    /// <summary>
    /// Gets the preferred locale code.
    /// </summary>
    public string PreferredLocaleCode { get; }
}

/// <summary>
/// Represents authenticated session record.
/// </summary>
public sealed class AuthenticatedSessionRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatedSessionRecord"/> class.
    /// </summary>
    /// <param name="sessionToken">The session token.</param>
    /// <param name="accountId">The account id.</param>
    /// <param name="username">The username.</param>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="preferredLocaleCode">The preferred locale code.</param>
    /// <param name="expiresAtUtc">The expires at utc.</param>
    public AuthenticatedSessionRecord(
        string sessionToken,
        long accountId,
        string username,
        string emailAddress,
        string preferredLocaleCode,
        DateTime expiresAtUtc)
    {
        SessionToken = sessionToken;
        AccountId = accountId;
        Username = username;
        EmailAddress = emailAddress;
        PreferredLocaleCode = preferredLocaleCode;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>
    /// Gets the session token.
    /// </summary>
    public string SessionToken { get; }

    /// <summary>
    /// Gets the account id.
    /// </summary>
    public long AccountId { get; }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public string EmailAddress { get; }

    /// <summary>
    /// Gets the preferred locale code.
    /// </summary>
    public string PreferredLocaleCode { get; }

    /// <summary>
    /// Gets the expires at utc.
    /// </summary>
    public DateTime ExpiresAtUtc { get; }
}
