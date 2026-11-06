using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Publishes durable human lobby transitions over authenticated duplex channels.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class MatchLobbyService : IMatchLobbyService, IDisposable
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(5);
    private readonly MatchApplicationService _applicationService;
    private readonly ILogger<MatchLobbyService> _logger;
    private readonly MatchChannels<IMatchLobbyServiceCallback> _channels;
    private readonly Timer _expiryTimer;
    private int _sweeping;

    /// <summary>Creates the lobby boundary using the authoritative application handler.</summary>
    public MatchLobbyService(
        MatchApplicationService applicationService,
        ILogger<MatchLobbyService> logger)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _channels = new MatchChannels<IMatchLobbyServiceCallback>(
            AuthorizeChannel, (token, matchId) => Cleanup(token), _logger);
        _expiryTimer = new Timer(_ => SweepExpired(), null, SweepInterval, SweepInterval);
    }

    /// <inheritdoc />
    public void OpenLobbyChannel(string sessionToken)
    {
        ThrowFailure(_applicationService.Authorize(sessionToken));
        _channels.Register(sessionToken, 0);
    }

    /// <inheritdoc />
    public LobbySnapshot CreateMatch(string sessionToken, MatchConfiguration configuration)
    {
        MatchSettings? settings = configuration == null ? null : MapSettings(configuration);
        Match match = GetMatch(_applicationService.Create(sessionToken, settings));
        return MatchContractMapper.Lobby(match);
    }

    /// <inheritdoc />
    public LobbySnapshot InviteToMatch(
        string sessionToken,
        long friendAccountId,
        MatchConfiguration configuration)
    {
        MatchSettings? settings = configuration == null ? null : MapSettings(configuration);
        Match match = GetMatch(_applicationService.Invite(
            sessionToken,
            friendAccountId,
            settings));
        return MatchContractMapper.Lobby(match);
    }

    /// <inheritdoc />
    public AvailableMatch[] GetAvailableMatches(string sessionToken)
    {
        MatchOperationResult<Match[]> result = _applicationService.Available(sessionToken);
        ThrowFailure(result.Status);
        return result.GetValue().Select(MapAvailableMatch).ToArray();
    }

    /// <inheritdoc />
    public LobbySnapshot JoinMatch(string sessionToken, string matchCode)
    {
        Match match = GetMatch(_applicationService.Join(sessionToken, matchCode));
        _channels.Publish(match.Id, (recipientToken, callback) =>
            PublishStarted(recipientToken, callback, match.Id));
        return MatchContractMapper.Lobby(match);
    }

    /// <inheritdoc />
    public LobbySnapshot ChangeTurnDuration(
        string sessionToken,
        long matchId,
        int turnDurationSeconds)
    {
        MatchOperationResult<Match> result = _applicationService.ChangeTurnDuration(
            sessionToken,
            matchId,
            turnDurationSeconds);
        return MatchContractMapper.Lobby(GetMatch(result));
    }

    /// <inheritdoc />
    public MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId)
    {
        throw MatchBoundary.Unsupported(nameof(StartSystemMatch));
    }

    /// <inheritdoc />
    public void CancelMatch(string sessionToken, long matchId)
    {
        ThrowFailure(_applicationService.Cancel(sessionToken, matchId));
        PublishClosed(matchId);
    }

    private bool AuthorizeChannel(string token, long matchId)
    {
        return _applicationService.Authorize(token) == MatchOperationStatus.Success;
    }

    private void PublishStarted(string token, IMatchLobbyServiceCallback callback, long matchId)
    {
        MatchOperationResult<Match> result = _applicationService.Get(token, matchId);
        if (result.Status == MatchOperationStatus.Success)
        {
            callback.OnMatchStarted(MatchContractMapper.InitialState(result.GetValue()));
        }
    }

    private static MatchSettings MapSettings(MatchConfiguration configuration)
    {
        return new MatchSettings
        {
            TurnDurationSeconds = configuration.TurnDurationSeconds,
            CountsTowardStatistics = configuration.CountsTowardStatistics,
            OpponentKind = configuration.OpponentKind.ToString()
        };
    }

    private static AvailableMatch MapAvailableMatch(Match match)
    {
        return new AvailableMatch
        {
            MatchCode = match.Code
                ?? throw new InvalidOperationException("An available match must have a join code."),
            CreatorDisplayName = match.Participants[0].DisplayName,
            TurnDurationSeconds = match.TurnDurationSeconds
        };
    }

    /// <summary>Removes a revoked session and cancels its waiting lobbies.</summary>
    public void RemoveSession(string sessionToken)
    {
        _channels.RemoveSession(sessionToken);
        Cleanup(sessionToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _expiryTimer.Dispose();
        _channels.Dispose();
    }

    private void SweepExpired()
    {
        if (Interlocked.Exchange(ref _sweeping, 1) != 0)
        {
            return;
        }
        try
        {
            foreach (long id in _applicationService.CancelExpiredWaitingSessions())
            {
                PublishClosed(id);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The {Operation} background operation failed. CorrelationId: {CorrelationId}",
                nameof(SweepExpired),
                CorrelationContext.GetOrCreate());
        }
        finally
        {
            Volatile.Write(ref _sweeping, 0);
        }
    }

    private void Cleanup(string token)
    {
        foreach (long id in _applicationService.CancelWaitingForSession(token))
        {
            PublishClosed(id);
        }
    }

    private void PublishClosed(long id)
    {
        _channels.Publish(id, (token, callback) => PublishClosedToParticipant(token, callback, id));
    }

    private void PublishClosedToParticipant(
        string token,
        IMatchLobbyServiceCallback callback,
        long matchId)
    {
        MatchOperationResult<Match> result = _applicationService.Get(token, matchId);
        if (result.Status == MatchOperationStatus.Success)
        {
            callback.OnLobbyClosed(matchId);
        }
    }

    private static Match GetMatch(MatchOperationResult<Match> result)
    {
        ThrowFailure(result.Status);
        return result.GetValue();
    }

    private static void ThrowFailure(MatchOperationStatus status)
    {
        if (status == MatchOperationStatus.Success)
        {
            return;
        }
        if (status == MatchOperationStatus.MissingSessionToken)
        {
            throw SessionFaultFactory.Create(SessionFailure.MissingSessionToken);
        }
        if (status == MatchOperationStatus.InvalidSession)
        {
            throw SessionFaultFactory.Create(SessionFailure.InvalidSession);
        }
        if (status == MatchOperationStatus.InvalidFriendAccountId)
        {
            throw CreateFriendshipFault(FriendshipFailure.InvalidAccountId);
        }
        if (status == MatchOperationStatus.FriendAccountUnavailable)
        {
            throw CreateFriendshipFault(FriendshipFailure.AccountUnavailable);
        }
        if (status == MatchOperationStatus.FriendshipNotFound)
        {
            throw CreateFriendshipFault(FriendshipFailure.FriendshipNotFound);
        }

        MatchFailure failure = GetMatchFailure(status);
        throw new FaultException<MatchFault>(new MatchFault
        {
            Reason = failure
        }, "The match request was rejected.");
    }

    private static FaultException<FriendshipFault> CreateFriendshipFault(
        FriendshipFailure failure)
    {
        return new FaultException<FriendshipFault>(new FriendshipFault
        {
            Reason = failure
        }, "The friendship request was rejected.");
    }

    private static MatchFailure GetMatchFailure(MatchOperationStatus status)
    {
        MatchFailure failure = MatchFailure.Unauthorized;
        if (status == MatchOperationStatus.InactiveAccount)
        {
            failure = MatchFailure.InactiveAccount;
        }
        else if (status == MatchOperationStatus.InvalidMatchId)
        {
            failure = MatchFailure.InvalidMatchId;
        }
        else if (status == MatchOperationStatus.InvalidConfiguration)
        {
            failure = MatchFailure.InvalidConfiguration;
        }
        else if (status == MatchOperationStatus.InvalidTurnDuration)
        {
            failure = MatchFailure.InvalidTurnDuration;
        }
        else if (status == MatchOperationStatus.MatchNotFound)
        {
            failure = MatchFailure.MatchNotFound;
        }
        else if (status == MatchOperationStatus.CannotJoinOwnMatch)
        {
            failure = MatchFailure.CannotJoinOwnMatch;
        }
        else if (status == MatchOperationStatus.InvalidMatchCode)
        {
            failure = MatchFailure.InvalidMatchCode;
        }
        else if (status == MatchOperationStatus.MatchCodeUnavailable)
        {
            failure = MatchFailure.MatchCodeUnavailable;
        }
        else if (status == MatchOperationStatus.MatchNotWaiting)
        {
            failure = MatchFailure.MatchNotWaiting;
        }
        else if (status == MatchOperationStatus.MatchNotInProgress)
        {
            failure = MatchFailure.MatchNotInProgress;
        }

        return failure;
    }
}
