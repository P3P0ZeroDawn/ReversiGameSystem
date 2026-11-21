using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Contracts.Callbacks;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Manages the active WCF duplex callback channels for the friendship service.
/// </summary>
public sealed class FriendshipSubscriptions : IDisposable
{
    private readonly object _gate = new object();
    private readonly object _dispatchSync = new object();
    private readonly Dictionary<string, Channel> _channels =
        new Dictionary<string, Channel>(StringComparer.Ordinal);
    private readonly Queue<Action> _dispatchQueue = new Queue<Action>();
    private readonly ILogger<FriendshipSubscriptions> _logger;
    private int _sweepOffset;
    private bool _disposed;
    private bool _dispatching;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendshipSubscriptions"/> class.
    /// </summary>
    /// <param name="logger">The diagnostic logger for callback and sweep operations.</param>
    /// <exception cref="ArgumentNullException">The logger is null.</exception>
    public FriendshipSubscriptions(ILogger<FriendshipSubscriptions> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new active friendship channel.
    /// </summary>
    /// <param name="token">The authenticated session token.</param>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="callback">The duplex callback channel.</param>
    /// <param name="presenceRegistration">The presence registration bound to the channel.</param>
    internal void Register(
        string token,
        long accountId,
        IFriendshipServiceCallback callback,
        IDisposable presenceRegistration)
    {
        Channel? existingChannel = null;
        Channel newChannel;

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

            newChannel = new Channel(
                token,
                accountId,
                callback,
                presenceRegistration,
                OnChannelClosed);

            _channels[token] = newChannel;
        }

        existingChannel?.Close();
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

        Channel? channel = null;
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
    /// Queues a callback notification to all active channels belonging to the target accounts.
    /// </summary>
    /// <param name="accountIds">The target account identifiers.</param>
    /// <param name="send">The callback action to invoke for each matching channel.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public void PublishToAccounts(
        HashSet<long> accountIds,
        Action<IFriendshipServiceCallback> send)
    {
        if (accountIds == null)
        {
            throw new ArgumentNullException(nameof(accountIds));
        }

        if (send == null)
        {
            throw new ArgumentNullException(nameof(send));
        }

        if (accountIds.Count == 0)
        {
            return;
        }

        HashSet<long> targets = new HashSet<long>(accountIds);
        QueueDispatch(() => PublishNow(targets, send));
    }

    /// <summary>
    /// Checks up to <paramref name="maximumChannels"/> active channels and removes invalid ones.
    /// </summary>
    /// <param name="maximumChannels">The maximum number of channels to inspect per sweep.</param>
    /// <param name="isSessionValid">Predicate validating whether a session token is active.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maximumChannels"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="isSessionValid"/> is null.
    /// </exception>
    public void SweepChannels(int maximumChannels, Func<string, bool> isSessionValid)
    {
        if (maximumChannels <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumChannels),
                maximumChannels,
                "Maximum channels must be positive.");
        }

        if (isSessionValid == null)
        {
            throw new ArgumentNullException(nameof(isSessionValid));
        }

        Channel[] channels = Snapshot();
        if (channels.Length == 0)
        {
            _sweepOffset = 0;
            return;
        }

        int count = Math.Min(maximumChannels, channels.Length);
        for (int index = 0; index < count; index++)
        {
            Channel channel = channels[(_sweepOffset + index) % channels.Length];
            bool isOpen = channel.Communication.State == CommunicationState.Opened;
            if (!isOpen || !isSessionValid(channel.Token))
            {
                Remove(channel.Token);
            }
        }

        _sweepOffset = (_sweepOffset + count) % channels.Length;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Channel[] channelsToClose;
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

        lock (_dispatchSync)
        {
            _dispatchQueue.Clear();
        }

        foreach (Channel channel in channelsToClose)
        {
            channel.Close();
        }
    }

    private Channel[] Snapshot()
    {
        lock (_gate)
        {
            return _channels.Values.ToArray();
        }
    }

    private void OnChannelClosed(Channel channel)
    {
        lock (_gate)
        {
            if (_channels.TryGetValue(channel.Token, out Channel? current)
                && ReferenceEquals(current, channel))
            {
                _channels.Remove(channel.Token);
            }
        }
    }

    private void PublishNow(
        HashSet<long> accountIds,
        Action<IFriendshipServiceCallback> send)
    {
        Channel[] activeChannels = Snapshot();
        for (int index = 0; index < activeChannels.Length; index++)
        {
            Channel subscriber = activeChannels[index];
            if (!accountIds.Contains(subscriber.AccountId) || subscriber.Callback == null)
            {
                continue;
            }

            try
            {
                send(subscriber.Callback);
            }
            catch (Exception exception) when (exception is CommunicationException
                or TimeoutException
                or ObjectDisposedException)
            {
                Remove(subscriber.Token);
                _logger.LogWarning(
                    exception,
                    "Disconnected friendship callback channel for account {AccountId} removed.",
                    subscriber.AccountId);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected failure publishing friendship callback for account {AccountId}.",
                    subscriber.AccountId);
            }
        }
    }

    private void QueueDispatch(Action action)
    {
        bool startDispatcher = false;
        lock (_dispatchSync)
        {
            if (_disposed)
            {
                return;
            }

            _dispatchQueue.Enqueue(action);
            if (!_dispatching)
            {
                _dispatching = true;
                startDispatcher = true;
            }
        }

        if (startDispatcher)
        {
            ThreadPool.QueueUserWorkItem(_ => ProcessDispatchQueue());
        }
    }

    private void ProcessDispatchQueue()
    {
        while (true)
        {
            Action? action = null;
            lock (_dispatchSync)
            {
                if (_dispatchQueue.Count == 0)
                {
                    _dispatching = false;
                    return;
                }

                action = _dispatchQueue.Dequeue();
            }

            try
            {
                action();
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The friendship callback dispatch operation failed.");
            }
        }
    }

    private sealed class Channel
    {
        private readonly Action<Channel>? _onClosed;
        private int _closed;

        internal Channel(
            string token,
            long accountId,
            IFriendshipServiceCallback callback,
            IDisposable presenceRegistration,
            Action<Channel>? onClosed)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            AccountId = accountId;
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            Communication = (ICommunicationObject)callback;
            PresenceRegistration = presenceRegistration
                ?? throw new ArgumentNullException(nameof(presenceRegistration));
            _onClosed = onClosed;

            Terminated = (_, _) => Close();
            Communication.Closed += Terminated;
            Communication.Faulted += Terminated;
        }

        internal string Token { get; }

        internal long AccountId { get; }

        internal IFriendshipServiceCallback? Callback { get; private set; }

        internal ICommunicationObject Communication { get; }

        private IDisposable? PresenceRegistration { get; set; }

        private EventHandler? Terminated { get; set; }

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
            catch (Exception exception) when (exception is CommunicationException
                or TimeoutException
                or ObjectDisposedException)
            {
                // Channel is already faulted, timed out, or disposed during teardown.
            }

            _onClosed?.Invoke(this);
        }
    }
}
