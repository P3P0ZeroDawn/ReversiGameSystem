namespace ReversiGameSystem.Server.Application.Friendships;

public interface IFriendshipRepository
{
    bool IsAccountActive(long accountId);

    FriendAccount[] GetFriends(long accountId);

    bool AreFriends(long accountId, long friendAccountId);

    long[] ReadConfirmedFriendIds(long accountId);
}
