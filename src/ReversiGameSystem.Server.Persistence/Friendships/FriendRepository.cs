using System.Data.Entity;
using System.Linq;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Friendships;

/// <summary>Reads public friendships without tracking private account entities.</summary>
public sealed class FriendRepository : IFriendshipRepository
{
    private const string ActiveStatus = "ACTIVE";
    private const string AcceptedStatus = "ACCEPTED";

    /// <inheritdoc />
    public bool IsAccountActive(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        return context.Account.AsNoTracking().Any(account =>
            account.accountId == accountId && account.statusCode == ActiveStatus);
    }

    /// <inheritdoc />
    public FriendAccount[] GetFriends(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        return (from relationship in context.Friendship.AsNoTracking()
                join caller in context.Account on accountId equals caller.accountId
                join friend in context.Account on
                    (relationship.accountIdA == accountId
                        ? relationship.accountIdB : relationship.accountIdA) equals friend.accountId
                where (relationship.accountIdA == accountId
                        || relationship.accountIdB == accountId)
                    && relationship.statusCode == AcceptedStatus
                    && relationship.endedAtUtc == null
                    && friend.accountId != accountId
                    && caller.statusCode == ActiveStatus && friend.statusCode == ActiveStatus
                orderby friend.username, friend.accountId
                select new
                {
                    friend.accountId,
                    friend.username,
                    HasProfileImage = friend.profileImageUri != null && friend.profileImageUri != ""
                })
            .ToArray()
            .Select(friend => new FriendAccount(
                friend.accountId, friend.username, friend.HasProfileImage))
            .ToArray();
    }

    /// <inheritdoc />
    public bool AreFriends(long accountId, long friendAccountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        return (from relationship in context.Friendship.AsNoTracking()
                join caller in context.Account on accountId equals caller.accountId
                join friend in context.Account on friendAccountId equals friend.accountId
                where ((relationship.accountIdA == accountId
                            && relationship.accountIdB == friendAccountId)
                        || (relationship.accountIdB == accountId
                            && relationship.accountIdA == friendAccountId))
                    && accountId != friendAccountId
                    && relationship.statusCode == AcceptedStatus
                    && relationship.endedAtUtc == null
                    && caller.statusCode == ActiveStatus && friend.statusCode == ActiveStatus
                select relationship).Any();
    }
}
