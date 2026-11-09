using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
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
    private readonly MatchCallbackSubscriptions<IMatchLobbyServiceCallback>
        _callbackSubscriptions;
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
        _callbackSubscriptions = new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
            AuthorizeChannel, (token, matchId) => Cleanup(token), _logger);
        _expiryTimer = new Timer(_ => SweepExpired(), null, SweepInterval, SweepInterval);
    }

    /// <inheritdoc />
    public void OpenLobbyChannel(string sessionToken)
    {
        ThrowFailure(_applicationService.EvaluateLobbyAccess(sessionToken));
        _callbackSubscriptions.RegisterForAllMatches(sessionToken);
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
        MatchResult<Match[]> result = _applicationService.Available(sessionToken);
        ThrowFailure(result.Status);
        Match[] matches = result.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        return matches.Select(MapAvailableMatch).ToArray();
    }

    /// <inheritdoc />
    public LobbySnapshot JoinMatch(string sessionToken, string matchCode)
    {
        Match match = GetMatch(_applicationService.Join(sessionToken, matchCode));
        _callbackSubscriptions.Publish(match.Id, (recipientToken, callback) =>
            PublishStarted(recipientToken, callback, match.Id));
        return MatchContractMapper.Lobby(match);
    }

    /// <inheritdoc />
    public LobbySnapshot ChangeTurnDuration(
        string sessionToken,
        long matchId,
        int turnDurationSeconds)
    {
        MatchResult<Match> result = _applicationService.ChangeTurnDuration(
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
        return _applicationService.EvaluateLobbyAccess(token) == MatchStatus.Success;
    }

    private void PublishStarted(string token, IMatchLobbyServiceCallback callback, long matchId)
    {
        MatchResult<Match> result = _applicationService.Get(token, matchId);
        if (result.Status == MatchStatus.Success)
        {
            Match match = result.Value
                ?? throw new InvalidOperationException(
                    "A successful match result must contain its value.");
            callback.OnMatchStarted(MatchContractMapper.InitialState(match));
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
        _callbackSubscriptions.RemoveSession(sessionToken);
        Cleanup(sessionToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _expiryTimer.Dispose();
        _callbackSubscriptions.Dispose();
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
        _callbackSubscriptions.Publish(
            id,
            (token, callback) => PublishClosedToParticipant(token, callback, id));
    }

    private void PublishClosedToParticipant(
        string token,
        IMatchLobbyServiceCallback callback,
        long matchId)
    {
        MatchResult<Match> result = _applicationService.Get(token, matchId);
        if (result.Status == MatchStatus.Success)
        {
            callback.OnLobbyClosed(matchId);
        }
    }

    private static Match GetMatch(MatchResult<Match> result)
    {
        ThrowFailure(result.Status);
        return result.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
    }

    private static void ThrowFailure(MatchStatus status)
    {
        if (status == MatchStatus.Success)
        {
            return;
        }
        if (status == MatchStatus.MissingSessionToken)
        {
            throw SessionFaultFactory.Create(SessionFailure.MissingSessionToken);
        }
        if (status == MatchStatus.InvalidSession)
        {
            throw SessionFaultFactory.Create(SessionFailure.InvalidSession);
        }
        if (status == MatchStatus.InvalidFriendAccountId)
        {
            throw CreateFriendshipFault(FriendshipFailure.InvalidAccountId);
        }
        if (status == MatchStatus.FriendAccountUnavailable)
        {
            throw CreateFriendshipFault(FriendshipFailure.AccountUnavailable);
        }
        if (status == MatchStatus.FriendshipNotFound)
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

    private static MatchFailure GetMatchFailure(MatchStatus status)
    {
        MatchFailure failure = MatchFailure.Unauthorized;
        if (status == MatchStatus.InactiveAccount)
        {
            failure = MatchFailure.InactiveAccount;
        }
        else if (status == MatchStatus.InvalidMatchId)
        {
            failure = MatchFailure.InvalidMatchId;
        }
        else if (status == MatchStatus.InvalidConfiguration)
        {
            failure = MatchFailure.InvalidConfiguration;
        }
        else if (status == MatchStatus.InvalidTurnDuration)
        {
            failure = MatchFailure.InvalidTurnDuration;
        }
        else if (status == MatchStatus.MatchNotFound)
        {
            failure = MatchFailure.MatchNotFound;
        }
        else if (status == MatchStatus.CannotJoinOwnMatch)
        {
            failure = MatchFailure.CannotJoinOwnMatch;
        }
        else if (status == MatchStatus.InvalidMatchCode)
        {
            failure = MatchFailure.InvalidMatchCode;
        }
        else if (status == MatchStatus.MatchCodeUnavailable)
        {
            failure = MatchFailure.MatchCodeUnavailable;
        }
        else if (status == MatchStatus.MatchNotWaiting)
        {
            failure = MatchFailure.MatchNotWaiting;
        }
        else if (status == MatchStatus.MatchNotInProgress)
        {
            failure = MatchFailure.MatchNotInProgress;
        }

        return failure;
    }
}
