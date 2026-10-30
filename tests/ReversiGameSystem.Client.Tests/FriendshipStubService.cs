using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple)]
internal sealed class FriendshipStubService : Proxy.IFriendshipService
{
    internal Proxy.IFriendshipServiceCallback? Callback { get; private set; }
    internal bool RejectSession { get; set; }
    internal string? LastCorrelationId { get; private set; }
    internal ManualResetEventSlim? RequestStarted { get; set; }
    internal ManualResetEventSlim? ContinueRequest { get; set; }
    private int _requestCount;
    internal int RequestCount => Volatile.Read(ref _requestCount);

    public void OpenFriendshipChannel(string sessionToken) =>
        Callback = OperationContext.Current.GetCallbackChannel<Proxy.IFriendshipServiceCallback>();

    public Task OpenFriendshipChannelAsync(string sessionToken)
    {
        OpenFriendshipChannel(sessionToken);
        return Task.CompletedTask;
    }

    public Proxy.PlayerSummary[] FindPlayers(string sessionToken, string searchText) =>
        throw new NotSupportedException();

    public Task<Proxy.PlayerSummary[]> FindPlayersAsync(string sessionToken, string searchText) =>
        throw new NotSupportedException();

    public Proxy.FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId) =>
        throw new NotSupportedException();

    public Task<Proxy.FriendRequest> SendFriendRequestAsync(string sessionToken, long recipientAccountId) =>
        throw new NotSupportedException();

    public Proxy.FriendRequest[] GetPendingFriendRequests(string sessionToken) =>
        throw new NotSupportedException();

    public Task<Proxy.FriendRequest[]> GetPendingFriendRequestsAsync(string sessionToken) =>
        throw new NotSupportedException();

    public void RespondToFriendRequest(string sessionToken, string requestId, bool accept) =>
        throw new NotSupportedException();

    public Task RespondToFriendRequestAsync(string sessionToken, string requestId, bool accept) =>
        throw new NotSupportedException();

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
        return new[]
        {
            new Proxy.FriendSummary
            {
                Account = new Proxy.AccountReference { AccountId = 2, Username = "Bob" },
                IsOnline = false
            }
        };
    }

    public Task<Proxy.FriendSummary[]> GetFriendsAsync(string sessionToken) =>
        Task.FromResult(GetFriends(sessionToken));

    public void RemoveFriend(string sessionToken, long friendAccountId) =>
        throw new NotSupportedException();

    public Task RemoveFriendAsync(string sessionToken, long friendAccountId) =>
        throw new NotSupportedException();
}
