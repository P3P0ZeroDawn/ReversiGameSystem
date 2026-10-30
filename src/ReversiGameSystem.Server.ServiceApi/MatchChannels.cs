using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace ReversiGameSystem.Server.ServiceApi;

internal sealed class MatchChannels<T> : IDisposable where T : class
{
    private readonly object _gate = new object();
    private readonly List<Subscription> _channels = new List<Subscription>();
    private readonly Func<string, long, bool> _authorize;
    private readonly Action<string> _removed;
    private readonly MatchBoundary _boundary;
    private readonly Timer _timer;
    private bool _disposed;
    private int _sweeping;

    internal MatchChannels(Func<string, long, bool> authorize, Action<string> removed,
        MatchBoundary boundary)
    {
        _authorize = authorize;
        _removed = removed;
        _boundary = boundary;
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
        => PublishWhere(matchId, (token, _, callback) => send(token, callback));

    internal void PublishAll(Action<string, long, T> send) => PublishWhere(-1, send);

    private void PublishWhere(long matchId, Action<string, long, T> send)
    {
        ThreadPool.QueueUserWorkItem(_ =>
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
                    _boundary.Log("MatchCallback", exception);
                    Remove(channel, true);
                }
                catch (TimeoutException exception)
                {
                    _boundary.Log("MatchCallback", exception);
                    Remove(channel, true);
                }
                catch (Exception exception)
                {
                    _boundary.Log("MatchCallback", exception);
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
    }

    private bool IsValid(Subscription channel) => channel.Communication.State == CommunicationState.Opened
        && _authorize(channel.Token, channel.MatchId);

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
                    _boundary.Log("MatchChannelSweep", exception);
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
                _removed(channel.Token);
            }
            catch (Exception exception)
            {
                _boundary.Log("MatchChannelCleanup", exception);
            }
        }
    }

    private sealed class Subscription
    {
        internal Subscription(string token, long matchId, T callback)
        {
            Token = token; MatchId = matchId; Callback = callback;
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
