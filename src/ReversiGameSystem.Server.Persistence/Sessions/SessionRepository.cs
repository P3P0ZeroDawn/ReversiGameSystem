using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Persistence.Sessions;

/// <summary>Stores active authenticated and guest sessions in the server process.</summary>
public sealed class SessionRepository : ISessionRepository
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, AuthenticatedSession> _authenticated =
        new ConcurrentDictionary<string, AuthenticatedSession>(StringComparer.Ordinal);
    private readonly Dictionary<string, GuestSession> _guests =
        new Dictionary<string, GuestSession>(StringComparer.Ordinal);
    private readonly object _guestSessionSync = new object();
    private readonly IUtcClock _clock;
    private readonly TimeSpan _sessionLifetime;

    /// <summary>Creates the process-owned session repository.</summary>
    /// <param name="clock">The authoritative UTC clock.</param>
    /// <exception cref="ArgumentNullException">The clock is absent.</exception>
    public SessionRepository(IUtcClock clock)
        : this(clock, DefaultLifetime)
    {
    }

    /// <summary>Creates the process-owned session repository.</summary>
    /// <param name="clock">The authoritative UTC clock.</param>
    /// <param name="sessionLifetime">The lifetime applied to new sessions.</param>
    /// <exception cref="ArgumentNullException">The clock is absent.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The lifetime is not positive.</exception>
    public SessionRepository(IUtcClock clock, TimeSpan sessionLifetime)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (sessionLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionLifetime));
        }

        _sessionLifetime = sessionLifetime;
    }

    /// <inheritdoc />
    public AuthenticatedSession Create(LoginAccount account)
    {
        if (account == null)
        {
            throw new ArgumentNullException(nameof(account));
        }

        AuthenticatedSession session;
        do
        {
            session = new AuthenticatedSession(
                CreateToken(),
                account.AccountId,
                account.Username,
                account.EmailAddress,
                account.PreferredLocaleCode,
                _clock.UtcNow.Add(_sessionLifetime));
        }
        while (!_authenticated.TryAdd(session.SessionToken, session));

        return session;
    }

    /// <inheritdoc />
    public SessionOperationResult<GuestSession> CreateGuest(string displayName)
    {
        SessionOperationResult<GuestSession> result;
        lock (_guestSessionSync)
        {
            RemoveExpiredGuests();
            bool isAvailable = true;
            foreach (GuestSession active in _guests.Values)
            {
                if (string.Equals(
                    active.DisplayName,
                    displayName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    isAvailable = false;
                    break;
                }
            }

            if (isAvailable)
            {
                GuestSession session = new GuestSession(
                    CreateToken(),
                    displayName,
                    _clock.UtcNow.Add(_sessionLifetime));
                _guests.Add(session.SessionToken, session);
                result = SessionOperationResult<GuestSession>.CreateSuccess(session);
            }
            else
            {
                result = SessionOperationResult<GuestSession>.CreateFailure(
                    SessionOperationStatus.GuestNameUnavailable);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public SessionOperationResult<AuthenticatedSession> FindAuthenticated(
        string sessionToken)
    {
        SessionOperationResult<AuthenticatedSession> result;
        if (string.IsNullOrWhiteSpace(sessionToken)
            || !_authenticated.TryGetValue(sessionToken, out AuthenticatedSession? active))
        {
            result = SessionOperationResult<AuthenticatedSession>.CreateFailure(
                SessionOperationStatus.InvalidSession);
        }
        else if (active.ExpiresAtUtc <= _clock.UtcNow)
        {
            _authenticated.TryRemove(sessionToken, out _);
            result = SessionOperationResult<AuthenticatedSession>.CreateFailure(
                SessionOperationStatus.InvalidSession);
        }
        else
        {
            result = SessionOperationResult<AuthenticatedSession>.CreateSuccess(active);
        }

        return result;
    }

    /// <inheritdoc />
    public bool TryGet(
        string sessionToken,
        out AuthenticatedSession? session)
    {
        SessionOperationResult<AuthenticatedSession> result = FindAuthenticated(sessionToken);
        bool found = result.Status == SessionOperationStatus.Success;
        session = found ? result.GetValue() : null;
        return found;
    }

    /// <inheritdoc />
    public bool TryGetGuest(string sessionToken, out GuestSession? session)
    {
        lock (_guestSessionSync)
        {
            RemoveExpiredGuests();
            session = null;
            return !string.IsNullOrWhiteSpace(sessionToken)
                && _guests.TryGetValue(sessionToken, out session);
        }
    }

    /// <inheritdoc />
    public void Revoke(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        _authenticated.TryRemove(sessionToken, out _);
        lock (_guestSessionSync)
        {
            _guests.Remove(sessionToken);
        }
    }

    private void RemoveExpiredGuests()
    {
        List<string> expired = new List<string>();
        foreach (KeyValuePair<string, GuestSession> item in _guests)
        {
            if (item.Value.ExpiresAtUtc <= _clock.UtcNow)
            {
                expired.Add(item.Key);
            }
        }

        foreach (string token in expired)
        {
            _guests.Remove(token);
        }
    }

    private static string CreateToken()
    {
        byte[] tokenBytes = new byte[32];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            random.GetBytes(tokenBytes);
        }

        return Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
