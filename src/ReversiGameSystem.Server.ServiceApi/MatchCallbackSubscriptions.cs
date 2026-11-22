using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Manages WCF duplex callback subscriptions and asynchronous dispatch for match services.
/// </summary>
/// <typeparam name="TCallback">The WCF callback contract interface.</typeparam>
public sealed class MatchCallbackSubscriptions<TCallback> : IDisposable
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

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="MatchCallbackSubscriptions{TCallback}"/> class.
    /// </summary>
    /// <param name="canKeepSubscription">
    /// Predicate validating whether a subscription remains authorized.
    /// </param>
    /// <param name="logger">The diagnostic logger.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public MatchCallbackSubscriptions(
        Func<string, long, bool> canKeepSubscription,
        ILogger logger)
        : this(canKeepSubscription, null, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="MatchCallbackSubscriptions{TCallback}"/> class.
    /// </summary>
    /// <param name="canKeepSubscription">
    /// Predicate validating whether a subscription remains authorized.
    /// </param>
    /// <param name="subscriptionRemoved">
    /// Optional callback invoked after a subscription is removed.
    /// </param>
    /// <param name="logger">The diagnostic logger.</param>
    /// <exception cref="ArgumentNullException">A required dependency is null.</exception>
    public MatchCallbackSubscriptions(
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
                throw new ObjectDisposedException(
                    nameof(MatchCallbackSubscriptions<TCallback>));
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

    /// <summary>
    /// Checks whether an active subscription exists for a session token and match.
    /// </summary>
    /// <param name="token">The authenticated session token.</param>
    /// <param name="matchId">The match identifier.</param>
    /// <returns><c>true</c> if a subscription exists; otherwise, <c>false</c>.</returns>
    public bool HasSubscription(string token, long matchId)
    {
        lock (_subscriptionsSync)
        {
            return _subscriptions.Any(channel => channel.Token == token
                && channel.MatchId == matchId);
        }
    }

    /// <summary>
    /// Removes all active subscriptions associated with a session token.
    /// </summary>
    /// <param name="token">The closed or revoked session token.</param>
    public void RemoveSession(string token)
    {
        foreach (Subscription channel in Snapshot().Where(item => item.Token == token))
        {
            Remove(channel, true);
        }
    }

    /// <summary>
    /// Queues a callback notification for all active subscriptions of a match.
    /// </summary>
    /// <param name="matchId">The target match identifier.</param>
    /// <param name="send">The callback action receiving session token and channel.</param>
    /// <exception cref="ArgumentNullException"><paramref name="send"/> is null.</exception>
    public void Publish(long matchId, Action<string, TCallback> send)
    {
        if (send == null)
        {
            throw new ArgumentNullException(nameof(send));
        }

        PublishWhere(matchId, (token, _, callback) => send(token, callback));
    }

    internal void PublishAll(Action<string, long, TCallback> send)
    {
        PublishWhere(null, send);
    }

    /// <inheritdoc />
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
            catch (Exception exception) when (exception is CommunicationException
                or TimeoutException
                or ObjectDisposedException)
            {
                Log(LogLevel.Warning, "MatchCallback", exception);
                Remove(channel, true);
            }
            catch (Exception exception)
            {
                Log(LogLevel.Error, "MatchCallback", exception);
            }
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
                Log(LogLevel.Error, "MatchChannelCleanup", exception);
            }
        }
    }

    private void Log(LogLevel level, string operation, Exception exception)
    {
        string correlationId = CorrelationContext.GetOrCreate();
        try
        {
            _logger.Log(
                level,
                exception,
                "The {Operation} background operation failed. CorrelationId: {CorrelationId}",
                operation,
                correlationId);
        }
        catch (Exception loggingFailure)
        {
            string context =
                "The match callback diagnostic logger failed. Operation: "
                + operation
                + ". CorrelationId: "
                + correlationId
                + ".";
            FailureDiagnostics.WriteLoggerFallback(context, exception, loggingFailure);
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
                Log(LogLevel.Error, "MatchCallbackDispatch", exception);
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
