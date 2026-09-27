using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.LocalRuntime.Sessions;

public sealed class InMemoryAuthenticatedSessionStore : IAuthenticatedSessionStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, AuthenticatedSessionRecord> sessions =
        new ConcurrentDictionary<string, AuthenticatedSessionRecord>(StringComparer.Ordinal);
    private readonly IUtcClock clock;
    private readonly TimeSpan sessionLifetime;

    public InMemoryAuthenticatedSessionStore(IUtcClock clock)
        : this(clock, DefaultLifetime)
    {
    }

    public InMemoryAuthenticatedSessionStore(IUtcClock clock, TimeSpan sessionLifetime)
    {
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (sessionLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionLifetime));
        }

        this.sessionLifetime = sessionLifetime;
    }

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
                clock.UtcNow.Add(sessionLifetime));
        }
        while (!sessions.TryAdd(session.SessionToken, session));

        return Task.FromResult(session);
    }

    public Task<AuthenticatedSessionRecord?> FindAsync(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken)
            || !sessions.TryGetValue(sessionToken, out AuthenticatedSessionRecord? session))
        {
            return Task.FromResult<AuthenticatedSessionRecord?>(null);
        }

        if (session.ExpiresAtUtc <= clock.UtcNow)
        {
            sessions.TryRemove(sessionToken, out _);
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
