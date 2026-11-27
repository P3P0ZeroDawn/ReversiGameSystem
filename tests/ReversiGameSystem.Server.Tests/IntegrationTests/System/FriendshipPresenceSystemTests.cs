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
    public void FriendshipJourney_RequestAcceptPresenceAndRemove_CompletesEndToEnd()
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

        PlayerSummary[] searchResults = channelOne.Channel.FindPlayers(
            friendOne.SessionToken,
            FriendTwoUsername);
        FriendRequest sentRequest = channelOne.Channel.SendFriendRequest(
            friendOne.SessionToken,
            searchResults[0].Account.AccountId);

        bool requestReceived = SpinWait.SpinUntil(
            () => callbackTwo.HasReceivedRequest(sentRequest.RequestId),
            CallbackWaitMilliseconds);
        FriendRequest[] pendingForTwo = channelTwo.Channel.GetPendingFriendRequests(
            friendTwo.SessionToken);

        channelTwo.Channel.RespondToFriendRequest(
            friendTwo.SessionToken,
            pendingForTwo[0].RequestId,
            true);

        bool responseReceived = SpinWait.SpinUntil(
            () => callbackOne.HasAcceptedResponse(sentRequest.RequestId),
            CallbackWaitMilliseconds);
        bool connectedReceived = SpinWait.SpinUntil(
            () => callbackOne.HasChange(friendTwo.Account.AccountId, FriendChange.Connected),
            CallbackWaitMilliseconds);
        FriendSummary[] friendsOnline = channelOne.Channel.GetFriends(friendOne.SessionToken);

        sessionChannel.Channel.CloseSession(friendTwo.SessionToken);
        bool disconnectedReceived = SpinWait.SpinUntil(
            () => callbackOne.HasChange(friendTwo.Account.AccountId, FriendChange.Disconnected),
            CallbackWaitMilliseconds);

        channelOne.Channel.RemoveFriend(friendOne.SessionToken, friendTwo.Account.AccountId);
        FriendSummary[] friendsAfterRemoval = channelOne.Channel.GetFriends(
            friendOne.SessionToken);

        int expectedCount = 1;
        int expectedEmptyCount = 0;

        Assert.AreEqual(expectedCount, searchResults.Length);
        Assert.IsTrue(requestReceived);
        Assert.AreEqual(expectedCount, pendingForTwo.Length);
        Assert.IsTrue(responseReceived);
        Assert.IsTrue(connectedReceived);
        Assert.AreEqual(expectedCount, friendsOnline.Length);
        Assert.IsTrue(friendsOnline[0].IsOnline);
        Assert.IsTrue(disconnectedReceived);
        Assert.AreEqual(expectedEmptyCount, friendsAfterRemoval.Length);
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class RecordingFriendshipCallback : IFriendshipServiceCallback
    {
        private readonly ConcurrentQueue<string> _receivedRequestIds =
            new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<(string RequestId, bool Accepted)> _responses =
            new ConcurrentQueue<(string RequestId, bool Accepted)>();
        private readonly ConcurrentQueue<(long AccountId, FriendChange Change)> _changes =
            new ConcurrentQueue<(long AccountId, FriendChange Change)>();

        internal bool HasReceivedRequest(string requestId)
        {
            return _receivedRequestIds.Contains(requestId);
        }

        internal bool HasAcceptedResponse(string requestId)
        {
            return _responses.Any(item => item.RequestId == requestId && item.Accepted);
        }

        internal bool HasChange(long accountId, FriendChange change)
        {
            return _changes.Any(item => item.AccountId == accountId && item.Change == change);
        }

        public void OnFriendRequestReceived(FriendRequest request)
        {
            _receivedRequestIds.Enqueue(request.RequestId);
        }

        public void OnFriendRequestResponded(string requestId, bool accepted)
        {
            _responses.Enqueue((requestId, accepted));
        }

        public void OnFriendChanged(long accountId, FriendChange change)
        {
            _changes.Enqueue((accountId, change));
        }
    }
}
