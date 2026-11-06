using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Publishes authenticated friend presence over live duplex channels.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class FriendshipService : IFriendshipService, IDisposable
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(5);
    private const int MaximumSweepChannels = 64;
    private readonly object _gate = new object();
    private readonly Dictionary<string, FriendshipChannel> _channels =
        new Dictionary<string, FriendshipChannel>(StringComparer.Ordinal);
    private readonly Queue<long> _changes = new Queue<long>();
    private readonly HashSet<long> _published = new HashSet<long>();
    private readonly ISessionRepository _sessions;
    private readonly IFriendshipRepository _friends;
    private readonly ILogger<FriendshipService> _logger;
    private readonly Timer _timer;
    private bool _dispatching;
    private bool _disposed;
    private int _sweeping;
    private int _sweepOffset;

    /// <summary>Creates the shared friendship boundary and expiry sweep.</summary>
    /// <param name="sessions">The authoritative authenticated sessions.</param>
    /// <param name="friends">The current active friendships.</param>
    /// <param name="logger">The sanitized diagnostic logger.</param>
    public FriendshipService(ISessionRepository sessions,
        IFriendshipRepository friends, ILogger<FriendshipService> logger)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _friends = friends ?? throw new ArgumentNullException(nameof(friends));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timer = new Timer(_ => Sweep(MaximumSweepChannels), null, SweepInterval, SweepInterval);
    }

    /// <inheritdoc />
    public void OpenFriendshipChannel(string sessionToken) =>
        Execute(nameof(OpenFriendshipChannel), () => OpenChannel(sessionToken));

    /// <inheritdoc />
    public FriendSummary[] GetFriends(string sessionToken) =>
        Execute(nameof(GetFriends), () => ReadFriends(sessionToken));

    private void OpenChannel(string sessionToken)
    {
        long accountId = RequireCaller(sessionToken);
        IFriendshipServiceCallback callback = OperationContext.Current
            .GetCallbackChannel<IFriendshipServiceCallback>();
        FriendshipChannel channel = new FriendshipChannel(sessionToken, accountId, callback);
        channel.Terminated = (_, _) => RemoveChannel(channel);
        channel.Communication.Closed += channel.Terminated;
        channel.Communication.Faulted += channel.Terminated;
        RegisterChannel(channel);
        if (!IsValid(channel))
        {
            RemoveChannel(channel);
            throw SessionFault(SessionFailure.InvalidSession);
        }
    }

    private FriendSummary[] ReadFriends(string sessionToken)
    {
        long accountId = RequireCaller(sessionToken);
        FriendAccount[] friends = _friends.GetFriends(accountId);
        Sweep();
        RequireCaller(sessionToken);
        lock (_gate)
        {
            ThrowIfDisposed();
            return friends.Select(CreateFriendSummary).ToArray();
        }
    }

    // ReadFriends holds the bookkeeping lock while mapping channel presence.
    private FriendSummary CreateFriendSummary(FriendAccount friend) => new FriendSummary
    {
        Account = new AccountReference
        {
            AccountId = friend.AccountId,
            Username = friend.Username
        },
        HasProfileImage = friend.HasProfileImage,
        IsOnline = _channels.Values.Any(channel => channel.AccountId == friend.AccountId)
    };

    /// <summary>Removes a revoked session and reports cleanup failures.</summary>
    /// <param name="sessionToken">The revoked opaque token.</param>
    public void RemoveSession(string sessionToken)
    {
        try
        {
            FriendshipChannel? channel;
            lock (_gate)
            {
                _channels.TryGetValue(sessionToken ?? string.Empty, out channel);
            }

            if (channel != null)
            {
                RemoveChannel(channel);
            }
        }
        catch (Exception exception)
        {
            LogFailure(nameof(RemoveSession), exception);
        }
    }

    /// <inheritdoc />
    public PlayerSummary[] FindPlayers(string sessionToken, string searchText) =>
        throw Unsupported(nameof(FindPlayers));

    /// <inheritdoc />
    public FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId) =>
        throw Unsupported(nameof(SendFriendRequest));

    /// <inheritdoc />
    public FriendRequest[] GetPendingFriendRequests(string sessionToken) =>
        throw Unsupported(nameof(GetPendingFriendRequests));

    /// <inheritdoc />
    public void RespondToFriendRequest(string sessionToken, string requestId, bool accept) =>
        throw Unsupported(nameof(RespondToFriendRequest));

    /// <inheritdoc />
    public void RemoveFriend(string sessionToken, long friendAccountId) =>
        throw Unsupported(nameof(RemoveFriend));

    /// <inheritdoc />
    public void Dispose()
    {
        FriendshipChannel[] channels;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            channels = _channels.Values.ToArray();
            _channels.Clear();
            _changes.Clear();
            _published.Clear();
        }

        _timer.Dispose();
        foreach (FriendshipChannel channel in channels)
        {
            AbortChannel(channel);
        }
    }

    private long RequireCaller(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw SessionFault(SessionFailure.MissingSessionToken);
        }

        if (!_sessions.TryGet(token, out AuthenticatedSession? session) || session == null)
        {
            throw SessionFault(SessionFailure.InvalidSession);
        }

        if (!_friends.IsAccountActive(session.AccountId))
        {
            RemoveSession(token);
            throw SessionFault(SessionFailure.InactiveAccount);
        }

        return session.AccountId;
    }

    private bool IsValid(FriendshipChannel channel) =>
        channel.Communication.State == CommunicationState.Opened
        && _sessions.TryGet(channel.Token, out AuthenticatedSession? session)
        && session != null && session.AccountId == channel.AccountId
        && _friends.IsAccountActive(channel.AccountId);

    private void RegisterChannel(FriendshipChannel channel)
    {
        FriendshipChannel? replaced = null;
        try
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                _channels.TryGetValue(channel.Token, out replaced);
                _channels[channel.Token] = channel;
                QueueChange(channel.AccountId);
            }
        }
        catch
        {
            channel.Detach();
            throw;
        }

        if (replaced != null)
        {
            AbortChannel(replaced);
        }
    }

    private void AbortChannel(FriendshipChannel channel)
    {
        channel.Detach();
        try
        {
            channel.Communication.Abort();
        }
        catch (Exception exception)
        {
            LogFailure(nameof(AbortChannel), exception);
        }
    }

    private void SweepChannels(int maximumChannels)
    {
        FriendshipChannel[] channels = Snapshot();
        int count = Math.Min(maximumChannels, channels.Length);
        for (int index = 0; index < count; index++)
        {
            FriendshipChannel channel = channels[(_sweepOffset + index) % channels.Length];
            if (!IsValid(channel))
            {
                RemoveChannel(channel);
            }
        }

        _sweepOffset = channels.Length == 0 ? 0 : (_sweepOffset + count) % channels.Length;
    }

    private void Sweep(int maximumChannels = int.MaxValue)
    {
        if (Interlocked.Exchange(ref _sweeping, 1) != 0)
        {
            return;
        }

        try
        {
            SweepChannels(maximumChannels);
        }
        catch (Exception exception)
        {
            LogFailure(nameof(Sweep), exception);
        }
        finally
        {
            Volatile.Write(ref _sweeping, 0);
        }
    }

    private FriendshipChannel[] Snapshot()
    {
        lock (_gate)
        {
            return _channels.Values.ToArray();
        }
    }

    private void RemoveChannel(FriendshipChannel channel)
    {
        lock (_gate)
        {
            if (_channels.TryGetValue(channel.Token, out FriendshipChannel? current)
                && ReferenceEquals(current, channel))
            {
                _channels.Remove(channel.Token);
                if (!_disposed)
                {
                    QueueChange(channel.AccountId);
                }
            }
        }

        channel.Detach();
    }

    // The gate protects bookkeeping only. Database access and remote calls run outside it.
    private void QueueChange(long accountId)
    {
        _changes.Enqueue(accountId);
        if (!_dispatching)
        {
            _dispatching = true;
            ThreadPool.QueueUserWorkItem(_ => Dispatch());
        }
    }

    private void Dispatch()
    {
        while (true)
        {
            long accountId;
            lock (_gate)
            {
                if (_disposed || _changes.Count == 0)
                {
                    _dispatching = false;
                    return;
                }

                accountId = _changes.Dequeue();
            }

            try
            {
                Publish(accountId);
            }
            catch (Exception exception)
            {
                LogFailure(nameof(Dispatch), exception);
            }
        }
    }

    private void Publish(long accountId)
    {
        Sweep();
        if (!TryCaptureChange(accountId, out bool online))
        {
            return;
        }

        foreach (FriendshipChannel recipient in Snapshot())
        {
            try
            {
                SendChange(recipient, accountId, online);
            }
            catch (CommunicationException exception)
            {
                DisconnectFailedRecipient(recipient, exception);
            }
            catch (TimeoutException exception)
            {
                DisconnectFailedRecipient(recipient, exception);
            }
            catch (Exception exception)
            {
                LogFailure(nameof(Publish), exception);
            }
        }
    }

    private void DisconnectFailedRecipient(FriendshipChannel recipient, Exception exception)
    {
        RemoveChannel(recipient);
        AbortChannel(recipient);
        LogFailure(nameof(Publish), exception);
    }

    private void Execute(string operation, Action action) => Execute(operation, () =>
    {
        action();
        return true;
    });

    private TResult Execute<TResult>(string operation, Func<TResult> action)
    {
        try
        {
            return action();
        }
        catch (FaultException<SessionFault>)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogFailure(operation, exception);
            throw CreateServiceFault(operation);
        }
    }

    private bool TryCaptureChange(long accountId, out bool online)
    {
        lock (_gate)
        {
            online = _channels.Values.Any(channel => channel.AccountId == accountId);
            if (online == _published.Contains(accountId))
            {
                return false;
            }

            if (online)
            {
                _published.Add(accountId);
            }
            else
            {
                _published.Remove(accountId);
            }

            return true;
        }
    }

    private void SendChange(FriendshipChannel recipient, long accountId, bool online)
    {
        if (!IsValid(recipient))
        {
            RemoveChannel(recipient);
            return;
        }

        if (!_friends.AreFriends(recipient.AccountId, accountId) || !IsCurrentChannel(recipient))
        {
            return;
        }

        // Validate again after the relationship query, immediately before sending.
        if (!IsValid(recipient))
        {
            RemoveChannel(recipient);
            return;
        }

        recipient.Callback.OnFriendChanged(accountId,
            online ? FriendChange.Connected : FriendChange.Disconnected);
    }

    private bool IsCurrentChannel(FriendshipChannel channel)
    {
        lock (_gate)
        {
            return !_disposed
                && _channels.TryGetValue(channel.Token, out FriendshipChannel? current)
                && ReferenceEquals(current, channel);
        }
    }

    private static FaultException<SessionFault> SessionFault(SessionFailure reason) =>
        SessionFaultFactory.Create(reason);

    private static FaultException<ServiceFault> CreateServiceFault(string operation) =>
        ServiceFaultFactory.Create(operation, CorrelationContext.GetOrCreate());

    private static FaultException<ServiceFault> Unsupported(string operation) =>
        CreateServiceFault(operation);

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FriendshipService));
        }
    }

    private void LogFailure(string operation, Exception exception)
    {
        string correlationId = CorrelationContext.GetOrCreate();
        try
        {
            _logger.LogError(exception,
                "The {Operation} friendship operation failed. CorrelationId: {CorrelationId}",
                operation, correlationId);
        }
        catch (Exception loggingFailure)
        {
            // Preserve revocation and report both failures through an independent safe sink.
            Console.Error.WriteLine(
                "The friendship diagnostic logger failed. Operation: {0}. CorrelationId: {1}.{2}{3}{2}{4}",
                operation, correlationId, Environment.NewLine,
                FailureDiagnostics.Describe(exception),
                FailureDiagnostics.Describe(loggingFailure));
        }
    }
}
