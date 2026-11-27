using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.SystemTests;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("System")]
[TestCategory("Database")]
public sealed class FriendshipPresenceSystemTests
{
    private const string FriendOneUsername = "e2eFriendOne";
    private const string FriendOneEmail = "friend1@example.com";
    private const string FriendTwoUsername = "e2eFriendTwo";
    private const string FriendTwoEmail = "friend2@example.com";
    private const string SharedPassword = "StrongPassword1!";
    private const int CallbackWaitMilliseconds = 5000;

    private static SystemTestHost? _host;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _host = new SystemTestHost();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _host?.Dispose();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _host?.ResetState();
    }

    [TestMethod]
    public void FriendPresence_ConnectAndCloseSession_DeliversConnectedAndDisconnectedCallbacks()
    {
        if (_host == null)
        {
            Assert.Fail("SystemTestHost was not initialized.");
            return;
        }

        AuthenticatedSession friendOne = _host.RegisterAndLogin(
            FriendOneUsername,
            FriendOneEmail,
            SharedPassword);
        AuthenticatedSession friendTwo = _host.RegisterAndLogin(
            FriendTwoUsername,
            FriendTwoEmail,
            SharedPassword);
        SystemTestHost.SeedAcceptedFriendship(
            friendOne.Account.AccountId,
            friendTwo.Account.AccountId);

        RecordingFriendshipCallback callbackOne = new RecordingFriendshipCallback();
        RecordingFriendshipCallback callbackTwo = new RecordingFriendshipCallback();

        using SystemTestHost.ClientChannelScope<IFriendshipService> channelOne =
            _host.OpenDuplexChannel<IFriendshipService, IFriendshipServiceCallback>(callbackOne);
        using SystemTestHost.ClientChannelScope<IFriendshipService> channelTwo =
            _host.OpenDuplexChannel<IFriendshipService, IFriendshipServiceCallback>(callbackTwo);
        using SystemTestHost.ClientChannelScope<ISessionService> sessionChannel =
            _host.OpenChannel<ISessionService>();

        channelOne.Channel.OpenFriendshipChannel(friendOne.SessionToken);
        channelTwo.Channel.OpenFriendshipChannel(friendTwo.SessionToken);

        bool connectedReceived = SpinWait.SpinUntil(
            () => callbackOne.HasChange(friendTwo.Account.AccountId, FriendChange.Connected),
            CallbackWaitMilliseconds);
        FriendSummary[] friendsOnline = channelOne.Channel.GetFriends(friendOne.SessionToken);

        sessionChannel.Channel.CloseSession(friendTwo.SessionToken);
        bool disconnectedReceived = SpinWait.SpinUntil(
            () => callbackOne.HasChange(friendTwo.Account.AccountId, FriendChange.Disconnected),
            CallbackWaitMilliseconds);
        FriendSummary[] friendsOffline = channelOne.Channel.GetFriends(friendOne.SessionToken);
        int expectedFriendCount = 1;

        Assert.IsTrue(connectedReceived);
        Assert.AreEqual(expectedFriendCount, friendsOnline.Length);
        Assert.IsTrue(friendsOnline[0].IsOnline);
        Assert.IsTrue(disconnectedReceived);
        Assert.AreEqual(expectedFriendCount, friendsOffline.Length);
        Assert.IsFalse(friendsOffline[0].IsOnline);
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class RecordingFriendshipCallback : IFriendshipServiceCallback
    {
        private readonly ConcurrentQueue<(long AccountId, FriendChange Change)> _changes =
            new ConcurrentQueue<(long AccountId, FriendChange Change)>();

        internal bool HasChange(long accountId, FriendChange change)
        {
            return _changes.Any(item => item.AccountId == accountId && item.Change == change);
        }

        public void OnFriendRequestReceived(FriendRequest request)
        {
        }

        public void OnFriendRequestResponded(string requestId, bool accepted)
        {
        }

        public void OnFriendChanged(long accountId, FriendChange change)
        {
            _changes.Enqueue((accountId, change));
        }
    }
}
