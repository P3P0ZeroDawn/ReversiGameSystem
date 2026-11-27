using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Tests;

internal static class TestSessionHelper
{
    internal static AuthenticatedSession CreateSession(
        SessionStore store,
        long accountId,
        string username,
        string localeCode = "en-US")
    {
        string token = "test-token-" + Guid.NewGuid().ToString("N");
        AuthenticatedSession session = new AuthenticatedSession(
            token,
            accountId,
            username,
            username + "@example.test",
            localeCode,
            store.CalculateExpirationUtc());
        store.TryAddAuthenticated(session);
        return session;
    }
}

internal sealed class TestUtcClock : IUtcClock
{
    internal TestUtcClock()
        : this(DateTime.UtcNow)
    {
    }

    internal TestUtcClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }
}
