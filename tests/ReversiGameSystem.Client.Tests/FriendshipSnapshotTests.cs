using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class FriendshipSnapshotTests
{
    [TestMethod]
    public void Refresh_ConcurrentConnection_PreservesLatestPresence()
    {
        FriendshipSnapshot snapshot = new();
        snapshot.BeginRefresh();
        snapshot.Change(2, FriendChange.Connected);
        snapshot.CompleteRefresh(new[] { Bob(false) });
        Assert.AreEqual(true, snapshot.Read()[0].IsOnline);
    }

    [TestMethod]
    public void Refresh_ConcurrentRemoval_DoesNotRestoreFriend()
    {
        FriendshipSnapshot snapshot = new();
        snapshot.BeginRefresh();
        snapshot.Change(2, FriendChange.Removed);
        snapshot.CompleteRefresh(new[] { Bob(true) });
        Assert.AreEqual(0, snapshot.Read().Length);
    }

    [TestMethod]
    public void Notifications_LastChangeWins_AndReadsAreIsolated()
    {
        FriendshipSnapshot snapshot = new();
        snapshot.BeginRefresh();
        snapshot.Change(2, FriendChange.Connected);
        snapshot.Change(2, FriendChange.Disconnected);
        snapshot.CompleteRefresh(new[] { Bob(true) });
        FriendSummary[] first = snapshot.Read();
        Assert.AreEqual(false, first[0].IsOnline);
        first[0].IsOnline = true;
        Assert.AreEqual(false, snapshot.Read()[0].IsOnline);
        Assert.AreEqual("Bob", snapshot.Read()[0].Username);
        snapshot.Change(2, FriendChange.Connected);
        Assert.AreEqual(true, snapshot.Read()[0].IsOnline);
        snapshot.Change(2, FriendChange.Removed);
        Assert.AreEqual(0, snapshot.Read().Length);
    }

    private static Proxy.FriendSummary Bob(bool online) => new()
    {
        Account = new Proxy.AccountReference { AccountId = 2, Username = "Bob" },
        IsOnline = online
    };
}
