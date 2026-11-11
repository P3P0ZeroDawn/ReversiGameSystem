using System;
using System.ServiceModel;
using System.Threading;
using ReversiGameSystem.Server.Contracts.Callbacks;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Encapsulates an active WCF duplex friendship callback channel and its presence registration.
/// </summary>
internal sealed class FriendshipChannel
{
    private readonly Action<FriendshipChannel>? _onClosed;
    private int _closed;

    internal FriendshipChannel(
        string token,
        long accountId,
        IFriendshipServiceCallback callback,
        IDisposable presenceRegistration,
        Action<FriendshipChannel>? onClosed)
    {
        Token = token ?? throw new ArgumentNullException(nameof(token));
        AccountId = accountId;
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        Communication = (ICommunicationObject)callback;
        PresenceRegistration = presenceRegistration ?? throw new ArgumentNullException(nameof(presenceRegistration));
        _onClosed = onClosed;

        Terminated = (_, _) => Close();
        Communication.Closed += Terminated;
        Communication.Faulted += Terminated;
    }

    internal string Token { get; }

    internal long AccountId { get; }

    internal IFriendshipServiceCallback? Callback { get; private set; }

    internal ICommunicationObject Communication { get; }

    internal IDisposable? PresenceRegistration { get; private set; }

    internal EventHandler? Terminated { get; private set; }

    /// <summary>
    /// Idempotently closes the channel, detaches event handlers, and disposes presence.
    /// </summary>
    internal void Close()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

        if (Terminated != null)
        {
            Communication.Closed -= Terminated;
            Communication.Faulted -= Terminated;
            Terminated = null;
        }

        PresenceRegistration?.Dispose();
        PresenceRegistration = null;

        Callback = null;

        try
        {
            Communication.Abort();
        }
        catch
        {
        }

        _onClosed?.Invoke(this);
    }
}
