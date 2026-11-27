using System.Collections.Generic;
using System.Linq;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class FriendshipSnapshot
{
    private readonly object _friendSnapshotLock = new object();
    private readonly Dictionary<long, FriendSummary> _friends
        = new Dictionary<long, FriendSummary>();
    private readonly Dictionary<long, FriendChange> _pending = new Dictionary<long, FriendChange>();
    private bool _refreshing;

    internal void BeginRefresh()
    {
        lock (_friendSnapshotLock)
        {
            _refreshing = true;
            _pending.Clear();
        }
    }

    internal void CompleteRefresh(IEnumerable<Proxy.FriendSummary> friends)
    {
        lock (_friendSnapshotLock)
        {
            _friends.Clear();
            foreach (Proxy.FriendSummary friend in friends)
            {
                if (friend.Account != null)
                {
                    FriendSummary summary = new FriendSummary(
                        friend.Account.AccountId,
                        friend.Account.Username,
                        friend.HasProfileImage,
                        friend.IsOnline);
                    _friends[summary.AccountId] = summary;
                }
            }
            foreach (KeyValuePair<long, FriendChange> change in _pending)
            {
                Apply(change.Key, change.Value);
            }
            _pending.Clear();
            _refreshing = false;
        }
    }

    internal void CancelRefresh()
    {
        lock (_friendSnapshotLock)
        {
            _pending.Clear();
            _refreshing = false;
        }
    }

    internal void Change(long accountId, FriendChange change)
    {
        lock (_friendSnapshotLock)
        {
            if (_refreshing)
            {
                _pending[accountId] = change;
            }
            Apply(accountId, change);
        }
    }

    internal FriendSummary[] Read()
    {
        lock (_friendSnapshotLock)
        {
            return _friends.Values.OrderBy(friend => friend.Username)
                .Select(Clone).ToArray();
        }
    }

    internal void Clear()
    {
        lock (_friendSnapshotLock)
        {
            _friends.Clear();
            _pending.Clear();
            _refreshing = false;
        }
    }

    private void Apply(long accountId, FriendChange change)
    {
        if (change == FriendChange.Removed)
        {
            _friends.Remove(accountId);
        }
        else if (_friends.ContainsKey(accountId))
        {
            _friends[accountId].IsOnline = change == FriendChange.Connected;
        }
    }

    private static FriendSummary Clone(FriendSummary friend)
    {
        return new FriendSummary(
            friend.AccountId,
            friend.Username,
            friend.HasProfileImage,
            friend.IsOnline);
    }
}
