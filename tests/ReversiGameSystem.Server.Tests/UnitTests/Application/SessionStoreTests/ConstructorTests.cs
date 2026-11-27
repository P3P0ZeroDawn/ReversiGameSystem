using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionStoreTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ConstructorTests : SessionStoreTestBase
{
    [TestMethod]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionStore(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_ZeroLifetime_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = new SessionStore(Clock, TimeSpan.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void Constructor_NegativeLifetime_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = new SessionStore(Clock, TimeSpan.FromMinutes(-5));

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void CalculateExpirationUtc_DefaultLifetime_AddsEightHours()
    {
        SessionStore defaultStore = new SessionStore(Clock);
        DateTime expectedExpirationUtc = Clock.UtcNow.AddHours(8);

        DateTime actualExpirationUtc = defaultStore.CalculateExpirationUtc();

        Assert.AreEqual(expectedExpirationUtc, actualExpirationUtc);
    }
}
