using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionStoreTests;

public abstract class SessionStoreTestBase
{
    private protected UtcClockStub Clock { get; private set; } = null!;

    private protected SessionStore Store { get; private set; } = null!;

    [TestInitialize]
    public void SetUp()
    {
        Clock = new UtcClockStub(new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
        Store = new SessionStore(Clock, TimeSpan.FromMinutes(30));
    }

    private protected AuthenticatedSession CreateAuthenticatedSession(
        string token = "token-1",
        long accountId = 42,
        DateTime? expiresAtUtc = null)
    {
        return new AuthenticatedSession(
            token,
            accountId,
            "PlayerOne",
            "player@example.com",
            "en-US",
            expiresAtUtc ?? Store.CalculateExpirationUtc());
    }

    private protected GuestSession CreateGuestSession(
        string token = "guest-1",
        string displayName = "Guest-Alpha",
        DateTime? expiresAtUtc = null)
    {
        return new GuestSession(
            token,
            displayName,
            expiresAtUtc ?? Store.CalculateExpirationUtc());
    }
}
