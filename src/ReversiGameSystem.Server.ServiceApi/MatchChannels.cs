using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.ServiceApi;

internal sealed class MatchChannels<T> : IDisposable where T : class
{
    private readonly object _gate = new object();
    private readonly object _dispatchSync = new object();
    private readonly List<Subscription> _channels = new List<Subscription>();
    private readonly Queue<Action> _dispatchQueue = new Queue<Action>();
    private readonly Func<string, long, bool> _authorize;
    private readonly Action<string, long> _removed;
    private readonly ILogger _logger;
    private readonly Timer _timer;
    private bool _disposed;
    private bool _dispatching;
    private int _sweeping;

    internal MatchChannels(
        Func<string, long, bool> authorize,
        Action<string, long> removed,
        ILogger logger)
    {
        _authorize = authorize ?? throw new ArgumentNullException(nameof(authorize));
        _removed = removed ?? throw new ArgumentNullException(nameof(removed));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timer = new Timer(_ => Sweep(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    internal void Register(string token, long matchId)
    {
        T callback = OperationContext.Current.GetCallbackChannel<T>();
        Subscription channel = new Subscription(token, matchId, callback);
        channel.Terminated = (_, _) => Remove(channel, true);
        lock (_gate)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MatchChannels<T>));
            }
            foreach (Subscription old in _channels.Where(item => item.Token == token
                && item.MatchId == matchId).ToArray())
            {
                _channels.Remove(old);
                old.Detach();
            }
            _channels.Add(channel);
            channel.Communication.Closed += channel.Terminated;
            channel.Communication.Faulted += channel.Terminated;
        }

    }

    internal void RemoveSession(string token)
    {
        foreach (Subscription channel in Snapshot().Where(item => item.Token == token))
        {
            Remove(channel, true);
        }
    }

    internal void Publish(long matchId, Action<string, T> send)
    {
        PublishWhere(matchId, (token, _, callback) => send(token, callback));
    }

    internal void PublishAll(Action<string, long, T> send)
    {
        PublishWhere(-1, send);
    }

    private void PublishWhere(long matchId, Action<string, long, T> send)
    {
        QueueDispatch(() =>
        {
            foreach (Subscription channel in Snapshot().Where(item => matchId == -1 || item.MatchId == 0
                || item.MatchId == matchId))
            {
                try
                {
                    if (!IsValid(channel))
                    {
                        Remove(channel, true);
                        continue;
                    }
                    send(channel.Token, channel.MatchId, channel.Callback);
                }
                catch (ReversiGameSystem.Server.Application.Matches.MatchRejectedException)
                {
                    if (channel.MatchId != 0)
                    {
                        Remove(channel, true);
                    }
                }
                catch (ReversiGameSystem.Server.Application.Accounts.SessionRejectedException)
                {
                    Remove(channel, true);
                }
                catch (CommunicationException exception)
                {
                    Log("MatchCallback", exception);
                    Remove(channel, true);
                }
                catch (TimeoutException exception)
                {
                    Log("MatchCallback", exception);
                    Remove(channel, true);
                }
                catch (Exception exception)
                {
                    Log("MatchCallback", exception);
                }
            }
        });
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            foreach (Subscription channel in _channels)
            {
                channel.Detach();
            }
            _channels.Clear();
        }
        _timer.Dispose();
        lock (_dispatchSync)
        {
            _dispatchQueue.Clear();
        }
    }

    private bool IsValid(Subscription channel)
    {
        return channel.Communication.State == CommunicationState.Opened
            && _authorize(channel.Token, channel.MatchId);
    }

    private Subscription[] Snapshot()
    {
        lock (_gate)
        {
            return _channels.ToArray();
        }
    }

    private void Sweep()
    {
        if (Interlocked.Exchange(ref _sweeping, 1) != 0)
        {
            return;
        }
        try
        {
            foreach (Subscription channel in Snapshot())
            {
                try
                {
                    if (!IsValid(channel))
                    {
                        Remove(channel, true);
                    }
                }
                catch (Exception exception)
                {
                    Log("MatchChannelSweep", exception);
                }
            }
        }
        finally
        {
            Volatile.Write(ref _sweeping, 0);
        }
    }

    private void Remove(Subscription channel, bool notify)
    {
        lock (_gate)
        {
            if (!_channels.Remove(channel))
            {
                return;
            }
            channel.Detach();
        }
        if (notify)
        {
            try
            {
                _removed(channel.Token, channel.MatchId);
            }
            catch (Exception exception)
            {
                Log("MatchChannelCleanup", exception);
            }
        }
    }

    private void Log(string operation, Exception exception)
    {
        _logger.LogError(
            exception,
            "The {Operation} background operation failed. CorrelationId: {CorrelationId}",
            operation,
            CorrelationContext.GetOrCreate());
    }

    private void QueueDispatch(Action action)
    {
        bool startDispatcher = false;
        lock (_dispatchSync)
        {
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
                Log("MatchCallbackDispatch", exception);
            }
        }
    }

    private sealed class Subscription
    {
        internal Subscription(string token, long matchId, T callback)
        {
            Token = token;
            MatchId = matchId;
            Callback = callback;
            Communication = (ICommunicationObject)callback;
        }
        internal string Token { get; }
        internal long MatchId { get; }
        internal T Callback { get; }
        internal ICommunicationObject Communication { get; }
        internal EventHandler? Terminated { get; set; }
        internal void Detach()
        {
            Communication.Closed -= Terminated;
            Communication.Faulted -= Terminated;
        }
    }
}
