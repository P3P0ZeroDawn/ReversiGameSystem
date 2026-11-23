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

/// <summary>Contains an expected session outcome and its optional value.</summary>
/// <typeparam name="T">The successful value type.</typeparam>
public sealed class SessionResult<T>
{
    /// <summary>Creates a failed session result.</summary>
    /// <param name="status">The expected failure status.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The supplied status represents success.
    /// </exception>
    public SessionResult(SessionOperationStatus status)
    {
        if (status == SessionOperationStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A failure result cannot use Success status.");
        }

        Status = status;
    }

    /// <summary>Creates a successful session result.</summary>
    /// <param name="value">The successful operation value.</param>
    /// <exception cref="ArgumentNullException">The value is absent.</exception>
    public SessionResult(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Status = SessionOperationStatus.Success;
        Value = value;
    }

    /// <summary>Gets the expected operation status.</summary>
    public SessionOperationStatus Status { get; }

    /// <summary>Gets the successful operation value, or default on failure.</summary>
    public T? Value { get; }
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
