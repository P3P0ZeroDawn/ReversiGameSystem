using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ReversiGameSystem.Server.ServiceApi;

internal class MatchCallbackSubscriptions<TCallback> : IDisposable
    where TCallback : class
{
    private const long AllMatchesSubscription = 0;
    private readonly object _subscriptionsSync = new object();
    private readonly object _dispatchSync = new object();
    private readonly List<Subscription> _subscriptions = new List<Subscription>();
    private readonly Queue<Action> _dispatchQueue = new Queue<Action>();
    private readonly Func<string, long, bool> _canKeepSubscription;
    private readonly Action<string, long>? _subscriptionRemoved;
    private readonly ILogger _logger;
    private bool _disposed;
    private bool _dispatching;

    internal MatchCallbackSubscriptions(
        Func<string, long, bool> canKeepSubscription,
        ILogger logger)
        : this(canKeepSubscription, null, logger)
    {
    }

    internal MatchCallbackSubscriptions(
        Func<string, long, bool> canKeepSubscription,
        Action<string, long>? subscriptionRemoved,
        ILogger logger)
    {
        _canKeepSubscription = canKeepSubscription
            ?? throw new ArgumentNullException(nameof(canKeepSubscription));
        _subscriptionRemoved = subscriptionRemoved;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal void Register(string token, long matchId)
    {
        TCallback callback = OperationContext.Current.GetCallbackChannel<TCallback>();
        Subscription channel = new Subscription(token, matchId, callback);
        channel.Terminated = (_, _) => Remove(channel, true);
        lock (_subscriptionsSync)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MatchCallbackSubscriptions<TCallback>));
            }
            foreach (Subscription old in _subscriptions.Where(item => item.Token == token
                && item.MatchId == matchId).ToArray())
            {
                _subscriptions.Remove(old);
                old.Detach();
            }
            _subscriptions.Add(channel);
            channel.Communication.Closed += channel.Terminated;
            channel.Communication.Faulted += channel.Terminated;
        }

    }

    internal void RegisterForAllMatches(string token)
    {
        Register(token, AllMatchesSubscription);
    }

    internal void RemoveSession(string token)
    {
        foreach (Subscription channel in Snapshot().Where(item => item.Token == token))
        {
            Remove(channel, true);
        }
    }

    internal void Publish(long matchId, Action<string, TCallback> send)
    {
        PublishWhere(matchId, (token, _, callback) => send(token, callback));
    }

    internal void PublishAll(Action<string, long, TCallback> send)
    {
        PublishWhere(null, send);
    }

    private void PublishWhere(
        long? matchId,
        Action<string, long, TCallback> send)
    {
        QueueDispatch(() => PublishNow(matchId, send));
    }

    private void PublishNow(
        long? matchId,
        Action<string, long, TCallback> send)
    {
        foreach (Subscription channel in Snapshot().Where(item =>
            !matchId.HasValue
            || item.MatchId == AllMatchesSubscription
            || item.MatchId == matchId.Value))
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
                if (channel.MatchId != AllMatchesSubscription)
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
    }

    public void Dispose()
    {
        lock (_subscriptionsSync)
        {
            _disposed = true;
            foreach (Subscription channel in _subscriptions)
            {
                channel.Detach();
            }
            _subscriptions.Clear();
        }
        lock (_dispatchSync)
        {
            _dispatchQueue.Clear();
        }
    }

    private bool IsValid(Subscription channel)
    {
        return channel.Communication.State == CommunicationState.Opened
            && _canKeepSubscription(channel.Token, channel.MatchId);
    }

    private Subscription[] Snapshot()
    {
        lock (_subscriptionsSync)
        {
            return _subscriptions.ToArray();
        }
    }

    private void Remove(Subscription channel, bool notify)
    {
        lock (_subscriptionsSync)
        {
            if (!_subscriptions.Remove(channel))
            {
                return;
            }
            channel.Detach();
        }
        if (notify && _subscriptionRemoved is not null)
        {
            try
            {
                _subscriptionRemoved(channel.Token, channel.MatchId);
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
        internal Subscription(string token, long matchId, TCallback callback)
        {
            Token = token;
            MatchId = matchId;
            Callback = callback;
            Communication = (ICommunicationObject)callback;
        }
        internal string Token { get; }
        internal long MatchId { get; }
        internal TCallback Callback { get; }
        internal ICommunicationObject Communication { get; }
        internal EventHandler? Terminated { get; set; }
        internal void Detach()
        {
            Communication.Closed -= Terminated;
            Communication.Faulted -= Terminated;
        }
    }
}
