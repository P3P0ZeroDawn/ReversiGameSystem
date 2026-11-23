using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Application.Accounts;

public sealed class SessionStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, AuthenticatedSession> _authenticated =
        new ConcurrentDictionary<string, AuthenticatedSession>(StringComparer.Ordinal);
    private readonly Dictionary<string, GuestSession> _guests =
        new Dictionary<string, GuestSession>(StringComparer.Ordinal);
    private readonly object _guestSessionSync = new object();
    private readonly IUtcClock _clock;
    private readonly TimeSpan _sessionLifetime;

    public event Action<string>? SessionRevoked;

    public SessionStore(IUtcClock clock)
        : this(clock, DefaultLifetime)
    {
    }

    public SessionStore(IUtcClock clock, TimeSpan sessionLifetime)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (sessionLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sessionLifetime),
                "Session lifetime must be positive.");
        }

        _sessionLifetime = sessionLifetime;
    }

    public DateTime CalculateExpirationUtc()
    {
        DateTime expiresAtUtc = _clock.UtcNow.Add(_sessionLifetime);
        return expiresAtUtc;
    }

    public bool TryAddAuthenticated(AuthenticatedSession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        bool added = _authenticated.TryAdd(session.SessionToken, session);
        return added;
    }

    public bool TryReserveGuest(GuestSession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        bool reserved = false;
        lock (_guestSessionSync)
        {
            RemoveExpiredGuestsCore();
            if (!IsGuestNameInUse(session.DisplayName))
            {
                _guests.Add(session.SessionToken, session);
                reserved = true;
            }
        }

        return reserved;
    }

    public SessionResult<AuthenticatedSession> FindAuthenticated(
        string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return new SessionResult<AuthenticatedSession>(
                SessionStatus.MissingSessionToken);
        }

        if (!_authenticated.TryGetValue(sessionToken, out AuthenticatedSession? active))
        {
            return new SessionResult<AuthenticatedSession>(
                SessionStatus.InvalidSession);
        }

        if (active.ExpiresAtUtc <= _clock.UtcNow)
        {
            if (_authenticated.TryRemove(sessionToken, out _))
            {
                SessionRevoked?.Invoke(sessionToken);
            }

            return new SessionResult<AuthenticatedSession>(
                SessionStatus.InvalidSession);
        }

        return new SessionResult<AuthenticatedSession>(active);
    }

    public bool IsGuestActive(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return false;
        }

        lock (_guestSessionSync)
        {
            RemoveExpiredGuestsCore();
            bool isActive = _guests.TryGetValue(sessionToken, out GuestSession? guest)
                && guest.ExpiresAtUtc > _clock.UtcNow;
            return isActive;
        }
    }

    public void RevokeSession(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        bool removed = _authenticated.TryRemove(sessionToken, out _);
        lock (_guestSessionSync)
        {
            if (_guests.Remove(sessionToken))
            {
                removed = true;
            }
        }

        if (removed)
        {
            SessionRevoked?.Invoke(sessionToken);
        }
    }

    public void RevokeAccountSessions(long accountId)
    {
        if (accountId <= 0)
        {
            return;
        }

        List<string> revokedTokens = new List<string>();
        foreach (KeyValuePair<string, AuthenticatedSession> entry in _authenticated)
        {
            if (entry.Value.AccountId == accountId
                && _authenticated.TryRemove(entry.Key, out _))
            {
                revokedTokens.Add(entry.Key);
            }
        }

        RaiseSessionRevoked(revokedTokens);
    }

    public void RemoveExpiredSessions()
    {
        DateTime nowUtc = _clock.UtcNow;
        List<string> expiredTokens = new List<string>();

        foreach (KeyValuePair<string, AuthenticatedSession> entry in _authenticated)
        {
            if (entry.Value.ExpiresAtUtc <= nowUtc
                && _authenticated.TryRemove(entry.Key, out _))
            {
                expiredTokens.Add(entry.Key);
            }
        }

        lock (_guestSessionSync)
        {
            List<string> expiredGuests = RemoveExpiredGuestsCore();
            expiredTokens.AddRange(expiredGuests);
        }

        RaiseSessionRevoked(expiredTokens);
    }

    private bool IsGuestNameInUse(string displayName)
    {
        foreach (GuestSession active in _guests.Values)
        {
            if (string.Equals(
                active.DisplayName,
                displayName,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private List<string> RemoveExpiredGuestsCore()
    {
        DateTime nowUtc = _clock.UtcNow;
        List<string> expired = new List<string>();
        foreach (KeyValuePair<string, GuestSession> item in _guests)
        {
            if (item.Value.ExpiresAtUtc <= nowUtc)
            {
                expired.Add(item.Key);
            }
        }

        foreach (string token in expired)
        {
            _guests.Remove(token);
        }

        return expired;
    }

    private void RaiseSessionRevoked(List<string> tokens)
    {
        for (int index = 0; index < tokens.Count; index++)
        {
            SessionRevoked?.Invoke(tokens[index]);
        }
    }
}
