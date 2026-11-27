using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionStoreTests;

[TestClass]
[TestCategory("Unit")]
public sealed class GuestSessionTests : SessionStoreTestBase
{
    [TestMethod]
    public void TryReserveGuest_NullSession_ThrowsArgumentNullException()
    {
        Action action = () => Store.TryReserveGuest(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void TryReserveGuest_AvailableName_ReturnsTrue()
    {
        GuestSession guest = CreateGuestSession("guest-1", "Guest-Alpha");

        bool actualReserved = Store.TryReserveGuest(guest);

        Assert.IsTrue(actualReserved);
    }

    [TestMethod]
    public void TryReserveGuest_DuplicateNameDifferentCase_ReturnsFalse()
    {
        Store.TryReserveGuest(CreateGuestSession("guest-1", "Guest-Alpha"));
        GuestSession duplicate = CreateGuestSession("guest-2", "guest-alpha");

        bool actualReserved = Store.TryReserveGuest(duplicate);

        Assert.IsFalse(actualReserved);
    }

    [TestMethod]
    public void TryReserveGuest_ExpiredExistingGuest_ReleasesNameAndReturnsTrue()
    {
        GuestSession first = CreateGuestSession("guest-1", "Guest-Alpha");
        Store.TryReserveGuest(first);
        Clock.UtcNow = first.ExpiresAtUtc;
        GuestSession replacement = CreateGuestSession("guest-2", "Guest-Alpha");

        bool actualReserved = Store.TryReserveGuest(replacement);

        Assert.IsTrue(actualReserved);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("unknown")]
    public void IsGuestActive_MissingOrUnknownToken_ReturnsFalse(string? token)
    {
        bool actualActive = Store.IsGuestActive(token!);

        Assert.IsFalse(actualActive);
    }

    [TestMethod]
    public void IsGuestActive_ActiveGuest_ReturnsTrue()
    {
        Store.TryReserveGuest(CreateGuestSession("guest-1", "Guest-Alpha"));

        bool actualActive = Store.IsGuestActive("guest-1");

        Assert.IsTrue(actualActive);
    }

    [TestMethod]
    public void IsGuestActive_ExpiredGuest_ReturnsFalse()
    {
        GuestSession guest = CreateGuestSession("guest-1", "Guest-Alpha");
        Store.TryReserveGuest(guest);
        Clock.UtcNow = guest.ExpiresAtUtc;

        bool actualActive = Store.IsGuestActive("guest-1");

        Assert.IsFalse(actualActive);
    }
}
