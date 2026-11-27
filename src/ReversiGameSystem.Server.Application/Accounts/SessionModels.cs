using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Application.Accounts;

public enum SessionStatus
{
    Success,
    InvalidCredentials,
    InactiveAccount,
    InvalidVerificationCode,
    ExpiredVerificationCode,
    InvalidGuestName,
    GuestNameUnavailable,
    MissingSessionToken,
    InvalidSession
}

public sealed class SessionResult<T>
{
    public SessionResult(SessionStatus status)
    {
        if (status == SessionStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A failure result cannot use Success status.");
        }

        Status = status;
    }

    public SessionResult(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Status = SessionStatus.Success;
        Value = value;
    }

    public SessionStatus Status { get; }

    public T? Value { get; }
}

public sealed class LoginAccount
{
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

    public long AccountId { get; }

    public string Username { get; }

    public string EmailAddress { get; }

    public string PasswordHash { get; }

    public string StatusCode { get; }

    public string PreferredLocaleCode { get; }
}

public sealed class AuthenticatedSession
{
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

    public string SessionToken { get; }

    public long AccountId { get; }

    public string Username { get; }

    public string EmailAddress { get; }

    public string PreferredLocaleCode { get; }

    public DateTime ExpiresAtUtc { get; }
}

public sealed class GuestSession
{
    private readonly object _participationLock = new object();
    private readonly Dictionary<long, int> _matchSeats = new Dictionary<long, int>();

    public GuestSession(string sessionToken, string displayName, DateTime expiresAtUtc)
    {
        SessionToken = sessionToken;
        DisplayName = displayName;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string SessionToken { get; }

    public string DisplayName { get; }

    public DateTime ExpiresAtUtc { get; }

    public void RegisterParticipation(long matchId, int seatNumber)
    {
        lock (_participationLock)
        {
            _matchSeats[matchId] = seatNumber;
        }
    }

    public bool OwnsMatchSeat(long matchId, int seatNumber)
    {
        lock (_participationLock)
        {
            return _matchSeats.ContainsKey(matchId) && _matchSeats[matchId] == seatNumber;
        }
    }
}
