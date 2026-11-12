using System;

namespace ReversiGameSystem.Services;

internal enum SessionStatus
{
    Success,
    MissingCredentials,
    InvalidCredentials,
    InactiveAccount,
    InvalidVerificationCode,
    ExpiredVerificationCode,
    InvalidGuestName,
    GuestNameUnavailable,
    MissingSessionToken,
    InvalidSession,
    ServiceUnavailable
}

internal sealed class SessionResult<T>
{
    internal SessionResult(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Status = SessionStatus.Success;
    }

    internal SessionResult(SessionStatus status)
    {
        if (status == SessionStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
        Value = default;
    }

    internal SessionStatus Status { get; }

    internal T? Value { get; }
}

internal sealed class AuthenticatedAccountSession
{
    internal AuthenticatedAccountSession(
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

    internal string SessionToken { get; }

    internal long AccountId { get; }

    internal string Username { get; }

    internal string EmailAddress { get; }

    internal string PreferredLocaleCode { get; }

    internal DateTime ExpiresAtUtc { get; }
}

internal sealed class GuestAccountSession
{
    internal GuestAccountSession(string sessionToken, string displayName, DateTime expiresAtUtc)
    {
        SessionToken = sessionToken;
        DisplayName = displayName;
        ExpiresAtUtc = expiresAtUtc;
    }

    internal string SessionToken { get; }

    internal string DisplayName { get; }

    internal DateTime ExpiresAtUtc { get; }
}
