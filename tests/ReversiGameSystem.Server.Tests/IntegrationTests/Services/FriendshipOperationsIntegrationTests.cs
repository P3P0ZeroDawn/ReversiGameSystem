using System;
using System.ServiceModel;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.Tests.UnitTests.Services.FriendshipService;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("IntegrationWcf")]
public sealed class FriendshipOperationsIntegrationTests : FriendshipServiceTestBase
{
    private const long SenderId = 10;
    private const long RecipientId = 20;
    private static readonly TimeSpan CallbackTimeout = TimeSpan.FromSeconds(3);

    [TestCleanup]
    public void CloseSubscriptions()
    {
        Subscriptions.Dispose();
    }

    [TestMethod]
    public void FindPlayers_RemoteSearch_MapsPublicPlayerIdentity()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");
        Friendships.SearchPlayers = new[] { new FriendAccount(RecipientId, "Recipient", true) };
        using CallbackRecorder callback = new CallbackRecorder();
        using WcfChannelScope<IFriendshipService> scope = OpenChannel(callback);

        PlayerSummary[] players = scope.Channel.FindPlayers(token, "Recipient");

        Assert.AreEqual((RecipientId, "Recipient", true),
            (players[0].Account.AccountId, players[0].Account.Username,
                players[0].HasProfileImage));
    }

    [TestMethod]
    public void GetPendingFriendRequests_RemoteQuery_PreservesStableRequestIdentity()
    {
        string token = AddAuthenticatedSession(RecipientId, "Recipient");
        PendingFriendRequest request = CreateRequest();
        Friendships.PendingRequests = new[] { request };
        using CallbackRecorder callback = new CallbackRecorder();
        using WcfChannelScope<IFriendshipService> scope = OpenChannel(callback);

        FriendRequest[] requests = scope.Channel.GetPendingFriendRequests(token);

        Assert.AreEqual(request.RequestId, requests[0].RequestId);
    }

    [TestMethod]
    public void SendFriendRequest_CommittedRequest_NotifiesRecipientOverCallback()
    {
        string senderToken = AddAuthenticatedSession(SenderId, "Sender");
        string recipientToken = AddAuthenticatedSession(RecipientId, "Recipient");
        PendingFriendRequest request = CreateRequest();
        Friendships.CreatedRequest = new FriendshipResult<PendingFriendRequest>(request);
        using CallbackRecorder callback = new CallbackRecorder();
        using WcfChannelScope<IFriendshipService> scope = OpenChannel(callback);
        scope.Channel.OpenFriendshipChannel(recipientToken);

        scope.Channel.SendFriendRequest(senderToken, RecipientId);
        bool received = callback.RequestReceived.Wait(CallbackTimeout);

        Assert.AreEqual((true, request.RequestId), (received, callback.RequestId));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void RespondToFriendRequest_CommittedDecision_NotifiesSender(bool accept)
    {
        string senderToken = AddAuthenticatedSession(SenderId, "Sender");
        string recipientToken = AddAuthenticatedSession(RecipientId, "Recipient");
        PendingFriendRequest request = CreateRequest();
        Friendships.PendingRequests = new[] { request };
        using CallbackRecorder callback = new CallbackRecorder();
        using WcfChannelScope<IFriendshipService> scope = OpenChannel(callback);
        scope.Channel.OpenFriendshipChannel(senderToken);

        scope.Channel.RespondToFriendRequest(recipientToken, request.RequestId, accept);
        bool received = callback.ResponseReceived.Wait(CallbackTimeout);

        Assert.AreEqual((true, request.RequestId, accept),
            (received, callback.RequestId, callback.Accepted));
    }

    [TestMethod]
    public void RemoveFriend_CommittedRemoval_NotifiesOtherAccount()
    {
        string senderToken = AddAuthenticatedSession(SenderId, "Sender");
        string recipientToken = AddAuthenticatedSession(RecipientId, "Recipient");
        using CallbackRecorder callback = new CallbackRecorder();
        using WcfChannelScope<IFriendshipService> scope = OpenChannel(callback);
        scope.Channel.OpenFriendshipChannel(recipientToken);

        scope.Channel.RemoveFriend(senderToken, RecipientId);
        bool received = callback.ChangeReceived.Wait(CallbackTimeout);

        Assert.AreEqual((true, SenderId, FriendChange.Removed),
            (received, callback.ChangedAccountId, callback.Change));
    }

    [TestMethod]
    [DataRow(FriendshipStatus.FriendshipAlreadyExists, FriendshipFailure.FriendshipAlreadyExists)]
    [DataRow(FriendshipStatus.RequestAlreadyPending, FriendshipFailure.RequestAlreadyPending)]
    [DataRow(FriendshipStatus.AccountNotFound, FriendshipFailure.AccountUnavailable)]
    public void SendFriendRequest_RejectedRequest_ExposesDeclaredFault(
        FriendshipStatus rejection, FriendshipFailure expectedFailure)
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");
        Friendships.CreatedRequest = new FriendshipResult<PendingFriendRequest>(rejection);
        using CallbackRecorder callback = new CallbackRecorder();
        using WcfChannelScope<IFriendshipService> scope = OpenChannel(callback);

        FaultException<FriendshipFault> failure =
            Assert.ThrowsExactly<FaultException<FriendshipFault>>(
                () => scope.Channel.SendFriendRequest(token, RecipientId));

        Assert.AreEqual(expectedFailure, failure.Detail.Reason);
    }

    [TestMethod]
    public void RespondToFriendRequest_SenderCannotAccept_ExposesUnauthorizedRequest()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");
        PendingFriendRequest request = CreateRequest();
        Friendships.PendingRequests = new[] { request };
        using CallbackRecorder callback = new CallbackRecorder();
        using WcfChannelScope<IFriendshipService> scope = OpenChannel(callback);

        FaultException<FriendshipFault> failure =
            Assert.ThrowsExactly<FaultException<FriendshipFault>>(
                () => scope.Channel.RespondToFriendRequest(token, request.RequestId, true));

        Assert.AreEqual(FriendshipFailure.UnauthorizedRequest, failure.Detail.Reason);
    }

    [TestMethod]
    public void FindPlayers_TechnicalFailure_UsesCentralErrorHandler()
    {
        string token = AddAuthenticatedSession(SenderId, "Sender");
        Friendships.ThrowOnIsAccountActive = true;
        using CallbackRecorder callback = new CallbackRecorder();
        using WcfChannelScope<IFriendshipService> scope = OpenChannel(callback);

        FaultException<ServiceFault> failure = Assert.ThrowsExactly<FaultException<ServiceFault>>(
            () => scope.Channel.FindPlayers(token, "Recipient"));

        Assert.IsFalse(string.IsNullOrWhiteSpace(failure.Detail.CorrelationId));
    }

    private WcfChannelScope<IFriendshipService> OpenChannel(CallbackRecorder callback)
    {
        return WcfTestHostHelper.OpenDuplexChannel<IFriendshipService, IFriendshipServiceCallback>(
            Service, "FriendshipOperations", callback);
    }

    private static PendingFriendRequest CreateRequest()
    {
        return new PendingFriendRequest(new FriendAccount(SenderId, "Sender", false),
            new FriendAccount(RecipientId, "Recipient", true), DateTime.UtcNow);
    }

    [CallbackBehavior(UseSynchronizationContext = false)]
    private sealed class CallbackRecorder : IFriendshipServiceCallback, IDisposable
    {
        internal ManualResetEventSlim RequestReceived { get; } = new ManualResetEventSlim();
        internal ManualResetEventSlim ResponseReceived { get; } = new ManualResetEventSlim();
        internal ManualResetEventSlim ChangeReceived { get; } = new ManualResetEventSlim();
        internal string RequestId { get; private set; } = string.Empty;
        internal bool Accepted { get; private set; }
        internal long ChangedAccountId { get; private set; }
        internal FriendChange Change { get; private set; }

        public void OnFriendRequestReceived(FriendRequest request)
        {
            RequestId = request.RequestId;
            RequestReceived.Set();
        }

        public void OnFriendRequestResponded(string requestId, bool accepted)
        {
            RequestId = requestId;
            Accepted = accepted;
            ResponseReceived.Set();
        }

        public void OnFriendChanged(long accountId, FriendChange change)
        {
            ChangedAccountId = accountId;
            Change = change;
            ChangeReceived.Set();
        }

        public void Dispose()
        {
            RequestReceived.Dispose();
            ResponseReceived.Dispose();
            ChangeReceived.Dispose();
        }
    }
}
