using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>Owns temporary guest names and revokes both kinds of session.</summary>
public sealed class SessionRegistry
{
    private readonly IAuthenticatedSessionStore _authenticated;
    private readonly IUtcClock _clock;
    private readonly object _gate = new object();
    private readonly Dictionary<string, GuestSessionRecord> _guests =
        new Dictionary<string, GuestSessionRecord>(StringComparer.Ordinal);

    /// <summary>Creates the shared authenticated and guest session registry.</summary>
    /// <param name="authenticated">The shared authenticated session store.</param>
    /// <param name="clock">The authoritative UTC clock.</param>
    public SessionRegistry(IAuthenticatedSessionStore authenticated, IUtcClock clock)
    {
        _authenticated = authenticated ?? throw new ArgumentNullException(nameof(authenticated));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Creates a session for an account whose code has been consumed.</summary>
    /// <param name="account">The verified active account.</param>
    /// <returns>The authenticated session.</returns>
    public AuthenticatedSessionRecord CreateAuthenticated(LoginAccount account)
    {
        return _authenticated.Create(account);
    }

    /// <summary>Validates and atomically reserves a guest display name.</summary>
    /// <param name="displayName">The unprefixed guest name.</param>
    /// <returns>The temporary guest session.</returns>
    /// <exception cref="SessionRejectedException">
    /// The name is invalid or already reserved.
    /// </exception>
    public GuestSessionRecord CreateGuest(string displayName)
    {
        string name = displayName?.Trim() ?? string.Empty;
        if (UsernamePolicy.Validate(name) != UsernameValidationError.None)
        {
            throw new SessionRejectedException(SessionRejection.InvalidGuestName);
        }
        lock (_gate)
        {
            RemoveExpiredGuests();
            foreach (GuestSessionRecord active in _guests.Values)
            {
                if (string.Equals(active.DisplayName, "Guest-" + name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new SessionRejectedException(SessionRejection.GuestNameUnavailable);
                }
            }
            GuestSessionRecord session = new GuestSessionRecord(CreateToken(),
                "Guest-" + name, _clock.UtcNow.AddHours(8));
            _guests.Add(session.SessionToken, session);
            return session;
        }
    }

    /// <summary>Tries to retrieve a guest session that has not expired or been revoked.</summary>
    /// <param name="token">The opaque server token.</param>
    /// <param name="session">The active guest session, or null when absent.</param>
    /// <returns>True when an active guest session exists.</returns>
    public bool TryGetGuest(string token, out GuestSessionRecord? session)
    {
        lock (_gate)
        {
            RemoveExpiredGuests();
            session = null;
            return !string.IsNullOrWhiteSpace(token)
                && _guests.TryGetValue(token, out session);
        }
    }

    /// <summary>Revokes either kind of session and releases its temporary name.</summary>
    /// <param name="token">The opaque server token.</param>
    /// <exception cref="SessionRejectedException">The token is missing.</exception>
    public void Close(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SessionRejectedException(SessionRejection.MissingSessionToken);
        }
        _authenticated.Revoke(token);
        lock (_gate)
        {
            _guests.Remove(token);
        }
    }

    private void RemoveExpiredGuests()
    {
        List<string> expired = new List<string>();
        foreach (KeyValuePair<string, GuestSessionRecord> item in _guests)
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
        byte[] bytes = new byte[32];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }
}
