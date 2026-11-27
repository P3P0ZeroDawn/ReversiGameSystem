using System;
using System.ServiceModel;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

[CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
internal sealed class FriendshipCallback : Proxy.IFriendshipServiceCallback
{
    private readonly Action<long, FriendChange> _changed;
    private readonly Action _requestsChanged;
    private readonly Action<bool> _requestResponded;

    internal FriendshipCallback(Action<long, FriendChange> changed,
        Action requestsChanged, Action<bool> requestResponded)
    {
        _changed = changed;
        _requestsChanged = requestsChanged;
        _requestResponded = requestResponded;
    }

    public void OnFriendChanged(long accountId, Proxy.FriendChange change)
    {
        FriendChange mappedChange = change switch
        {
            Proxy.FriendChange.Connected => FriendChange.Connected,
            Proxy.FriendChange.Disconnected => FriendChange.Disconnected,
            Proxy.FriendChange.Removed => FriendChange.Removed,
            _ => throw new ArgumentOutOfRangeException(nameof(change))
        };
        _changed(accountId, mappedChange);
    }

    public void OnFriendRequestReceived(Proxy.FriendRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RequestId))
        {
            throw new InvalidOperationException("The friend request notification is invalid.");
        }
        _requestsChanged();
    }

    public void OnFriendRequestResponded(string requestId, bool accepted)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new InvalidOperationException("The friend response notification is invalid.");
        }
        _requestResponded(accepted);
    }
}
