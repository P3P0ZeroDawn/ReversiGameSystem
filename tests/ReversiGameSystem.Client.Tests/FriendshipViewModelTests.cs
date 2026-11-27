using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Features.Friendships;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipViewModelTests
{
    private const long CallerId = 1;
    private const long OtherId = 2;

    [TestMethod]
    public async Task FindPlayers_WhitespaceOnly_DoesNotCallServer()
    {
        FriendshipClientStub client = new FriendshipClientStub();
        FriendshipViewModel viewModel = new FriendshipViewModel(client, CreateSession());

        await viewModel.FindPlayers("  ");

        Assert.AreEqual(0, client.Searches.Count);
    }

    [TestMethod]
    public async Task FindPlayers_NewSearchWhilePending_DisplaysOnlyLatestSearch()
    {
        TaskCompletionSource<FriendshipStatus> completion =
            new TaskCompletionSource<FriendshipStatus>();
        FriendshipClientStub client = new FriendshipClientStub { SearchDelay = completion.Task };
        FriendshipViewModel viewModel = new FriendshipViewModel(client, CreateSession());

        Task<FriendshipStatus> initialSearch = viewModel.FindPlayers("B");
        await viewModel.FindPlayers("Bob");
        completion.SetResult(FriendshipStatus.Success);
        await initialSearch;

        Assert.AreEqual("Bob", viewModel.Players[0].Username);
    }

    [TestMethod]
    public async Task FindPlayers_SessionChangedDuringRequest_DiscardsOldPlayers()
    {
        ClientSession session = CreateSession();
        TaskCompletionSource<FriendshipStatus> completion =
            new TaskCompletionSource<FriendshipStatus>();
        FriendshipClientStub client = new FriendshipClientStub { SearchDelay = completion.Task };
        FriendshipViewModel viewModel = new FriendshipViewModel(client, session);

        Task<FriendshipStatus> search = viewModel.FindPlayers("Bob");
        session.Clear();
        completion.SetResult(FriendshipStatus.Success);
        await search;

        Assert.AreEqual(0, viewModel.Players.Length);
    }

    [TestMethod]
    public async Task FindPlayers_Guest_DoesNotCallServer()
    {
        ClientSession session = new ClientSession();
        session.StartGuest(new GuestAccountSession("guest-session", "Guest", DateTime.MaxValue));
        FriendshipClientStub client = new FriendshipClientStub();
        FriendshipViewModel viewModel = new FriendshipViewModel(client, session);

        await viewModel.FindPlayers("Bob");

        Assert.AreEqual(0, client.Searches.Count);
    }

    [TestMethod]
    public async Task LoadPendingRequests_MixedDirections_ShowsOnlyIncomingRequests()
    {
        FriendshipClientStub client = new FriendshipClientStub
        {
            PendingRequests = new[]
            {
                CreateRequest("incoming", OtherId, CallerId),
                CreateRequest("outgoing", CallerId, OtherId)
            }
        };
        FriendshipViewModel viewModel = new FriendshipViewModel(client, CreateSession());

        await viewModel.LoadPendingRequests();

        CollectionAssert.AreEqual(new[] { "incoming" },
            Array.ConvertAll(viewModel.IncomingRequests, item => item.Request.RequestId));
    }

    [TestMethod]
    public async Task LoadPendingRequests_NotificationDuringRefresh_DoesNotDropRefresh()
    {
        TaskCompletionSource<FriendshipStatus> completion =
            new TaskCompletionSource<FriendshipStatus>();
        FriendshipClientStub client = new FriendshipClientStub { RequestsDelay = completion.Task };
        FriendshipViewModel viewModel = new FriendshipViewModel(client, CreateSession());

        Task<FriendshipStatus> refresh = viewModel.LoadPendingRequests();
        await viewModel.LoadPendingRequests();
        completion.SetResult(FriendshipStatus.Success);
        await refresh;

        Assert.AreEqual(2, client.RequestReads);
    }

    [TestMethod]
    public async Task SendFriendRequest_ConcurrentSubmission_DoesNotSendDuplicate()
    {
        TaskCompletionSource<FriendshipStatus> completion =
            new TaskCompletionSource<FriendshipStatus>();
        FriendshipClientStub client = new FriendshipClientStub { Operation = completion.Task };
        FriendshipViewModel viewModel = new FriendshipViewModel(client, CreateSession());

        Task<FriendshipStatus> first = viewModel.SendFriendRequest(OtherId);
        await viewModel.SendFriendRequest(OtherId);
        completion.SetResult(FriendshipStatus.Success);
        await first;

        Assert.AreEqual(1, client.SendCalls);
    }

    [TestMethod]
    public async Task SendFriendRequest_SelfReference_DoesNotCallServer()
    {
        FriendshipClientStub client = new FriendshipClientStub();
        FriendshipViewModel viewModel = new FriendshipViewModel(client, CreateSession());

        await viewModel.SendFriendRequest(CallerId);

        Assert.AreEqual(0, client.SendCalls);
    }

    [TestMethod]
    public async Task RespondToFriendRequest_OutgoingRequest_DoesNotRespondAsSender()
    {
        FriendshipClientStub client = new FriendshipClientStub();
        FriendshipViewModel viewModel = new FriendshipViewModel(client, CreateSession());

        await viewModel.RespondToFriendRequest(
            CreateRequest("outgoing", CallerId, OtherId), true);

        Assert.AreEqual(0, client.ResponseCalls);
    }

    [TestMethod]
    public async Task SendFriendRequest_TechnicalFailure_ReleasesSubmissionState()
    {
        FriendshipClientStub client = new FriendshipClientStub
        {
            Operation = Task.FromException<FriendshipStatus>(new InvalidOperationException())
        };
        FriendshipViewModel viewModel = new FriendshipViewModel(client, CreateSession());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => viewModel.SendFriendRequest(OtherId));

        Assert.IsFalse(viewModel.IsSubmitting);
    }

    [TestMethod]
    public async Task SessionChanged_PreviouslyLoadedRequests_DiscardsPrivateRequestData()
    {
        ClientSession session = CreateSession();
        FriendshipClientStub client = new FriendshipClientStub
        {
            PendingRequests = new[] { CreateRequest("incoming", OtherId, CallerId) }
        };
        FriendshipViewModel viewModel = new FriendshipViewModel(client, session);
        await viewModel.LoadPendingRequests();

        session.Clear();

        Assert.AreEqual(0, viewModel.IncomingRequests.Length);
    }

    private static ClientSession CreateSession()
    {
        ClientSession session = new ClientSession();
        session.StartAuthenticated(new AuthenticatedAccountSession("friendship-session", CallerId,
            "Alice", "alice@example.test", "en-US", DateTime.MaxValue));
        return session;
    }

    private static PendingFriendRequest CreateRequest(
        string requestId, long senderId, long recipientId)
    {
        return new PendingFriendRequest(requestId, new PlayerAccount(senderId, "Sender", false),
            new PlayerAccount(recipientId, "Recipient", false), DateTime.UtcNow);
    }

    private sealed class FriendshipClientStub : IFriendshipClient
    {
        public bool IsConnected { get; } = true;
        internal List<string> Searches { get; } = new List<string>();
        internal PlayerAccount[] Players { get; private set; } = Array.Empty<PlayerAccount>();
        internal PendingFriendRequest[] PendingRequests { get; set; }
            = Array.Empty<PendingFriendRequest>();
        internal Task<FriendshipStatus>? SearchDelay { get; set; }
        internal Task<FriendshipStatus>? RequestsDelay { get; set; }
        internal Task<FriendshipStatus> Operation { get; set; }
            = Task.FromResult(FriendshipStatus.Success);
        internal int SendCalls { get; private set; }
        internal int ResponseCalls { get; private set; }
        internal int RequestReads { get; private set; }

        public async Task<FriendshipStatus> FindPlayers(string sessionToken, string searchText)
        {
            Searches.Add(searchText);
            Task<FriendshipStatus>? pending = SearchDelay;
            SearchDelay = null;
            if (pending != null)
            {
                await pending;
            }
            Players = new[] { new PlayerAccount(OtherId, searchText, true) };
            return FriendshipStatus.Success;
        }

        public async Task<FriendshipStatus> LoadPendingRequests(string sessionToken)
        {
            RequestReads++;
            Task<FriendshipStatus>? pending = RequestsDelay;
            RequestsDelay = null;
            if (pending != null)
            {
                await pending;
            }
            return FriendshipStatus.Success;
        }

        public Task<FriendshipStatus> LoadFriends(string sessionToken)
        {
            return Task.FromResult(FriendshipStatus.Success);
        }

        public FriendSummary[] ReadFriends()
        {
            return Array.Empty<FriendSummary>();
        }

        public PlayerAccount[] ReadPlayers()
        {
            return Players;
        }

        public PendingFriendRequest[] ReadPendingRequests()
        {
            return PendingRequests;
        }

        public Task<FriendshipStatus> SendFriendRequest(
            string sessionToken, long recipientAccountId)
        {
            SendCalls++;
            return Operation;
        }

        public Task<FriendshipStatus> RespondToFriendRequest(
            string sessionToken, string requestId, bool accept)
        {
            ResponseCalls++;
            return Operation;
        }

        public Task<FriendshipStatus> RemoveFriend(string sessionToken, long friendAccountId)
        {
            return Operation;
        }
    }
}
