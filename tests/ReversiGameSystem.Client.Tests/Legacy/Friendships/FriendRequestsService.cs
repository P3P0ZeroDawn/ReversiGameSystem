using System.Collections.Generic;

namespace ReversiGameSystem.Services;

internal enum FriendRequestsRetrievalResult
{
    Success,
    ServerConnectionFailed,
    RetrievalError
}

internal sealed class FriendRequestsService
{
    private readonly string _recipientUsername;
    private readonly FriendshipStore _store;

    internal FriendRequestsService(string recipientUsername, FriendshipStore store)
    {
        _recipientUsername = recipientUsername;
        _store = store;
    }

    internal FriendRequestsRetrievalResult TryGetPending(
        out IReadOnlyList<FriendshipRequest> requests)
    {
        requests = _store.GetPendingReceived(_recipientUsername);
        return FriendRequestsRetrievalResult.Success;
    }
}
