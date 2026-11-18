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
            AuthorizeChannel, (token, matchId) => CancelWaitingLobbiesForSession(token), _logger);
        _expiryTimer = new Timer(_ => SweepExpiredLobbies(), null, SweepInterval, SweepInterval);
    }

    /// <inheritdoc />
    public void OpenLobbyChannel(string sessionToken)
    {
        ThrowFaultIfFailed(_applicationService.EvaluateLobbyAccess(sessionToken));
        _callbackSubscriptions.RegisterForAllMatches(sessionToken);
    }

    /// <inheritdoc />
    public LobbySnapshot CreateMatch(string sessionToken, MatchConfiguration configuration)
    {
        MatchSettings? settings = configuration == null ? null : MapSettings(configuration);
        Match match = RequireMatchValue(_applicationService.CreateMatch(sessionToken, settings));
        return MatchContractMapper.MapLobby(match);
    }

    /// <inheritdoc />
    public LobbySnapshot InviteToMatch(
        string sessionToken,
        long friendAccountId,
        MatchConfiguration configuration)
    {
        MatchSettings? settings = configuration == null ? null : MapSettings(configuration);
        Match match = RequireMatchValue(_applicationService.InviteToMatch(
            sessionToken,
            friendAccountId,
            settings));
        return MatchContractMapper.MapLobby(match);
    }

    /// <inheritdoc />
    public AvailableMatch[] GetAvailableMatches(string sessionToken)
    {
        MatchResult<Match[]> result = _applicationService.GetAvailableMatches(sessionToken);
        ThrowFaultIfFailed(result.Status);
        Match[] matches = result.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        return matches.Select(MapAvailableMatch).ToArray();
    }

    /// <inheritdoc />
    public LobbySnapshot JoinMatch(string sessionToken, string matchCode)
    {
        Match match = RequireMatchValue(_applicationService.JoinMatch(sessionToken, matchCode));
        _callbackSubscriptions.Publish(match.Id, (recipientToken, callback) =>
            PublishMatchStarted(recipientToken, callback, match.Id));
        return MatchContractMapper.MapLobby(match);
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
        return MatchContractMapper.MapLobby(RequireMatchValue(result));
    }

    /// <inheritdoc />
    public MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId)
    {
        throw ServiceFaultFactory.Create(nameof(StartSystemMatch), CorrelationContext.GetOrCreate());
    }

    /// <inheritdoc />
    public void CancelMatch(string sessionToken, long matchId)
    {
        ThrowFaultIfFailed(_applicationService.CancelMatch(sessionToken, matchId));
        PublishLobbyClosed(matchId);
    }

    private bool AuthorizeChannel(string token, long matchId)
    {
        return _applicationService.EvaluateLobbyAccess(token) == MatchStatus.Success;
    }

    private void PublishMatchStarted(string token, IMatchLobbyServiceCallback callback, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetParticipantMatch(token, matchId);
        if (result.Status == MatchStatus.Success)
        {
            Match match = result.Value
                ?? throw new InvalidOperationException(
                    "A successful match result must contain its value.");
            callback.OnMatchStarted(MatchContractMapper.MapInitialState(match));
        }
    }

    private static MatchSettings MapSettings(MatchConfiguration configuration)
    {
        MatchSettings settings = new MatchSettings
        {
            TurnDurationSeconds = configuration.TurnDurationSeconds,
            CountsTowardStatistics = configuration.CountsTowardStatistics,
            OpponentKind = configuration.OpponentKind.ToString()
        };
        return settings;
    }

    private static AvailableMatch MapAvailableMatch(Match match)
    {
        AvailableMatch available = new AvailableMatch
        {
            MatchCode = match.Code
                ?? throw new InvalidOperationException("An available match must have a join code."),
            CreatorDisplayName = match.Participants[0].DisplayName,
            TurnDurationSeconds = match.TurnDurationSeconds
        };
        return available;
    }

    /// <summary>Removes a revoked session and cancels its waiting lobbies.</summary>
    public void RemoveSession(string sessionToken)
    {
        _callbackSubscriptions.RemoveSession(sessionToken);
        CancelWaitingLobbiesForSession(sessionToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _expiryTimer.Dispose();
        _callbackSubscriptions.Dispose();
    }

    private void SweepExpiredLobbies()
    {
        if (Interlocked.Exchange(ref _sweeping, 1) != 0)
        {
            return;
        }
        try
        {
            foreach (long id in _applicationService.CancelExpiredWaitingSessions())
            {
                PublishLobbyClosed(id);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The {Operation} background operation failed. CorrelationId: {CorrelationId}",
                nameof(SweepExpiredLobbies),
                CorrelationContext.GetOrCreate());
        }
        finally
        {
            Volatile.Write(ref _sweeping, 0);
        }
    }

    private void CancelWaitingLobbiesForSession(string token)
    {
        foreach (long id in _applicationService.CancelWaitingForSession(token))
        {
            PublishLobbyClosed(id);
        }
    }

    private void PublishLobbyClosed(long id)
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
        MatchResult<Match> result = _applicationService.GetParticipantMatch(token, matchId);
        if (result.Status == MatchStatus.Success)
        {
            callback.OnLobbyClosed(matchId);
        }
    }

    private static Match RequireMatchValue(MatchResult<Match> result)
    {
        ThrowFaultIfFailed(result.Status);
        return result.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
    }

    private static void ThrowFaultIfFailed(MatchStatus status)
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

        MatchFailure failure = MapMatchFailure(status);
        MatchFault detail = new MatchFault
        {
            Reason = failure
        };
        throw new FaultException<MatchFault>(detail, "The match request was rejected.");
    }

    private static FaultException<FriendshipFault> CreateFriendshipFault(
        FriendshipFailure failure)
    {
        FriendshipFault detail = new FriendshipFault
        {
            Reason = failure
        };
        FaultException<FriendshipFault> fault = new FaultException<FriendshipFault>(
            detail,
            "The friendship request was rejected.");
        return fault;
    }

    private static MatchFailure MapMatchFailure(MatchStatus status)
    {
        switch (status)
        {
            case MatchStatus.InactiveAccount:
                return MatchFailure.InactiveAccount;
            case MatchStatus.InvalidMatchId:
                return MatchFailure.InvalidMatchId;
            case MatchStatus.InvalidConfiguration:
                return MatchFailure.InvalidConfiguration;
            case MatchStatus.InvalidTurnDuration:
                return MatchFailure.InvalidTurnDuration;
            case MatchStatus.MatchNotFound:
                return MatchFailure.MatchNotFound;
            case MatchStatus.CannotJoinOwnMatch:
                return MatchFailure.CannotJoinOwnMatch;
            case MatchStatus.InvalidMatchCode:
                return MatchFailure.InvalidMatchCode;
            case MatchStatus.MatchCodeUnavailable:
                return MatchFailure.MatchCodeUnavailable;
            case MatchStatus.MatchNotWaiting:
                return MatchFailure.MatchNotWaiting;
            case MatchStatus.MatchNotInProgress:
                return MatchFailure.MatchNotInProgress;
            default:
                return MatchFailure.Unauthorized;
        }
    }
}
