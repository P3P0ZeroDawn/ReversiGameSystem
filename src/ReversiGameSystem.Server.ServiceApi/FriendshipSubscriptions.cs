using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Contracts.Callbacks;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Manages the active WCF duplex callback channels for the friendship service.
/// </summary>
public sealed class FriendshipSubscriptions : IDisposable
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, FriendshipChannel> _channels =
        new Dictionary<string, FriendshipChannel>(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>
    /// Registers a new active friendship channel.
    /// </summary>
    /// <param name="token">The authenticated session token.</param>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="callback">The duplex callback channel.</param>
    /// <param name="presenceRegistration">The presence registration to bind to the channel.</param>
    /// <returns>The registered channel.</returns>
    internal FriendshipChannel Register(
        string token,
        long accountId,
        IFriendshipServiceCallback callback,
        IDisposable presenceRegistration)
    {
        FriendshipChannel? existingChannel = null;
        FriendshipChannel newChannel;

        lock (_gate)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FriendshipSubscriptions));
            }

            if (_channels.TryGetValue(token, out existingChannel))
            {
                _channels.Remove(token);
            }

            newChannel = new FriendshipChannel(
                token,
                accountId,
                callback,
                presenceRegistration,
                OnChannelClosed);

            _channels[token] = newChannel;
        }

        existingChannel?.Close();
        return newChannel;
    }

    /// <summary>
    /// Removes and closes the active channel for a session token.
    /// </summary>
    /// <param name="token">The session token whose channel is removed.</param>
    public void Remove(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        FriendshipChannel? channel = null;
        lock (_gate)
        {
            if (_channels.TryGetValue(token, out channel))
            {
                _channels.Remove(token);
            }
        }

        channel?.Close();
    }

    /// <summary>
    /// Returns a thread-safe snapshot of all currently active channels.
    /// </summary>
    /// <returns>The array of active channels.</returns>
    internal FriendshipChannel[] Snapshot()
    {
        lock (_gate)
        {
            return _channels.Values.ToArray();
        }
    }

    /// <summary>
    /// Finds the active channels for a given account identifier.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>The array of matching active channels.</returns>
    internal FriendshipChannel[] FindChannelsForAccount(long accountId)
    {
        lock (_gate)
        {
            return _channels.Values
                .Where(channel => channel.AccountId == accountId)
                .ToArray();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        FriendshipChannel[] channelsToClose;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            channelsToClose = _channels.Values.ToArray();
            _channels.Clear();
        }

        foreach (FriendshipChannel channel in channelsToClose)
        {
            channel.Close();
        }
    }

    private void OnChannelClosed(FriendshipChannel channel)
    {
        lock (_gate)
        {
            if (_channels.TryGetValue(channel.Token, out FriendshipChannel? current)
                && ReferenceEquals(current, channel))
            {
                _channels.Remove(channel.Token);
            }
        }
    }
}
