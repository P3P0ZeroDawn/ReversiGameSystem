using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Stores active authenticated sessions in server memory.
/// </summary>
public sealed class InMemoryAuthenticatedSessionStore : IAuthenticatedSessionStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, AuthenticatedSessionRecord> _sessions =
        new ConcurrentDictionary<string, AuthenticatedSessionRecord>(StringComparer.Ordinal);
    private readonly IUtcClock _clock;
    private readonly TimeSpan _sessionLifetime;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InMemoryAuthenticatedSessionStore"/> class.
    /// </summary>
    /// <param name="clock">The UTC clock.</param>
    public InMemoryAuthenticatedSessionStore(IUtcClock clock)
        : this(clock, DefaultLifetime)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InMemoryAuthenticatedSessionStore"/> class.
    /// </summary>
    /// <param name="clock">The UTC clock.</param>
    /// <param name="sessionLifetime">The session lifetime.</param>
    public InMemoryAuthenticatedSessionStore(IUtcClock clock, TimeSpan sessionLifetime)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (sessionLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionLifetime));
        }

        _sessionLifetime = sessionLifetime;
    }

    /// <inheritdoc />
    public AuthenticatedSessionRecord Create(LoginAccount account)
    {
        if (account == null)
        {
            throw new ArgumentNullException(nameof(account));
        }

        AuthenticatedSessionRecord session;
        do
        {
            session = new AuthenticatedSessionRecord(
                CreateToken(),
                account.AccountId,
                account.Username,
                account.EmailAddress,
                account.PreferredLocaleCode,
                _clock.UtcNow.Add(_sessionLifetime));
        }
        while (!_sessions.TryAdd(session.SessionToken, session));

        return session;
    }

    /// <inheritdoc />
    public bool TryGet(string sessionToken, out AuthenticatedSessionRecord? session)
    {
        session = null;
        if (string.IsNullOrWhiteSpace(sessionToken)
            || !_sessions.TryGetValue(sessionToken, out AuthenticatedSessionRecord? active))
        {
            return false;
        }

        if (active.ExpiresAtUtc <= _clock.UtcNow)
        {
            _sessions.TryRemove(sessionToken, out _);
            return false;
        }

        session = active;
        return true;
    }

    /// <inheritdoc />
    public void Revoke(string sessionToken)
    {
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            _sessions.TryRemove(sessionToken, out _);
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
