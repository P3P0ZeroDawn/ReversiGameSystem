using System.Collections.Generic;

namespace ReversiGameSystem.Services;

internal enum FriendsRetrievalResult
{
    Success,
    ServerConnectionFailed,
    RetrievalError
}

/// <summary>
/// Retrieves active accounts with an accepted friendship relationship.
/// </summary>
internal sealed class FriendsService
{
    private readonly string _username;
    private readonly FriendshipStore _store;

    internal FriendsService(string username, FriendshipStore store)
    {
        _username = username;
        _store = store;
    }

    internal FriendsRetrievalResult TryGetFriends(
        out IReadOnlyList<FriendCandidate> friends)
    {
        friends = _store.GetFriends(_username);
        return FriendsRetrievalResult.Success;
    }
}
