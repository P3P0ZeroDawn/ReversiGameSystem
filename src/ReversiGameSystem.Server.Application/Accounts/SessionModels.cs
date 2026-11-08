using System;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>Identifies the expected outcome of a session operation.</summary>
public enum SessionOperationStatus
{
    /// <summary>The session operation completed successfully.</summary>
    Success,
    /// <summary>The credentials do not identify an authorized account.</summary>
    InvalidCredentials,
    /// <summary>The verified account is inactive.</summary>
    InactiveAccount,
    /// <summary>The verification code is invalid or has already been consumed.</summary>
    InvalidVerificationCode,
    /// <summary>The pending verification code has expired.</summary>
    ExpiredVerificationCode,
    /// <summary>The guest name violates the username policy.</summary>
    InvalidGuestName,
    /// <summary>An active guest already owns the requested name.</summary>
    GuestNameUnavailable,
    /// <summary>The required session token is absent.</summary>
    MissingSessionToken,
    /// <summary>The session is absent, expired, revoked, or has the wrong kind.</summary>
    InvalidSession
}

/// <summary>Contains an expected session outcome and its successful value.</summary>
/// <typeparam name="T">The successful value type.</typeparam>
public sealed class SessionOperationResult<T>
{
    private readonly T? _value;

    private SessionOperationResult(SessionOperationStatus status, T? value)
    {
        Status = status;
        _value = value;
    }

    /// <summary>Gets the expected operation status.</summary>
    public SessionOperationStatus Status { get; }

    /// <summary>Creates a successful result containing its required value.</summary>
    /// <param name="value">The successful operation value.</param>
    /// <returns>A successful session operation result.</returns>
    /// <exception cref="ArgumentNullException">The value is absent.</exception>
    public static SessionOperationResult<T> CreateSuccess(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new SessionOperationResult<T>(SessionOperationStatus.Success, value);
    }

    /// <summary>Creates an expected failed result without a value.</summary>
    /// <param name="status">The expected failure status.</param>
    /// <returns>A failed session operation result.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The supplied status represents success.
    /// </exception>
    public static SessionOperationResult<T> CreateFailure(SessionOperationStatus status)
    {
        if (status == SessionOperationStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return new SessionOperationResult<T>(status, default);
    }

    /// <summary>Gets the value contained in a successful result.</summary>
    /// <returns>The successful operation value.</returns>
    /// <exception cref="InvalidOperationException">The operation did not succeed.</exception>
    public T GetValue()
    {
        if (Status != SessionOperationStatus.Success || _value is null)
        {
            throw new InvalidOperationException(
                "A successful session result must contain a value.");
        }

        return _value;
    }
}

/// <summary>Contains the result of one account login lookup.</summary>
public sealed class AccountLoginLookupResult
{
    private readonly LoginAccount? _account;

    private AccountLoginLookupResult(bool isFound, LoginAccount? account)
    {
        IsFound = isFound;
        _account = account;
    }

    /// <summary>Gets whether an account was found.</summary>
    public bool IsFound { get; }

    /// <summary>Creates a successful lookup containing its account.</summary>
    /// <param name="account">The account found by the repository.</param>
    /// <returns>A successful account lookup.</returns>
    /// <exception cref="ArgumentNullException">The account is absent.</exception>
    public static AccountLoginLookupResult CreateFound(LoginAccount account)
    {
        if (account == null)
        {
            throw new ArgumentNullException(nameof(account));
        }

        return new AccountLoginLookupResult(true, account);
    }

    /// <summary>Creates a lookup indicating that no account was found.</summary>
    /// <returns>An account lookup without a result.</returns>
    public static AccountLoginLookupResult CreateNotFound()
    {
        return new AccountLoginLookupResult(false, null);
    }

    /// <summary>Gets the account contained in a successful lookup.</summary>
    /// <returns>The account found by the repository.</returns>
    /// <exception cref="InvalidOperationException">No account was found.</exception>
    public LoginAccount GetFoundAccount()
    {
        if (_account == null)
        {
            throw new InvalidOperationException(
                "A successful account lookup must contain an account.");
        }

        return _account;
    }
}

/// <summary>Contains the account data required by verified login.</summary>
public sealed class LoginAccount
{
    /// <summary>Creates the account data required by verified login.</summary>
    /// <param name="accountId">The persistent account identifier.</param>
    /// <param name="username">The public username.</param>
    /// <param name="emailAddress">The account email address.</param>
    /// <param name="passwordHash">The stored password hash.</param>
    /// <param name="statusCode">The current account status.</param>
    /// <param name="preferredLocaleCode">The preferred interface locale.</param>
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

    /// <summary>Gets the persistent account identifier.</summary>
    public long AccountId { get; }

    /// <summary>Gets the public username.</summary>
    public string Username { get; }

    /// <summary>Gets the account email address.</summary>
    public string EmailAddress { get; }

    /// <summary>Gets the stored password hash.</summary>
    public string PasswordHash { get; }

    /// <summary>Gets the current account status.</summary>
    public string StatusCode { get; }

    /// <summary>Gets the preferred interface locale.</summary>
    public string PreferredLocaleCode { get; }
}

/// <summary>Contains one active authenticated session.</summary>
public sealed class AuthenticatedSession
{
    /// <summary>Creates an authenticated session.</summary>
    /// <param name="sessionToken">The opaque session token.</param>
    /// <param name="accountId">The persistent account identifier.</param>
    /// <param name="username">The public username.</param>
    /// <param name="emailAddress">The account email address.</param>
    /// <param name="preferredLocaleCode">The preferred interface locale.</param>
    /// <param name="expiresAtUtc">The UTC expiration instant.</param>
    public AuthenticatedSession(
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

    /// <summary>Gets the opaque session token.</summary>
    public string SessionToken { get; }

    /// <summary>Gets the persistent account identifier.</summary>
    public long AccountId { get; }

    /// <summary>Gets the public username.</summary>
    public string Username { get; }

    /// <summary>Gets the account email address.</summary>
    public string EmailAddress { get; }

    /// <summary>Gets the preferred interface locale.</summary>
    public string PreferredLocaleCode { get; }

    /// <summary>Gets the UTC expiration instant.</summary>
    public DateTime ExpiresAtUtc { get; }
}

/// <summary>Contains one active guest session.</summary>
public sealed class GuestSession
{
    /// <summary>Creates a guest session.</summary>
    /// <param name="sessionToken">The opaque session token.</param>
    /// <param name="displayName">The reserved display name including its prefix.</param>
    /// <param name="expiresAtUtc">The UTC expiration instant.</param>
    public GuestSession(string sessionToken, string displayName, DateTime expiresAtUtc)
    {
        SessionToken = sessionToken;
        DisplayName = displayName;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Gets the opaque session token.</summary>
    public string SessionToken { get; }

    /// <summary>Gets the reserved display name including its prefix.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the UTC expiration instant.</summary>
    public DateTime ExpiresAtUtc { get; }
}

/// <summary>Identifies access failures used by services awaiting migration.</summary>
public enum SessionRejection
{
    /// <summary>The credentials do not identify an authorized account.</summary>
    InvalidCredentials,
    /// <summary>The verified account is inactive.</summary>
    InactiveAccount,
    /// <summary>The code is incorrect, consumed, or no longer pending.</summary>
    InvalidVerificationCode,
    /// <summary>The pending code has expired.</summary>
    ExpiredVerificationCode,
    /// <summary>The guest name violates the name policy.</summary>
    InvalidGuestName,
    /// <summary>An active guest already holds the name.</summary>
    GuestNameUnavailable,
    /// <summary>The required token was absent.</summary>
    MissingSessionToken,
    /// <summary>The token does not authorize the requested operation.</summary>
    InvalidSession
}

/// <summary>Reports an expected access rejection for services awaiting migration.</summary>
public sealed class SessionRejectedException : Exception
{
    /// <summary>Creates a generic invalid-session rejection.</summary>
    public SessionRejectedException()
        : this(SessionRejection.InvalidSession)
    {
    }

    /// <summary>Creates a rejection with diagnostic context.</summary>
    /// <param name="message">The diagnostic message, excluding secrets.</param>
    public SessionRejectedException(string message)
        : base(message)
    {
        Reason = SessionRejection.InvalidSession;
    }

    /// <summary>Creates a rejection preserving its underlying cause.</summary>
    /// <param name="message">The diagnostic message, excluding secrets.</param>
    /// <param name="innerException">The underlying cause.</param>
    public SessionRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = SessionRejection.InvalidSession;
    }

    /// <summary>Creates a rejection with a programmatically actionable reason.</summary>
    /// <param name="reason">The access rejection reason.</param>
    public SessionRejectedException(SessionRejection reason)
        : base("Session request rejected.")
    {
        Reason = reason;
    }

    /// <summary>Gets the access rejection reason.</summary>
    public SessionRejection Reason { get; }
}
