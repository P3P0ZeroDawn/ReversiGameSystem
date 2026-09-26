namespace ReversiGameSystem.Services;

/// <summary>
/// Accepts or rejects a pending request addressed to the authenticated player.
/// </summary>
internal sealed class FriendRequestResponseService
{
    private readonly string _recipientUsername;
    private readonly FriendshipStore _store;

    internal FriendRequestResponseService(
        string recipientUsername,
        FriendshipStore store)
    {
        _recipientUsername = recipientUsername;
        _store = store;
    }

    internal FriendRequestResponseResult Respond(string requestId, bool accept)
    {
        return _store.TryRespondToRequest(
            requestId,
            _recipientUsername,
            accept);
    }
}
