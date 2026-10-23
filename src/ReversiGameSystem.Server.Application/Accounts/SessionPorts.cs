using System;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>Defines server-authoritative access and session revocation.</summary>
public interface ISessionHandler
{
    /// <summary>Validates credentials and sends a single-use login code.</summary>
    /// <param name="loginIdentifier">The account username or email address.</param>
    /// <param name="password">The account password.</param>
    /// <returns>The UTC expiry of the accepted code.</returns>
    /// <exception cref="SessionRejectedException">
    /// Credentials are invalid, or the account is inactive after credential verification.
    /// </exception>
    public DateTime RequestLoginCode(string loginIdentifier, string password);
    /// <summary>Consumes a pending code and creates one authenticated session.</summary>
    /// <param name="loginIdentifier">The account username or email address.</param>
    /// <param name="verificationCode">The code delivered to the account owner.</param>
    /// <returns>The server-issued authenticated session.</returns>
    /// <exception cref="SessionRejectedException">
    /// The code is invalid or expired, or the account is inactive after code verification.
    /// </exception>
    public AuthenticatedSessionRecord CompleteLogin(
        string loginIdentifier, string verificationCode);
    /// <summary>Reserves a temporary guest name.</summary>
    /// <param name="displayName">The unprefixed guest name.</param>
    /// <returns>The server-issued guest session.</returns>
    /// <exception cref="SessionRejectedException">
    /// The guest name is invalid or already reserved by an active guest session.
    /// </exception>
    public GuestSessionRecord OpenGuestSession(string displayName);
    /// <summary>Revokes a session idempotently and releases a guest name.</summary>
    /// <param name="sessionToken">The opaque token to revoke.</param>
    /// <exception cref="SessionRejectedException">
    /// The session token is missing or contains only whitespace; its reason is MissingSessionToken.
    /// </exception>
    public void CloseSession(string sessionToken);
}

/// <summary>Identifies access failures without coupling to a transport contract.</summary>
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

/// <summary>Reports an expected access rejection without transport dependencies.</summary>
public sealed class SessionRejectedException : Exception
{
    /// <summary>Creates a generic invalid-session rejection.</summary>
    public SessionRejectedException() : this(SessionRejection.InvalidSession) { }

    /// <summary>Creates a rejection with diagnostic context.</summary>
    /// <param name="message">The diagnostic message, excluding secrets.</param>
    public SessionRejectedException(string message) : base(message)
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
    public SessionRejectedException(SessionRejection reason) : base("Session request rejected.")
    {
        Reason = reason;
    }

    /// <summary>Gets the access rejection reason.</summary>
    public SessionRejection Reason { get; }
}

/// <summary>Represents a temporary identity issued by the server.</summary>
public sealed class GuestSessionRecord
{
    /// <summary>Creates the public state of a temporary server session.</summary>
    /// <param name="sessionToken">The opaque server token.</param>
    /// <param name="displayName">The reserved guest name including its prefix.</param>
    /// <param name="expiresAtUtc">The UTC expiry.</param>
    public GuestSessionRecord(string sessionToken, string displayName, DateTime expiresAtUtc)
    {
        SessionToken = sessionToken;
        DisplayName = displayName;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Gets the opaque server token.</summary>
    public string SessionToken { get; }
    /// <summary>Gets the reserved guest name including its prefix.</summary>
    public string DisplayName { get; }
    /// <summary>Gets the UTC expiry.</summary>
    public DateTime ExpiresAtUtc { get; }
}
