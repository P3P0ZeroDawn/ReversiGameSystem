namespace ReversiGameSystem.Server.Application.Friendships;

public interface IFriendshipRepository
{
    bool IsAccountActive(long accountId);

    FriendAccount[] GetFriends(long accountId);

    bool AreFriends(long accountId, long friendAccountId);

    long[] ReadConfirmedFriendIds(long accountId);

    FriendAccount[] FindPlayers(long accountId, string searchText);

    FriendshipResult<PendingFriendRequest> CreatePendingRequest(long senderId, long recipientId);

    PendingFriendRequest[] ReadPendingRequests(long accountId);

    FriendshipStatus RespondToRequest(long accountId, PendingFriendRequest request, bool accept);

    FriendshipStatus RemoveFriendship(long accountId, long friendAccountId);
}
