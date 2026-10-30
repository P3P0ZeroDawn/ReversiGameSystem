using System;
using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Callbacks;

namespace ReversiGameSystem.Server.ServiceApi;

internal sealed class FriendshipChannel
{
    internal FriendshipChannel(string token, long accountId, IFriendshipServiceCallback callback)
    {
        Token = token;
        AccountId = accountId;
        Callback = callback;
        Communication = (ICommunicationObject)callback;
    }

    internal string Token { get; }
    internal long AccountId { get; }
    internal IFriendshipServiceCallback Callback { get; }
    internal ICommunicationObject Communication { get; }
    internal EventHandler? Terminated { get; set; }

    internal void Detach()
    {
        Communication.Closed -= Terminated;
        Communication.Faulted -= Terminated;
    }
}
