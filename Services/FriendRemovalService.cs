namespace ReversiGameSystem.Services;

internal enum FriendRemovalResult
{
    Success,
    NotFound,
    ServerConnectionFailed,
    PersistenceError
}

/// <summary>
/// Ends an accepted friendship for the authenticated player.
/// </summary>
internal sealed class FriendRemovalService
{
    private readonly string _username;
    private readonly FriendshipStore _store;

    internal FriendRemovalService(string username, FriendshipStore store)
    {
        _username = username;
        _store = store;
    }

    internal FriendRemovalResult Remove(string friendUsername)
    {
        return _store.TryRemoveFriendship(_username, friendUsername);
    }
}
