using System.Collections.Generic;
using System.Linq;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

// Reconciles notifications received while a snapshot request is in flight.
internal sealed class FriendshipSnapshot
{
    private readonly object _sync = new object();
    private readonly Dictionary<long, Proxy.FriendSummary> _friends = new();
    private readonly Dictionary<long, Proxy.FriendChange> _pending = new();
    private bool _refreshing;

    internal void BeginRefresh()
    {
        lock (_sync)
        {
            _refreshing = true;
            _pending.Clear();
        }
    }

    internal void CompleteRefresh(IEnumerable<Proxy.FriendSummary> friends)
    {
        lock (_sync)
        {
            _friends.Clear();
            foreach (Proxy.FriendSummary friend in friends)
            {
                if (friend.Account != null)
                {
                    _friends[friend.Account.AccountId] = Clone(friend);
                }
            }
            foreach (KeyValuePair<long, Proxy.FriendChange> change in _pending)
            {
                Apply(change.Key, change.Value);
            }
            _pending.Clear();
            _refreshing = false;
        }
    }

    internal void CancelRefresh()
    {
        lock (_sync)
        {
            _pending.Clear();
            _refreshing = false;
        }
    }

    internal void Change(long accountId, Proxy.FriendChange change)
    {
        lock (_sync)
        {
            if (_refreshing)
            {
                _pending[accountId] = change;
            }
            Apply(accountId, change);
        }
    }

    internal Proxy.FriendSummary[] Read()
    {
        lock (_sync)
        {
            return _friends.Values.OrderBy(friend => friend.Account.Username)
                .Select(Clone).ToArray();
        }
    }

    internal void Clear()
    {
        lock (_sync)
        {
            _friends.Clear();
            _pending.Clear();
            _refreshing = false;
        }
    }

    private void Apply(long accountId, Proxy.FriendChange change)
    {
        if (change == Proxy.FriendChange.Removed)
        {
            _friends.Remove(accountId);
        }
        else if (_friends.TryGetValue(accountId, out Proxy.FriendSummary friend))
        {
            friend.IsOnline = change == Proxy.FriendChange.Connected;
        }
    }

    private static Proxy.FriendSummary Clone(Proxy.FriendSummary friend) => new()
    {
        Account = new Proxy.AccountReference
        {
            AccountId = friend.Account.AccountId,
            Username = friend.Account.Username
        },
        IsOnline = friend.IsOnline,
        HasProfileImage = friend.HasProfileImage
    };
}
