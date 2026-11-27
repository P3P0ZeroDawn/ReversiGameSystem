using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class FriendshipServiceIntegrationTests
{
    private const long SenderId = 10L;
    private const long RecipientId = 20L;
    private const string SenderUsername = "Sender";
    private const string RecipientUsername = "Recipient";
    private const int CallbackWaitMilliseconds = 5000;

    private SessionStore _sessions = null!;
    private FriendshipRepositoryStub _friendships = null!;
    private AccountPresenceRegistry _presence = null!;
    private FriendshipSubscriptions _subscriptions = null!;
    private FriendshipService _service = null!;
    private string _senderToken = string.Empty;
    private string _recipientToken = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        UtcClockStub clock = new UtcClockStub();
        _sessions = new SessionStore(clock, NullLogger<SessionStore>.Instance);
        _friendships = new FriendshipRepositoryStub();
        _presence = new AccountPresenceRegistry();
        _subscriptions = new FriendshipSubscriptions(
            NullLogger<FriendshipSubscriptions>.Instance);
        FriendshipApplicationService applicationService =
            new FriendshipApplicationService(_sessions, _friendships);
        _service = new FriendshipService(applicationService, _presence, _subscriptions);
        _senderToken = TestSessionHelper.CreateSession(
            _sessions, SenderId, SenderUsername).SessionToken;
        _recipientToken = TestSessionHelper.CreateSession(
            _sessions, RecipientId, RecipientUsername).SessionToken;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _subscriptions?.Dispose();
    }

    [TestMethod]
    public void FindPlayers_ThroughWcf_ReturnsMappedPlayers()
    {
        _friendships.SearchPlayers = new[]
        {
            new FriendAccount(RecipientId, RecipientUsername, true)
        };
        using WcfChannelScope<IFriendshipService> scope =
            WcfTestHostHelper.OpenDuplexChannel<IFriendshipService, IFriendshipServiceCallback>(
                _service,
                "FriendshipFind",
                new RecordingFriendshipCallback());
        int expectedCount = 1;

        PlayerSummary[] actualPlayers = scope.Channel.FindPlayers(_senderToken, "Recip");

        Assert.AreEqual(expectedCount, actualPlayers.Length);
        Assert.AreEqual(RecipientId, actualPlayers[0].Account.AccountId);
        Assert.AreEqual(RecipientUsername, actualPlayers[0].Account.Username);
    }

    [TestMethod]
    public void SendFriendRequest_SubscribedRecipientThroughWcf_DeliversRequestCallback()
    {
        PendingFriendRequest expectedRequest = new PendingFriendRequest(
            new FriendAccount(SenderId, SenderUsername, false),
            new FriendAccount(RecipientId, RecipientUsername, false),
            DateTime.UtcNow);
        _friendships.CreatedRequest = new FriendshipResult<PendingFriendRequest>(expectedRequest);
        RecordingFriendshipCallback recipientCallback = new RecordingFriendshipCallback();
        using WcfChannelScope<IFriendshipService> senderScope =
            WcfTestHostHelper.OpenDuplexChannel<IFriendshipService, IFriendshipServiceCallback>(
                _service,
                "FriendshipSend",
                new RecordingFriendshipCallback());
        using DuplexChannelFactory<IFriendshipService> recipientFactory =
            WcfTestHostHelper.CreateDuplexFactory<IFriendshipService, IFriendshipServiceCallback>(
                senderScope.EndpointAddress,
                recipientCallback);
        IFriendshipService recipientChannel = recipientFactory.CreateChannel();
        try
        {
            recipientChannel.OpenFriendshipChannel(_recipientToken);

            FriendRequest actualSent = senderScope.Channel.SendFriendRequest(
                _senderToken,
                RecipientId);

            bool delivered = SpinWait.SpinUntil(
                () => recipientCallback.ReceivedRequests.Any(
                    request => request.RequestId == expectedRequest.RequestId),
                CallbackWaitMilliseconds);
            Assert.AreEqual(expectedRequest.RequestId, actualSent.RequestId);
            Assert.IsTrue(delivered);
        }
        finally
        {
            WcfTestHostHelper.CloseOrAbort(recipientChannel);
            WcfTestHostHelper.CloseOrAbort(recipientFactory);
        }
    }

    [TestMethod]
    public void RespondToFriendRequest_AcceptThroughWcf_DeliversResponseAndPresenceCallbacks()
    {
        PendingFriendRequest pending = new PendingFriendRequest(
            new FriendAccount(SenderId, SenderUsername, false),
            new FriendAccount(RecipientId, RecipientUsername, false),
            DateTime.UtcNow);
        _friendships.PendingRequests = new[] { pending };
        RecordingFriendshipCallback senderCallback = new RecordingFriendshipCallback();
        using WcfChannelScope<IFriendshipService> recipientScope =
            WcfTestHostHelper.OpenDuplexChannel<IFriendshipService, IFriendshipServiceCallback>(
                _service,
                "FriendshipRespond",
                new RecordingFriendshipCallback());
        using DuplexChannelFactory<IFriendshipService> senderFactory =
            WcfTestHostHelper.CreateDuplexFactory<IFriendshipService, IFriendshipServiceCallback>(
                recipientScope.EndpointAddress,
                senderCallback);
        IFriendshipService senderChannel = senderFactory.CreateChannel();
        try
        {
            senderChannel.OpenFriendshipChannel(_senderToken);
            recipientScope.Channel.OpenFriendshipChannel(_recipientToken);

            recipientScope.Channel.RespondToFriendRequest(
                _recipientToken,
                pending.RequestId,
                true);

            bool responseDelivered = SpinWait.SpinUntil(
                () => senderCallback.RespondedRequests.Any(
                    item => item.RequestId == pending.RequestId && item.Accepted),
                CallbackWaitMilliseconds);
            bool presenceDelivered = SpinWait.SpinUntil(
                () => senderCallback.HasChange(RecipientId, FriendChange.Connected),
                CallbackWaitMilliseconds);
            Assert.IsTrue(responseDelivered);
            Assert.IsTrue(presenceDelivered);
        }
        finally
        {
            WcfTestHostHelper.CloseOrAbort(senderChannel);
            WcfTestHostHelper.CloseOrAbort(senderFactory);
        }
    }

    [TestMethod]
    public void RemoveFriend_SubscribedFriendThroughWcf_DeliversRemovedCallback()
    {
        RecordingFriendshipCallback friendCallback = new RecordingFriendshipCallback();
        using WcfChannelScope<IFriendshipService> callerScope =
            WcfTestHostHelper.OpenDuplexChannel<IFriendshipService, IFriendshipServiceCallback>(
                _service,
                "FriendshipRemove",
                new RecordingFriendshipCallback());
        using DuplexChannelFactory<IFriendshipService> friendFactory =
            WcfTestHostHelper.CreateDuplexFactory<IFriendshipService, IFriendshipServiceCallback>(
                callerScope.EndpointAddress,
                friendCallback);
        IFriendshipService friendChannel = friendFactory.CreateChannel();
        try
        {
            friendChannel.OpenFriendshipChannel(_recipientToken);

            callerScope.Channel.RemoveFriend(_senderToken, RecipientId);

            bool removedDelivered = SpinWait.SpinUntil(
                () => friendCallback.HasChange(SenderId, FriendChange.Removed),
                CallbackWaitMilliseconds);
            Assert.IsTrue(removedDelivered);
        }
        finally
        {
            WcfTestHostHelper.CloseOrAbort(friendChannel);
            WcfTestHostHelper.CloseOrAbort(friendFactory);
        }
    }

    [TestMethod]
    public void SendFriendRequest_SelfTargetThroughWcf_ThrowsFriendshipFaultAndKeepsChannelOpen()
    {
        using WcfChannelScope<IFriendshipService> scope =
            WcfTestHostHelper.OpenDuplexChannel<IFriendshipService, IFriendshipServiceCallback>(
                _service,
                "FriendshipFault",
                new RecordingFriendshipCallback());
        FriendshipFailure expectedReason = FriendshipFailure.CannotSendRequestToSelf;
        CommunicationState expectedState = CommunicationState.Opened;
        Action action = () => scope.Channel.SendFriendRequest(_senderToken, SenderId);

        FaultException<FriendshipFault> fault =
            Assert.Throws<FaultException<FriendshipFault>>(action);

        Assert.AreEqual(expectedReason, fault.Detail.Reason);
        Assert.AreEqual(expectedState, scope.ChannelState);
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class RecordingFriendshipCallback : IFriendshipServiceCallback
    {
        internal ConcurrentQueue<FriendRequest> ReceivedRequests { get; } =
            new ConcurrentQueue<FriendRequest>();

        internal ConcurrentQueue<(string RequestId, bool Accepted)> RespondedRequests { get; } =
            new ConcurrentQueue<(string RequestId, bool Accepted)>();

        internal ConcurrentQueue<(long AccountId, FriendChange Change)> Changes { get; } =
            new ConcurrentQueue<(long AccountId, FriendChange Change)>();

        internal bool HasChange(long accountId, FriendChange change)
        {
            return Changes.Any(item => item.AccountId == accountId && item.Change == change);
        }

        public void OnFriendRequestReceived(FriendRequest request)
        {
            ReceivedRequests.Enqueue(request);
        }

        public void OnFriendRequestResponded(string requestId, bool accepted)
        {
            RespondedRequests.Enqueue((requestId, accepted));
        }

        public void OnFriendChanged(long accountId, FriendChange change)
        {
            Changes.Enqueue((accountId, change));
        }
    }
}
