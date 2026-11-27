using System;
using ReversiGameSystem.Server.Application.Friendships;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class FriendshipRepositoryStub : IFriendshipRepository
{
    internal int IsAccountActiveCalls { get; private set; }

    internal int GetFriendsCalls { get; private set; }

    internal int AreFriendsCalls { get; private set; }

    internal int ReadConfirmedFriendIdsCalls { get; private set; }

    internal long LastAccountId { get; private set; }

    internal long LastFriendAccountId { get; private set; }

    internal bool IsAccountActiveResult { get; set; } = true;

    internal FriendAccount[] Friends { get; set; } = Array.Empty<FriendAccount>();

    internal bool AreFriendsResult { get; set; }

    internal long[] ConfirmedFriendIds { get; set; } = Array.Empty<long>();

    internal bool ThrowOnIsAccountActive { get; set; }

    internal bool ThrowOnGetFriends { get; set; }

    internal bool ThrowOnAreFriends { get; set; }

    internal bool ThrowOnReadConfirmedFriendIds { get; set; }

    internal FriendAccount[] SearchPlayers { get; set; } = Array.Empty<FriendAccount>();

    internal PendingFriendRequest[] PendingRequests { get; set; }
        = Array.Empty<PendingFriendRequest>();

    internal FriendshipResult<PendingFriendRequest> CreatedRequest { get; set; }
        = new FriendshipResult<PendingFriendRequest>(FriendshipStatus.AccountNotFound);

    internal FriendshipStatus ResponseStatus { get; set; } = FriendshipStatus.Success;

    internal FriendshipStatus RemovalStatus { get; set; } = FriendshipStatus.Success;

    internal int CreateRequestCalls { get; private set; }

    internal int RespondToRequestCalls { get; private set; }

    internal int RemoveFriendshipCalls { get; private set; }

    internal string LastSearchText { get; private set; } = string.Empty;

    internal bool LastAcceptance { get; private set; }

    public FriendAccount[] FindPlayers(long accountId, string searchText)
    {
        LastAccountId = accountId;
        LastSearchText = searchText;
        return SearchPlayers;
    }

    public FriendshipResult<PendingFriendRequest> CreatePendingRequest(
        long senderId, long recipientId)
    {
        CreateRequestCalls++;
        LastAccountId = senderId;
        LastFriendAccountId = recipientId;
        return CreatedRequest;
    }

    public PendingFriendRequest[] ReadPendingRequests(long accountId)
    {
        LastAccountId = accountId;
        return PendingRequests;
    }

    public FriendshipStatus RespondToRequest(
        long accountId, PendingFriendRequest request, bool accept)
    {
        RespondToRequestCalls++;
        LastAccountId = accountId;
        LastAcceptance = accept;
        return ResponseStatus;
    }

    public FriendshipStatus RemoveFriendship(long accountId, long friendAccountId)
    {
        RemoveFriendshipCalls++;
        LastAccountId = accountId;
        LastFriendAccountId = friendAccountId;
        return RemovalStatus;
    }

    public bool IsAccountActive(long accountId)
    {
        IsAccountActiveCalls++;
        LastAccountId = accountId;
        if (ThrowOnIsAccountActive)
        {
            throw new InvalidOperationException("Simulated friendship repository failure.");
        }

        return IsAccountActiveResult;
    }

    public FriendAccount[] GetFriends(long accountId)
    {
        GetFriendsCalls++;
        LastAccountId = accountId;
        if (ThrowOnGetFriends)
        {
            throw new InvalidOperationException("Simulated friendship repository failure.");
        }

        return Friends;
    }

    public bool AreFriends(long accountId, long friendAccountId)
    {
        AreFriendsCalls++;
        LastAccountId = accountId;
        LastFriendAccountId = friendAccountId;
        if (ThrowOnAreFriends)
        {
            throw new InvalidOperationException("Simulated friendship repository failure.");
        }

        return AreFriendsResult;
    }

    public long[] ReadConfirmedFriendIds(long accountId)
    {
        ReadConfirmedFriendIdsCalls++;
        LastAccountId = accountId;
        if (ThrowOnReadConfirmedFriendIds)
        {
            throw new InvalidOperationException("Simulated friendship repository failure.");
        }

        return ConfirmedFriendIds;
    }
}
