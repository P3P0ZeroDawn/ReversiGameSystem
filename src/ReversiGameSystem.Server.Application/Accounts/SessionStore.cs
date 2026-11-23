using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Stores active authenticated and guest sessions in server process memory.
/// </summary>
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

    /// <summary>
    /// Occurs after an active session is revoked or removed due to expiration.
    /// </summary>
    public event Action<string>? SessionRevoked;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStore"/> class
    /// with the default 8-hour lifetime.
    /// </summary>
    /// <param name="clock">The authoritative UTC clock.</param>
    /// <exception cref="ArgumentNullException"><paramref name="clock"/> is null.</exception>
    public SessionStore(IUtcClock clock)
        : this(clock, DefaultLifetime)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStore"/> class.
    /// </summary>
    /// <param name="clock">The authoritative UTC clock.</param>
    /// <param name="sessionLifetime">The lifetime applied to new sessions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="clock"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="sessionLifetime"/> is not positive.
    /// </exception>
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

    /// <summary>
    /// Calculates the UTC expiration instant for a new session created now.
    /// </summary>
    /// <returns>The UTC expiration instant.</returns>
    public DateTime CalculateExpirationUtc()
    {
        DateTime expiresAtUtc = _clock.UtcNow.Add(_sessionLifetime);
        return expiresAtUtc;
    }

    /// <summary>
    /// Stores a new authenticated session when its token does not already exist.
    /// </summary>
    /// <param name="session">The authenticated session to store.</param>
    /// <returns><c>true</c> if stored; <c>false</c> if the token collided.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is null.</exception>
    public bool TryAddAuthenticated(AuthenticatedSession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        bool added = _authenticated.TryAdd(session.SessionToken, session);
        return added;
    }

    /// <summary>
    /// Atomically reserves a guest display name and stores the guest session.
    /// </summary>
    /// <param name="session">The guest session to store.</param>
    /// <returns><c>true</c> if reserved; <c>false</c> if the display name is in use.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is null.</exception>
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

    /// <summary>
    /// Finds an active authenticated session by token.
    /// </summary>
    /// <param name="sessionToken">The opaque session token.</param>
    /// <returns>
    /// The active session, <see cref="SessionStatus.MissingSessionToken"/>,
    /// or <see cref="SessionStatus.InvalidSession"/>.
    /// </returns>
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

    /// <summary>
    /// Checks whether a guest session token exists and has not expired.
    /// </summary>
    /// <param name="sessionToken">The opaque session token.</param>
    /// <returns><c>true</c> if the guest session is active; otherwise, <c>false</c>.</returns>
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

    /// <summary>
    /// Revokes any active authenticated or guest session matching the token.
    /// </summary>
    /// <param name="sessionToken">The opaque session token.</param>
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

    /// <summary>
    /// Revokes all active authenticated sessions belonging to an account.
    /// </summary>
    /// <param name="accountId">The persistent account identifier.</param>
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

    /// <summary>
    /// Removes all expired authenticated and guest sessions from memory.
    /// </summary>
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
