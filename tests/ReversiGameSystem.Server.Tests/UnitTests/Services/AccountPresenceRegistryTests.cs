using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class AccountPresenceRegistryTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Register_InvalidSessionToken_ThrowsArgumentException(string? sessionToken)
    {
        AccountPresenceRegistry registry = new AccountPresenceRegistry();

        Action action = () => registry.Register(sessionToken!, 10L);

        Assert.Throws<ArgumentException>(action);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-5L)]
    public void Register_NonPositiveAccountId_ThrowsArgumentOutOfRangeException(long accountId)
    {
        AccountPresenceRegistry registry = new AccountPresenceRegistry();

        Action action = () => registry.Register("token-1", accountId);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void Register_FirstConnection_MarksOnlineAndRaisesPresenceChangedTrue()
    {
        AccountPresenceRegistry registry = new AccountPresenceRegistry();
        List<KeyValuePair<long, bool>> events = new List<KeyValuePair<long, bool>>();
        registry.PresenceChanged += (accountId, isOnline) =>
            events.Add(new KeyValuePair<long, bool>(accountId, isOnline));
        int expectedEventCount = 1;
        KeyValuePair<long, bool> expectedEvent = new KeyValuePair<long, bool>(10L, true);

        using IDisposable registration = registry.Register("token-1", 10L);
        bool actualIsOnline = registry.IsOnline(10L);
        int actualEventCount = events.Count;
        KeyValuePair<long, bool> actualEvent = events[0];

        Assert.IsTrue(actualIsOnline);
        Assert.AreEqual(expectedEventCount, actualEventCount);
        Assert.AreEqual(expectedEvent, actualEvent);
    }

    [TestMethod]
    public void Register_SecondConnectionForSameAccount_KeepsOnlineWithoutDuplicateEvent()
    {
        AccountPresenceRegistry registry = new AccountPresenceRegistry();
        List<KeyValuePair<long, bool>> events = new List<KeyValuePair<long, bool>>();
        registry.PresenceChanged += (accountId, isOnline) =>
            events.Add(new KeyValuePair<long, bool>(accountId, isOnline));
        using IDisposable first = registry.Register("token-1", 10L);
        int expectedEventCount = 1;

        using IDisposable second = registry.Register("token-2", 10L);
        bool actualIsOnline = registry.IsOnline(10L);
        int actualEventCount = events.Count;

        Assert.IsTrue(actualIsOnline);
        Assert.AreEqual(expectedEventCount, actualEventCount);
    }

    [TestMethod]
    public void Dispose_FirstOfTwoConnections_KeepsOnlineWithoutOfflineEvent()
    {
        AccountPresenceRegistry registry = new AccountPresenceRegistry();
        List<KeyValuePair<long, bool>> events = new List<KeyValuePair<long, bool>>();
        registry.PresenceChanged += (accountId, isOnline) =>
            events.Add(new KeyValuePair<long, bool>(accountId, isOnline));
        IDisposable first = registry.Register("token-1", 10L);
        using IDisposable second = registry.Register("token-2", 10L);
        int expectedEventCount = 1;

        first.Dispose();
        bool actualIsOnline = registry.IsOnline(10L);
        int actualEventCount = events.Count;

        Assert.IsTrue(actualIsOnline);
        Assert.AreEqual(expectedEventCount, actualEventCount);
    }

    [TestMethod]
    public void Dispose_LastConnection_MarksOfflineAndRaisesPresenceChangedFalse()
    {
        AccountPresenceRegistry registry = new AccountPresenceRegistry();
        List<KeyValuePair<long, bool>> events = new List<KeyValuePair<long, bool>>();
        registry.PresenceChanged += (accountId, isOnline) =>
            events.Add(new KeyValuePair<long, bool>(accountId, isOnline));
        IDisposable registration = registry.Register("token-1", 10L);
        int expectedEventCount = 2;
        KeyValuePair<long, bool> expectedEvent = new KeyValuePair<long, bool>(10L, false);

        registration.Dispose();
        bool actualIsOnline = registry.IsOnline(10L);
        int actualEventCount = events.Count;
        KeyValuePair<long, bool> actualEvent = events[1];

        Assert.IsFalse(actualIsOnline);
        Assert.AreEqual(expectedEventCount, actualEventCount);
        Assert.AreEqual(expectedEvent, actualEvent);
    }

    [TestMethod]
    public void Dispose_CalledTwiceOnSameRegistration_OnlyUnregistersOnce()
    {
        AccountPresenceRegistry registry = new AccountPresenceRegistry();
        IDisposable first = registry.Register("token-1", 10L);
        using IDisposable second = registry.Register("token-2", 10L);

        first.Dispose();
        first.Dispose();
        bool actualIsOnline = registry.IsOnline(10L);

        Assert.IsTrue(actualIsOnline);
    }

    [TestMethod]
    public void Unregister_ActiveToken_MarksOfflineAndRaisesPresenceChangedFalse()
    {
        AccountPresenceRegistry registry = new AccountPresenceRegistry();
        List<KeyValuePair<long, bool>> events = new List<KeyValuePair<long, bool>>();
        registry.PresenceChanged += (accountId, isOnline) =>
            events.Add(new KeyValuePair<long, bool>(accountId, isOnline));
        registry.Register("token-1", 10L);
        int expectedEventCount = 2;
        KeyValuePair<long, bool> expectedEvent = new KeyValuePair<long, bool>(10L, false);

        registry.Unregister("token-1");
        bool actualIsOnline = registry.IsOnline(10L);
        int actualEventCount = events.Count;
        KeyValuePair<long, bool> actualEvent = events[1];

        Assert.IsFalse(actualIsOnline);
        Assert.AreEqual(expectedEventCount, actualEventCount);
        Assert.AreEqual(expectedEvent, actualEvent);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("unknown-token")]
    public void Unregister_MissingOrUnknownToken_LeavesPresenceUnchanged(string? sessionToken)
    {
        AccountPresenceRegistry registry = new AccountPresenceRegistry();
        using IDisposable registration = registry.Register("token-1", 10L);

        registry.Unregister(sessionToken!);
        bool actualIsOnline = registry.IsOnline(10L);

        Assert.IsTrue(actualIsOnline);
    }
}
