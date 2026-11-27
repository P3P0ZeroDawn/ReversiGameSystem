using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[TestCategory("IntegrationWcf")]
public sealed class FriendshipClientOperationsTests
{
    private const string SessionToken = "friendship-operation-session";
    private const long RecipientId = 2;
    private static readonly TimeSpan CallbackTimeout = TimeSpan.FromSeconds(3);

    [TestMethod]
    public async Task FindPlayers_ConfirmedSearch_MapsPublicIdentity()
    {
        using FriendshipHost host = new FriendshipHost();

        await host.Client.FindPlayers(SessionToken, "Bob");
        PlayerAccount player = host.Client.ReadPlayers()[0];

        Assert.AreEqual((RecipientId, "Bob", true),
            (player.AccountId, player.Username, player.HasProfileImage));
    }

    [TestMethod]
    public async Task FindPlayers_ConfirmedSnapshot_DoesNotExposeMutableArray()
    {
        using FriendshipHost host = new FriendshipHost();
        await host.Client.FindPlayers(SessionToken, "Bob");

        PlayerAccount[] players = host.Client.ReadPlayers();
        players[0] = new PlayerAccount(3, "AnotherPlayer", false);

        Assert.AreEqual("Bob", host.Client.ReadPlayers()[0].Username);
    }

    [TestMethod]
    public async Task LoadPendingRequests_ConfirmedQuery_PreservesOpaqueRequestId()
    {
        using FriendshipHost host = new FriendshipHost();
        host.Service.PendingRequests = new[] { CreateRequest() };

        await host.Client.LoadPendingRequests(SessionToken);

        Assert.AreEqual("opaque-request", host.Client.ReadPendingRequests()[0].RequestId);
    }

    [TestMethod]
    public async Task SendFriendRequest_ConfirmedSend_PreservesSessionAndRecipient()
    {
        using FriendshipHost host = new FriendshipHost();

        await host.Client.SendFriendRequest(SessionToken, RecipientId);

        Assert.AreEqual((SessionToken, RecipientId),
            (host.Service.LastSessionToken, host.Service.LastRecipientId));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task RespondToFriendRequest_RemoteDecision_PreservesRequestAndAcceptance(
        bool accept)
    {
        using FriendshipHost host = new FriendshipHost();

        await host.Client.RespondToFriendRequest(SessionToken, "opaque-request", accept);

        Assert.AreEqual(("opaque-request", accept),
            (host.Service.LastRequestId, host.Service.LastAcceptance));
    }

    [TestMethod]
    public async Task RemoveFriend_ConfirmedRemoval_UpdatesConfirmedFriendList()
    {
        using FriendshipHost host = new FriendshipHost();
        await host.Client.LoadFriends(SessionToken);

        await host.Client.RemoveFriend(SessionToken, RecipientId);

        Assert.AreEqual(0, host.Client.ReadFriends().Length);
    }

    [TestMethod]
    [DataRow(Proxy.FriendshipFailure.RequestAlreadyPending,
        (int)FriendshipStatus.RequestAlreadyPending)]
    [DataRow(Proxy.FriendshipFailure.FriendshipAlreadyExists,
        (int)FriendshipStatus.FriendshipAlreadyExists)]
    [DataRow(Proxy.FriendshipFailure.AccountUnavailable, (int)FriendshipStatus.AccountUnavailable)]
    public async Task SendFriendRequest_DeclaredRejection_PreservesReason(
        Proxy.FriendshipFailure rejection, int expectedStatus)
    {
        using FriendshipHost host = new FriendshipHost();
        host.Service.Failure = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = rejection });

        FriendshipStatus status = await host.Client.SendFriendRequest(SessionToken, RecipientId);

        Assert.AreEqual((FriendshipStatus)expectedStatus, status);
    }

    [TestMethod]
    public async Task FindPlayers_UnrelatedFriendshipFault_PropagatesTypedFault()
    {
        using FriendshipHost host = new FriendshipHost();
        host.Service.Failure = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.FriendshipAlreadyExists });

        await Assert.ThrowsExactlyAsync<FaultException<Proxy.FriendshipFault>>(
            () => host.Client.FindPlayers(SessionToken, "Bob"));
    }

    [TestMethod]
    public async Task SendFriendRequest_TechnicalFault_DoesNotRetryOrConvertFailureToSuccess()
    {
        using FriendshipHost host = new FriendshipHost();
        host.Service.Failure = new FaultException<Proxy.ServiceFault>(new Proxy.ServiceFault
        {
            FailureCode = "test-failure",
            CorrelationId = "friendship-correlation"
        });

        await Assert.ThrowsExactlyAsync<FaultException<Proxy.ServiceFault>>(
            () => host.Client.SendFriendRequest(SessionToken, RecipientId));

        Assert.AreEqual(1, host.Service.SendCalls);
    }

    [TestMethod]
    public async Task LoadPendingRequests_InvalidSession_ClearsPrivateConfirmedData()
    {
        using FriendshipHost host = new FriendshipHost();
        host.Service.PendingRequests = new[] { CreateRequest() };
        await host.Client.LoadPendingRequests(SessionToken);
        host.Service.Failure = new FaultException<Proxy.SessionFault>(
            new Proxy.SessionFault { Reason = Proxy.SessionFailure.InvalidSession });

        await host.Client.LoadPendingRequests(SessionToken);

        Assert.AreEqual(0, host.Client.ReadPendingRequests().Length);
    }

    [TestMethod]
    public async Task Close_ConfirmedPendingRequests_DiscardsSessionCache()
    {
        using FriendshipHost host = new FriendshipHost();
        host.Service.PendingRequests = new[] { CreateRequest() };
        await host.Client.LoadPendingRequests(SessionToken);

        host.Client.Close();

        Assert.AreEqual(0, host.Client.ReadPendingRequests().Length);
    }

    [TestMethod]
    public async Task Callback_ReceivedFriendRequest_NotifiesRequestRefresh()
    {
        using FriendshipHost host = new FriendshipHost();
        await host.Client.LoadPendingRequests(SessionToken);
        TaskCompletionSource<bool> notification = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        host.Client.RequestsChanged += () => notification.TrySetResult(true);

        host.Service.Callback!.OnFriendRequestReceived(CreateRequest());

        Task completed = await Task.WhenAny(notification.Task, Task.Delay(CallbackTimeout));

        Assert.AreSame(notification.Task, completed);
    }

    [TestMethod]
    public async Task Callback_AcceptedRequest_NotifiesFriendMembershipRefresh()
    {
        using FriendshipHost host = new FriendshipHost();
        await host.Client.LoadPendingRequests(SessionToken);
        TaskCompletionSource<bool> notification = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        host.Client.MembershipChanged += () => notification.TrySetResult(true);

        host.Service.Callback!.OnFriendRequestResponded("opaque-request", true);

        Task completed = await Task.WhenAny(notification.Task, Task.Delay(CallbackTimeout));

        Assert.AreSame(notification.Task, completed);
    }

    private static Proxy.FriendRequest CreateRequest()
    {
        return new Proxy.FriendRequest
        {
            RequestId = "opaque-request",
            Sender = new Proxy.AccountReference { AccountId = 1, Username = "Alice" },
            Recipient = new Proxy.AccountReference { AccountId = RecipientId, Username = "Bob" },
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private sealed class FriendshipHost : IDisposable
    {
        private const int OperationTimeoutSeconds = 5;
        private readonly ServiceHost _host;

        internal FriendshipStubService Service { get; } = new FriendshipStubService();
        internal FriendshipClient Client { get; }

        internal FriendshipHost()
        {
            string address = "net.pipe://localhost/ReversiFriendshipOperations/"
                + Guid.NewGuid().ToString("N");
            _host = new ServiceHost(Service, new Uri(address));
            ServiceEndpoint endpoint = _host.AddServiceEndpoint(
                typeof(Proxy.IFriendshipService), CreateBinding(), string.Empty);
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                operation.TaskMethod = null;
            }
            _host.Open();
            Client = new FriendshipClient(NullLogger<FriendshipClient>.Instance,
                context => new Proxy.FriendshipServiceClient(
                    context, CreateBinding(), new EndpointAddress(address)));
        }

        private static NetNamedPipeBinding CreateBinding()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(OperationTimeoutSeconds);
            return new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
            {
                OpenTimeout = timeout,
                SendTimeout = timeout,
                CloseTimeout = timeout
            };
        }

        public void Dispose()
        {
            Client.Dispose();
            _host.Abort();
        }
    }
}
