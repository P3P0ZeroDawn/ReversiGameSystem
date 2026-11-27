using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
internal sealed class FriendshipStubService : Proxy.IFriendshipService
{
    internal Proxy.IFriendshipServiceCallback? Callback { get; private set; }
    internal bool RejectSession { get; set; }
    internal string? LastCorrelationId { get; private set; }
    internal ManualResetEventSlim? RequestStarted { get; set; }
    internal ManualResetEventSlim? ContinueRequest { get; set; }
    private int _requestCount;
    internal int RequestCount
    {
        get { return Volatile.Read(ref _requestCount); }
    }

    internal FaultException? Failure { get; set; }
    internal string LastSearchText { get; private set; } = string.Empty;
    internal string LastSessionToken { get; private set; } = string.Empty;
    internal long LastRecipientId { get; private set; }
    internal long LastRemovedId { get; private set; }
    internal string LastRequestId { get; private set; } = string.Empty;
    internal bool LastAcceptance { get; private set; }
    internal int SendCalls { get; private set; }
    internal bool FriendshipRemoved { get; private set; }
    internal Proxy.PlayerSummary[] Players { get; set; } = new[]
    {
        new Proxy.PlayerSummary
        {
            Account = new Proxy.AccountReference { AccountId = 2, Username = "Bob" },
            HasProfileImage = true
        }
    };
    internal Proxy.FriendRequest[] PendingRequests { get; set; }
        = Array.Empty<Proxy.FriendRequest>();

    public void OpenFriendshipChannel(string sessionToken)
    {
        Callback = OperationContext.Current.GetCallbackChannel<Proxy.IFriendshipServiceCallback>();
    }

    public Task OpenFriendshipChannelAsync(string sessionToken)
    {
        OpenFriendshipChannel(sessionToken);
        return Task.CompletedTask;
    }

    public Proxy.PlayerSummary[] FindPlayers(string sessionToken, string searchText)
    {
        LastSessionToken = sessionToken;
        LastSearchText = searchText;
        if (Failure != null)
        {
            throw Failure;
        }
        return Players;
    }

    public Task<Proxy.PlayerSummary[]> FindPlayersAsync(string sessionToken, string searchText)
    {
        return Task.FromResult(FindPlayers(sessionToken, searchText));
    }

    public Proxy.FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId)
    {
        SendCalls++;
        LastSessionToken = sessionToken;
        LastRecipientId = recipientAccountId;
        if (Failure != null)
        {
            throw Failure;
        }
        Proxy.FriendRequest request = new Proxy.FriendRequest
        {
            RequestId = "confirmed-request",
            Sender = new Proxy.AccountReference { AccountId = 1, Username = "Alice" },
            Recipient = new Proxy.AccountReference
            {
                AccountId = recipientAccountId, Username = "Bob"
            },
            CreatedAtUtc = DateTime.UtcNow
        };
        PendingRequests = new[] { request };
        return request;
    }

    public Task<Proxy.FriendRequest> SendFriendRequestAsync(
        string sessionToken, long recipientAccountId)
    {
        return Task.FromResult(SendFriendRequest(sessionToken, recipientAccountId));
    }

    public Proxy.FriendRequest[] GetPendingFriendRequests(string sessionToken)
    {
        LastSessionToken = sessionToken;
        if (Failure != null)
        {
            throw Failure;
        }
        return PendingRequests;
    }

    public Task<Proxy.FriendRequest[]> GetPendingFriendRequestsAsync(string sessionToken)
    {
        return Task.FromResult(GetPendingFriendRequests(sessionToken));
    }

    public void RespondToFriendRequest(string sessionToken, string requestId, bool accept)
    {
        LastSessionToken = sessionToken;
        LastRequestId = requestId;
        LastAcceptance = accept;
        if (Failure != null)
        {
            throw Failure;
        }
        PendingRequests = PendingRequests
            .Where(request => request.RequestId != requestId).ToArray();
    }

    public Task RespondToFriendRequestAsync(string sessionToken, string requestId, bool accept)
    {
        RespondToFriendRequest(sessionToken, requestId, accept);
        return Task.CompletedTask;
    }

    public Proxy.FriendSummary[] GetFriends(string sessionToken)
    {
        LastCorrelationId = OperationContext.Current.IncomingMessageHeaders.GetHeader<string>(
            "CorrelationId", "urn:reversi:diagnostics:v1");
        Interlocked.Increment(ref _requestCount);
        RequestStarted?.Set();
        if (ContinueRequest != null && !ContinueRequest.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("The test request was not released.");
        }
        if (RejectSession)
        {
            throw new FaultException<Proxy.SessionFault>(new Proxy.SessionFault
            {
                Reason = Proxy.SessionFailure.InvalidSession
            }, new FaultReason("Session rejected"));
        }
        if (FriendshipRemoved)
        {
            return Array.Empty<Proxy.FriendSummary>();
        }
        return new[]
        {
            new Proxy.FriendSummary
            {
                Account = new Proxy.AccountReference { AccountId = 2, Username = "Bob" },
                IsOnline = false
            }
        };
    }

    public Task<Proxy.FriendSummary[]> GetFriendsAsync(string sessionToken)
    {
        return Task.FromResult(GetFriends(sessionToken));
    }

    public void RemoveFriend(string sessionToken, long friendAccountId)
    {
        LastSessionToken = sessionToken;
        LastRemovedId = friendAccountId;
        if (Failure != null)
        {
            throw Failure;
        }
        FriendshipRemoved = true;
    }

    public Task RemoveFriendAsync(string sessionToken, long friendAccountId)
    {
        RemoveFriend(sessionToken, friendAccountId);
        return Task.CompletedTask;
    }
}
