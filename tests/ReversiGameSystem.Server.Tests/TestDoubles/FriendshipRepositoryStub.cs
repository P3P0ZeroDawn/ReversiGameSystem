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
