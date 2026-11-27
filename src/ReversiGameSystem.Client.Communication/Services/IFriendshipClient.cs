using System.Threading.Tasks;

namespace ReversiGameSystem.Services;

internal interface IFriendshipClient
{
    bool IsConnected { get; }

    FriendSummary[] ReadFriends();

    PlayerAccount[] ReadPlayers();

    PendingFriendRequest[] ReadPendingRequests();

    Task<FriendshipStatus> LoadFriends(string sessionToken);

    Task<FriendshipStatus> FindPlayers(string sessionToken, string searchText);

    Task<FriendshipStatus> LoadPendingRequests(string sessionToken);

    Task<FriendshipStatus> SendFriendRequest(string sessionToken, long recipientAccountId);

    Task<FriendshipStatus> RespondToFriendRequest(string sessionToken, string requestId, bool accept);

    Task<FriendshipStatus> RemoveFriend(string sessionToken, long friendAccountId);
}
