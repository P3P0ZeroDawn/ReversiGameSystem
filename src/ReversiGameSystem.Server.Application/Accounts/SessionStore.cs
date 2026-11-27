using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SessionStore> _logger;

    public event Action<string>? SessionRevoked;

    public SessionStore(IUtcClock clock, ILogger<SessionStore> logger)
        : this(clock, DefaultLifetime, logger)
    {
    }

    public SessionStore(
        IUtcClock clock,
        TimeSpan sessionLifetime,
        ILogger<SessionStore> logger)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (sessionLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sessionLifetime),
                "Session lifetime must be positive.");
        }

        _sessionLifetime = sessionLifetime;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        List<GuestSession> expiredGuests;
        lock (_guestSessionSync)
        {
            expiredGuests = RemoveExpiredGuestsCore();
            if (!IsGuestNameInUse(session.DisplayName))
            {
                _guests.Add(session.SessionToken, session);
                reserved = true;
            }
        }

        LogExpiredGuests(expiredGuests);
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
                _logger.LogInformation(
                    "Authenticated session expired for account {AccountId}.",
                    active.AccountId);
                SessionRevoked?.Invoke(sessionToken);
            }

            return new SessionResult<AuthenticatedSession>(
                SessionStatus.InvalidSession);
        }

        return new SessionResult<AuthenticatedSession>(active);
    }

    public SessionResult<GuestSession> FindGuest(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return new SessionResult<GuestSession>(SessionStatus.MissingSessionToken);
        }

        List<GuestSession> expiredGuests;
        GuestSession? guest = null;
        lock (_guestSessionSync)
        {
            expiredGuests = RemoveExpiredGuestsCore();
            _guests.TryGetValue(sessionToken, out guest);
        }

        LogExpiredGuests(expiredGuests);
        if (guest == null)
        {
            return new SessionResult<GuestSession>(SessionStatus.InvalidSession);
        }

        return new SessionResult<GuestSession>(guest);
    }

    public bool IsGuestActive(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return false;
        }

        bool isActive;
        List<GuestSession> expiredGuests;
        lock (_guestSessionSync)
        {
            expiredGuests = RemoveExpiredGuestsCore();
            isActive = _guests.TryGetValue(sessionToken, out GuestSession? guest)
                && guest.ExpiresAtUtc > _clock.UtcNow;
        }

        LogExpiredGuests(expiredGuests);
        return isActive;
    }

    public void RevokeSession(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        bool removed = _authenticated.TryRemove(
            sessionToken,
            out AuthenticatedSession? removedAuthenticated);
        GuestSession? removedGuest = null;
        lock (_guestSessionSync)
        {
            if (_guests.TryGetValue(sessionToken, out GuestSession? guest))
            {
                _guests.Remove(sessionToken);
                removedGuest = guest;
                removed = true;
            }
        }

        if (removedAuthenticated != null)
        {
            _logger.LogInformation(
                "Authenticated session closed for account {AccountId}.",
                removedAuthenticated.AccountId);
        }

        if (removedGuest != null)
        {
            _logger.LogInformation(
                "Guest session closed for {GuestDisplayName}.",
                LogSanitizer.SanitizeText(removedGuest.DisplayName));
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

        if (revokedTokens.Count > 0)
        {
            _logger.LogInformation(
                "Revoked {SessionCount} active session(s) for account {AccountId}.",
                revokedTokens.Count,
                accountId);
        }

        RaiseSessionRevoked(revokedTokens);
    }

    public void RemoveExpiredSessions()
    {
        DateTime nowUtc = _clock.UtcNow;
        List<string> expiredTokens = new List<string>();
        List<long> expiredAccountIds = new List<long>();

        foreach (KeyValuePair<string, AuthenticatedSession> entry in _authenticated)
        {
            if (entry.Value.ExpiresAtUtc <= nowUtc
                && _authenticated.TryRemove(entry.Key, out _))
            {
                expiredTokens.Add(entry.Key);
                expiredAccountIds.Add(entry.Value.AccountId);
            }
        }

        List<GuestSession> expiredGuests;
        lock (_guestSessionSync)
        {
            expiredGuests = RemoveExpiredGuestsCore();
            for (int index = 0; index < expiredGuests.Count; index++)
            {
                expiredTokens.Add(expiredGuests[index].SessionToken);
            }
        }

        for (int index = 0; index < expiredAccountIds.Count; index++)
        {
            _logger.LogInformation(
                "Authenticated session expired for account {AccountId}.",
                expiredAccountIds[index]);
        }

        LogExpiredGuests(expiredGuests);
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

    private List<GuestSession> RemoveExpiredGuestsCore()
    {
        DateTime nowUtc = _clock.UtcNow;
        List<GuestSession> expired = new List<GuestSession>();
        foreach (KeyValuePair<string, GuestSession> item in _guests)
        {
            if (item.Value.ExpiresAtUtc <= nowUtc)
            {
                expired.Add(item.Value);
            }
        }

        for (int index = 0; index < expired.Count; index++)
        {
            _guests.Remove(expired[index].SessionToken);
        }

        return expired;
    }

    private void LogExpiredGuests(List<GuestSession> expiredGuests)
    {
        for (int index = 0; index < expiredGuests.Count; index++)
        {
            _logger.LogInformation(
                "Guest session expired for {GuestDisplayName}.",
                LogSanitizer.SanitizeText(expiredGuests[index].DisplayName));
        }
    }

    private void RaiseSessionRevoked(List<string> tokens)
    {
        for (int index = 0; index < tokens.Count; index++)
        {
            SessionRevoked?.Invoke(tokens[index]);
        }
    }
}
