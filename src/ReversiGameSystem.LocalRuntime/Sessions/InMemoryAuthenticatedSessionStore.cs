using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.LocalRuntime.Sessions;

/// <summary>
/// Represents in memory authenticated session store.
/// </summary>
public sealed class InMemoryAuthenticatedSessionStore : IAuthenticatedSessionStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, AuthenticatedSessionRecord> _sessions =
        new ConcurrentDictionary<string, AuthenticatedSessionRecord>(StringComparer.Ordinal);
    private readonly IUtcClock _clock;
    private readonly TimeSpan _sessionLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAuthenticatedSessionStore"/> class.
    /// </summary>
    /// <param name="clock">The clock.</param>
    public InMemoryAuthenticatedSessionStore(IUtcClock clock)
        : this(clock, DefaultLifetime)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAuthenticatedSessionStore"/> class.
    /// </summary>
    /// <param name="clock">The clock.</param>
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

    /// <summary>
    /// Performs the create async operation.
    /// </summary>
    /// <param name="accountId">The account id.</param>
    /// <param name="username">The username.</param>
    /// <param name="emailAddress">The email address.</param>
    /// <returns>The result of the operation.</returns>
    public Task<AuthenticatedSessionRecord> CreateAsync(
        long accountId,
        string username,
        string emailAddress)
    {
        AuthenticatedSessionRecord session;
        do
        {
            session = new AuthenticatedSessionRecord(
                CreateToken(),
                accountId,
                username,
                emailAddress,
                _clock.UtcNow.Add(_sessionLifetime));
        }
        while (!_sessions.TryAdd(session.SessionToken, session));

        return Task.FromResult(session);
    }

    /// <summary>
    /// Performs the find async operation.
    /// </summary>
    /// <param name="sessionToken">The session token.</param>
    /// <returns>The result of the operation.</returns>
    public Task<AuthenticatedSessionRecord?> FindAsync(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken)
            || !_sessions.TryGetValue(sessionToken, out AuthenticatedSessionRecord? session))
        {
            return Task.FromResult<AuthenticatedSessionRecord?>(null);
        }

        if (session.ExpiresAtUtc <= _clock.UtcNow)
        {
            _sessions.TryRemove(sessionToken, out _);
            return Task.FromResult<AuthenticatedSessionRecord?>(null);
        }

        return Task.FromResult<AuthenticatedSessionRecord?>(session);
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
