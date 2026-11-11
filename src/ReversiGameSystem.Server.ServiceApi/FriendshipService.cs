using System;
using System.Collections.Generic;
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

/// <summary>
/// Exposes public friendship and authorized presence notifications over WCF duplex channels.
/// </summary>
[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple,
    UseSynchronizationContext = false)]
public sealed class FriendshipService : IFriendshipService, IDisposable
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(5);
    private const int MaximumSweepChannels = 64;
    private readonly FriendshipApplicationService _applicationService;
    private readonly AccountPresenceRegistry _presenceRegistry;
    private readonly FriendshipSubscriptions _subscriptions;
    private readonly ILogger<FriendshipService> _logger;
    private readonly Timer _sweepTimer;
    private int _sweeping;
    private int _sweepOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendshipService"/> class.
    /// </summary>
    /// <param name="applicationService">The friendship application service.</param>
    /// <param name="presenceRegistry">The connected presence registry.</param>
    /// <param name="subscriptions">The active friendship callback subscriptions manager.</param>
    /// <param name="logger">The diagnostic logger.</param>
    /// <exception cref="ArgumentNullException">A required dependency is null.</exception>
    public FriendshipService(
        FriendshipApplicationService applicationService,
        AccountPresenceRegistry presenceRegistry,
        FriendshipSubscriptions subscriptions,
        ILogger<FriendshipService> logger)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _presenceRegistry = presenceRegistry
            ?? throw new ArgumentNullException(nameof(presenceRegistry));
        _subscriptions = subscriptions
            ?? throw new ArgumentNullException(nameof(subscriptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _presenceRegistry.PresenceChanged += OnPresenceChanged;
        _sweepTimer = new Timer(_ => Sweep(MaximumSweepChannels), null, SweepInterval, SweepInterval);
    }

    /// <inheritdoc />
    public void OpenFriendshipChannel(string sessionToken)
    {
        Execute(nameof(OpenFriendshipChannel), () =>
        {
            FriendshipResult<AuthenticatedSession> accessResult =
                _applicationService.EvaluateChannelAccess(sessionToken);
            ThrowFailure(accessResult.Status);

            AuthenticatedSession session = accessResult.Value
                ?? throw new InvalidOperationException(
                    "A successful access evaluation must contain its session.");

            IFriendshipServiceCallback callback = OperationContext.Current
                .GetCallbackChannel<IFriendshipServiceCallback>();

            IDisposable presenceRegistration =
                _presenceRegistry.Register(sessionToken, session.AccountId);

            _subscriptions.Register(
                sessionToken,
                session.AccountId,
                callback,
                presenceRegistration);
        });
    }

    /// <inheritdoc />
    public FriendSummary[] GetFriends(string sessionToken)
    {
        return Execute(nameof(GetFriends), () =>
        {
            FriendshipResult<FriendAccount[]> result =
                _applicationService.ReadConfirmedFriends(sessionToken);
            ThrowFailure(result.Status);

            FriendAccount[] friends = result.Value
                ?? throw new InvalidOperationException(
                    "A successful friends result must contain its value.");

            FriendSummary[] summaries = new FriendSummary[friends.Length];
            for (int index = 0; index < friends.Length; index++)
            {
                FriendAccount friend = friends[index];
                summaries[index] = new FriendSummary
                {
                    Account = new AccountReference
                    {
                        AccountId = friend.AccountId,
                        Username = friend.Username
                    },
                    HasProfileImage = friend.HasProfileImage,
                    IsOnline = _presenceRegistry.IsOnline(friend.AccountId)
                };
            }

            return summaries;
        });
    }

    /// <summary>
    /// Removes an active subscription when a session is closed.
    /// </summary>
    /// <param name="sessionToken">The closed session token.</param>
    public void RemoveSession(string sessionToken)
    {
        _subscriptions.Remove(sessionToken);
    }

    /// <inheritdoc />
    public PlayerSummary[] FindPlayers(string sessionToken, string searchText)
    {
        throw CreateUnsupportedFault(nameof(FindPlayers));
    }

    /// <inheritdoc />
    public FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId)
    {
        throw CreateUnsupportedFault(nameof(SendFriendRequest));
    }

    /// <inheritdoc />
    public FriendRequest[] GetPendingFriendRequests(string sessionToken)
    {
        throw CreateUnsupportedFault(nameof(GetPendingFriendRequests));
    }

    /// <inheritdoc />
    public void RespondToFriendRequest(string sessionToken, string requestId, bool accept)
    {
        throw CreateUnsupportedFault(nameof(RespondToFriendRequest));
    }

    /// <inheritdoc />
    public void RemoveFriend(string sessionToken, long friendAccountId)
    {
        throw CreateUnsupportedFault(nameof(RemoveFriend));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sweepTimer.Dispose();
        _presenceRegistry.PresenceChanged -= OnPresenceChanged;
        _subscriptions.Dispose();
    }

    private void OnPresenceChanged(long accountId, bool isOnline)
    {
        try
        {
            long[] friendIds = _applicationService.ReadConfirmedFriendIds(accountId);
            if (friendIds.Length == 0)
            {
                return;
            }

            HashSet<long> friendIdSet = new HashSet<long>(friendIds);
            FriendshipChannel[] activeChannels = _subscriptions.Snapshot();

            for (int index = 0; index < activeChannels.Length; index++)
            {
                FriendshipChannel subscriber = activeChannels[index];
                if (!friendIdSet.Contains(subscriber.AccountId))
                {
                    continue;
                }

                try
                {
                    subscriber.Callback?.OnFriendChanged(
                        accountId,
                        isOnline ? FriendChange.Connected : FriendChange.Disconnected);
                }
                catch (CommunicationException exception)
                {
                    _subscriptions.Remove(subscriber.Token);
                    LogFailure(nameof(OnPresenceChanged), exception);
                }
                catch (Exception exception)
                {
                    LogFailure(nameof(OnPresenceChanged), exception);
                }
            }
        }
        catch (Exception exception)
        {
            LogFailure(nameof(OnPresenceChanged), exception);
        }
    }

    private void SweepChannels(int maximumChannels)
    {
        FriendshipChannel[] channels = _subscriptions.Snapshot();
        if (channels.Length == 0)
        {
            _sweepOffset = 0;
            return;
        }

        int count = Math.Min(maximumChannels, channels.Length);
        for (int index = 0; index < count; index++)
        {
            FriendshipChannel channel = channels[(_sweepOffset + index) % channels.Length];
            if (!IsValid(channel))
            {
                _subscriptions.Remove(channel.Token);
            }
        }

        _sweepOffset = (_sweepOffset + count) % channels.Length;
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

    private bool IsValid(FriendshipChannel channel)
    {
        return channel.Communication.State == CommunicationState.Opened
            && _applicationService.EvaluateChannelAccess(channel.Token).Status == FriendshipStatus.Success;
    }

    private void Execute(string operation, Action action)
    {
        Execute(operation, () =>
        {
            action();
            return true;
        });
    }

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
        catch (FaultException<FriendshipFault>)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogFailure(operation, exception);
            throw CreateServiceFault(operation);
        }
    }

    private static void ThrowFailure(FriendshipStatus status)
    {
        switch (status)
        {
            case FriendshipStatus.Success:
                return;

            case FriendshipStatus.MissingSessionToken:
                throw SessionFaultFactory.Create(SessionFailure.MissingSessionToken);

            case FriendshipStatus.InvalidSession:
                throw SessionFaultFactory.Create(SessionFailure.InvalidSession);

            case FriendshipStatus.InactiveAccount:
                throw SessionFaultFactory.Create(SessionFailure.InactiveAccount);

            case FriendshipStatus.InvalidAccountId:
                throw CreateFriendshipFault(FriendshipFailure.InvalidAccountId);

            case FriendshipStatus.AccountNotFound:
                throw CreateFriendshipFault(FriendshipFailure.AccountUnavailable);

            case FriendshipStatus.FriendshipNotFound:
                throw CreateFriendshipFault(FriendshipFailure.FriendshipNotFound);

            default:
                throw CreateFriendshipFault(FriendshipFailure.AccountUnavailable);
        }
    }

    private static FaultException<FriendshipFault> CreateFriendshipFault(FriendshipFailure failure)
    {
        return new FaultException<FriendshipFault>(new FriendshipFault
        {
            Reason = failure
        }, "The friendship request was rejected.");
    }

    private static FaultException<ServiceFault> CreateServiceFault(string operation)
    {
        return ServiceFaultFactory.Create(operation, CorrelationContext.GetOrCreate());
    }

    private static FaultException<ServiceFault> CreateUnsupportedFault(string operation)
    {
        return CreateServiceFault(operation);
    }

    private void LogFailure(string operation, Exception exception)
    {
        string correlationId = CorrelationContext.GetOrCreate();
        try
        {
            _logger.LogError(
                exception,
                "The {Operation} friendship operation failed. CorrelationId: {CorrelationId}",
                operation,
                correlationId);
        }
        catch (Exception loggingFailure)
        {
            Console.Error.WriteLine(
                "The friendship diagnostic logger failed. Operation: {0}. CorrelationId: {1}.{2}{3}{2}{4}",
                operation,
                correlationId,
                Environment.NewLine,
                FailureDiagnostics.Describe(exception),
                FailureDiagnostics.Describe(loggingFailure));
        }
    }
}
