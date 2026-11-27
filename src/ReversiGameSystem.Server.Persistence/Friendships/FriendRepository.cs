using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Friendships;

public sealed class FriendRepository : IFriendshipRepository
{
    private const string ActiveStatus = "ACTIVE";
    private const string AcceptedStatus = "ACCEPTED";
    private const string PendingStatus = "PENDING";
    private const string RejectedStatus = "REJECTED";
    private const string RemovedStatus = "REMOVED";
    private const string FirstAccountParameter = "@firstAccountId";
    private const string SecondAccountParameter = "@secondAccountId";
    private const int PairAccountCount = 2;
    private const int NextRequestMillisecond = 1;
    private const string LockedRelationshipSql =
        "SELECT accountIdA, accountIdB, requestedByAccountId, statusCode, "
        + "requestedAtUtc, respondedAtUtc, endedAtUtc "
        + "FROM dbo.Friendship WITH (UPDLOCK, HOLDLOCK) "
        + "WHERE accountIdA = @firstAccountId AND accountIdB = @secondAccountId";

    public bool IsAccountActive(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        return context.Account.AsNoTracking().Any(account =>
            account.accountId == accountId && account.statusCode == ActiveStatus);
    }

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

    public long[] ReadConfirmedFriendIds(long accountId)
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
                select friend.accountId)
            .ToArray();
    }
    public FriendAccount[] FindPlayers(long accountId, string searchText)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        IQueryable<Account> accounts = context.Account.AsNoTracking()
            .Where(account => account.accountId != accountId
                && account.statusCode == ActiveStatus && account.username.Contains(searchText))
            .OrderBy(account => account.username).ThenBy(account => account.accountId);
        return ReadFriendAccounts(accounts);
    }

    public FriendshipResult<PendingFriendRequest> CreatePendingRequest(
        long senderId, long recipientId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable);
        Friendship? relationship = ReadLockedRelationship(context, senderId, recipientId)
            .SingleOrDefault();
        FriendAccount[] accounts = ReadActiveAccounts(context, senderId, recipientId);
        FriendAccount? sender = accounts.SingleOrDefault(account => account.AccountId == senderId);
        FriendAccount? recipient = accounts.SingleOrDefault(
            account => account.AccountId == recipientId);
        if (sender == null)
        {
            return new FriendshipResult<PendingFriendRequest>(FriendshipStatus.InactiveAccount);
        }
        if (recipient == null)
        {
            return new FriendshipResult<PendingFriendRequest>(FriendshipStatus.AccountNotFound);
        }
        if (relationship != null && relationship.endedAtUtc == null)
        {
            if (relationship.statusCode == AcceptedStatus)
            {
                return new FriendshipResult<PendingFriendRequest>(
                    FriendshipStatus.FriendshipAlreadyExists);
            }
            if (relationship.statusCode == PendingStatus)
            {
                return new FriendshipResult<PendingFriendRequest>(
                    FriendshipStatus.RequestAlreadyPending);
            }
        }
        Friendship pending = PreparePendingRelationship(relationship, senderId, recipientId);
        if (relationship == null)
        {
            context.Friendship.Add(pending);
        }
        context.SaveChanges();
        transaction.Commit();
        PendingFriendRequest request = MapRequest(pending, sender, recipient);
        return new FriendshipResult<PendingFriendRequest>(request);
    }

    public PendingFriendRequest[] ReadPendingRequests(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        return (from relationship in context.Friendship.AsNoTracking()
                join sender in context.Account on relationship.requestedByAccountId
                    equals sender.accountId
                join recipient in context.Account on
                    (relationship.accountIdA == relationship.requestedByAccountId
                        ? relationship.accountIdB : relationship.accountIdA)
                    equals recipient.accountId
                where (relationship.accountIdA == accountId || relationship.accountIdB == accountId)
                    && relationship.statusCode == PendingStatus && relationship.endedAtUtc == null
                    && sender.statusCode == ActiveStatus && recipient.statusCode == ActiveStatus
                orderby relationship.requestedAtUtc, relationship.accountIdA,
                    relationship.accountIdB
                select new
                {
                    Relationship = relationship,
                    SenderId = sender.accountId,
                    SenderName = sender.username,
                    SenderHasImage = sender.profileImageUri != null && sender.profileImageUri != "",
                    RecipientId = recipient.accountId,
                    RecipientName = recipient.username,
                    RecipientHasImage = recipient.profileImageUri != null
                        && recipient.profileImageUri != ""
                })
            .ToArray()
            .Select(item => MapRequest(item.Relationship,
                new FriendAccount(item.SenderId, item.SenderName, item.SenderHasImage),
                new FriendAccount(item.RecipientId, item.RecipientName, item.RecipientHasImage)))
            .ToArray();
    }

    public FriendshipStatus RespondToRequest(
        long accountId, PendingFriendRequest request, bool accept)
    {
        if (request.Recipient.AccountId != accountId)
        {
            return FriendshipStatus.UnauthorizedRequest;
        }
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable);
        Friendship? relationship = ReadLockedRelationship(
            context, request.Sender.AccountId, request.Recipient.AccountId).SingleOrDefault();
        if (relationship == null || relationship.statusCode != PendingStatus
            || relationship.endedAtUtc != null
            || relationship.requestedAtUtc != request.CreatedAtUtc
            || relationship.requestedByAccountId != request.Sender.AccountId)
        {
            return FriendshipStatus.RequestUnavailable;
        }
        FriendAccount[] accounts = ReadActiveAccounts(context, accountId, request.Sender.AccountId);
        if (!accounts.Any(account => account.AccountId == accountId))
        {
            return FriendshipStatus.InactiveAccount;
        }
        if (accounts.Length != PairAccountCount)
        {
            return FriendshipStatus.AccountNotFound;
        }
        relationship.statusCode = accept ? AcceptedStatus : RejectedStatus;
        relationship.respondedAtUtc = DateTime.UtcNow;
        context.SaveChanges();
        transaction.Commit();
        return FriendshipStatus.Success;
    }

    public FriendshipStatus RemoveFriendship(long accountId, long friendAccountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable);
        Friendship? relationship = ReadLockedRelationship(context, accountId, friendAccountId)
            .SingleOrDefault();
        if (relationship == null || relationship.statusCode != AcceptedStatus
            || relationship.endedAtUtc != null)
        {
            return FriendshipStatus.FriendshipNotFound;
        }
        if (!context.Account.Any(account => account.accountId == accountId
            && account.statusCode == ActiveStatus))
        {
            return FriendshipStatus.InactiveAccount;
        }
        relationship.statusCode = RemovedStatus;
        relationship.endedAtUtc = DateTime.UtcNow;
        context.SaveChanges();
        transaction.Commit();
        return FriendshipStatus.Success;
    }

    private static Friendship[] ReadLockedRelationship(
        ReversiGameSystemEntities context, long accountId, long friendAccountId)
    {
        long firstAccountId = Math.Min(accountId, friendAccountId);
        long secondAccountId = Math.Max(accountId, friendAccountId);
        return context.Friendship.SqlQuery(LockedRelationshipSql,
            new SqlParameter(FirstAccountParameter, firstAccountId),
            new SqlParameter(SecondAccountParameter, secondAccountId)).ToArray();
    }

    private static FriendAccount[] ReadActiveAccounts(
        ReversiGameSystemEntities context, long accountId, long otherAccountId)
    {
        IQueryable<Account> accounts = context.Account.AsNoTracking()
            .Where(account => (account.accountId == accountId
                    || account.accountId == otherAccountId)
                && account.statusCode == ActiveStatus);
        return ReadFriendAccounts(accounts);
    }

    private static Friendship PreparePendingRelationship(
        Friendship? relationship, long senderId, long recipientId)
    {
        DateTime now = DateTime.UtcNow;
        long ticks = now.Ticks - now.Ticks % TimeSpan.TicksPerMillisecond;
        DateTime createdAtUtc = new DateTime(ticks, DateTimeKind.Utc);
        if (relationship == null)
        {
            relationship = new Friendship
            {
                accountIdA = Math.Min(senderId, recipientId),
                accountIdB = Math.Max(senderId, recipientId)
            };
        }
        else if (createdAtUtc <= relationship.requestedAtUtc)
        {
            createdAtUtc = relationship.requestedAtUtc.AddMilliseconds(NextRequestMillisecond);
        }
        relationship.requestedByAccountId = senderId;
        relationship.statusCode = PendingStatus;
        relationship.requestedAtUtc = createdAtUtc;
        relationship.respondedAtUtc = null;
        relationship.endedAtUtc = null;
        return relationship;
    }

    private static FriendAccount[] ReadFriendAccounts(IQueryable<Account> accounts)
    {
        return accounts.Select(account => new
        {
            account.accountId,
            account.username,
            HasProfileImage = account.profileImageUri != null && account.profileImageUri != ""
        }).ToArray()
            .Select(account => new FriendAccount(
                account.accountId, account.username, account.HasProfileImage)).ToArray();
    }

    private static PendingFriendRequest MapRequest(
        Friendship relationship, FriendAccount sender, FriendAccount recipient)
    {
        return new PendingFriendRequest(sender, recipient, relationship.requestedAtUtc);
    }
}
