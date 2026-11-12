using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal static class FriendshipSnapshotTests
{
    internal static void Refresh_ConcurrentConnection_PreservesLatestPresence()
    {
        FriendshipSnapshot snapshot = new();
        snapshot.BeginRefresh();
        snapshot.Change(2, FriendChange.Connected);
        snapshot.CompleteRefresh(new[] { Bob(false) });
        TestAssert.AreEqual(true, snapshot.Read()[0].IsOnline);
    }

    internal static void Refresh_ConcurrentRemoval_DoesNotRestoreFriend()
    {
        FriendshipSnapshot snapshot = new();
        snapshot.BeginRefresh();
        snapshot.Change(2, FriendChange.Removed);
        snapshot.CompleteRefresh(new[] { Bob(true) });
        TestAssert.AreEqual(0, snapshot.Read().Length);
    }

    internal static void Notifications_LastChangeWins_AndReadsAreIsolated()
    {
        FriendshipSnapshot snapshot = new();
        snapshot.BeginRefresh();
        snapshot.Change(2, FriendChange.Connected);
        snapshot.Change(2, FriendChange.Disconnected);
        snapshot.CompleteRefresh(new[] { Bob(true) });
        FriendSummary[] first = snapshot.Read();
        TestAssert.AreEqual(false, first[0].IsOnline);
        first[0].IsOnline = true;
        TestAssert.AreEqual(false, snapshot.Read()[0].IsOnline);
        TestAssert.AreEqual("Bob", snapshot.Read()[0].Username);
        snapshot.Change(2, FriendChange.Connected);
        TestAssert.AreEqual(true, snapshot.Read()[0].IsOnline);
        snapshot.Change(2, FriendChange.Removed);
        TestAssert.AreEqual(0, snapshot.Read().Length);
    }

    private static Proxy.FriendSummary Bob(bool online) => new()
    {
        Account = new Proxy.AccountReference { AccountId = 2, Username = "Bob" },
        IsOnline = online
    };
}
