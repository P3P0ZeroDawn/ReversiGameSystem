using System;
using System.ServiceModel;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

[CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
internal sealed class FriendshipCallback : Proxy.IFriendshipServiceCallback
{
    private readonly Action<long, Proxy.FriendChange> _changed;

    internal FriendshipCallback(Action<long, Proxy.FriendChange> changed)
    {
        _changed = changed;
    }

    public void OnFriendChanged(long accountId, Proxy.FriendChange change) =>
        _changed(accountId, change);

    public void OnFriendRequestReceived(Proxy.FriendRequest request)
    {
        // Friend requests are outside the current presence flow.
    }

    public void OnFriendRequestResponded(string requestId, bool accepted)
    {
        // Friend requests are outside the current presence flow.
    }
}
