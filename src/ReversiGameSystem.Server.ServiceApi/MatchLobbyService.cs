using System;
using System.Linq;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Publishes durable human lobby transitions over authenticated duplex channels.</summary>
[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple,
    UseSynchronizationContext = false)]
public sealed class MatchLobbyService : IMatchLobbyService
{
    private readonly MatchApplicationService _applicationService;
    private readonly MatchCallbackSubscriptions<IMatchLobbyServiceCallback>
        _callbackSubscriptions;

    /// <summary>Creates the lobby boundary using the authoritative application handler.</summary>
    /// <param name="applicationService">The lobby application service.</param>
    /// <param name="callbackSubscriptions">The lobby duplex callback subscriptions.</param>
    /// <exception cref="ArgumentNullException">A required dependency is null.</exception>
    public MatchLobbyService(
        MatchApplicationService applicationService,
        MatchCallbackSubscriptions<IMatchLobbyServiceCallback> callbackSubscriptions)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _callbackSubscriptions = callbackSubscriptions
            ?? throw new ArgumentNullException(nameof(callbackSubscriptions));
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
        Match match = RequireValue(_applicationService.CreateMatch(sessionToken, settings));
        return MatchContractMapper.MapLobby(match);
    }

    /// <inheritdoc />
    public LobbySnapshot InviteToMatch(
        string sessionToken,
        long friendAccountId,
        MatchConfiguration configuration)
    {
        MatchSettings? settings = configuration == null ? null : MapSettings(configuration);
        Match match = RequireValue(_applicationService.InviteToMatch(
            sessionToken,
            friendAccountId,
            settings));
        return MatchContractMapper.MapLobby(match);
    }

    /// <inheritdoc />
    public AvailableMatch[] GetAvailableMatches(string sessionToken)
    {
        Match[] matches = RequireValue(_applicationService.GetAvailableMatches(sessionToken));
        return matches.Select(MapAvailableMatch).ToArray();
    }

    /// <inheritdoc />
    public LobbySnapshot JoinMatch(string sessionToken, string matchCode)
    {
        Match match = RequireValue(_applicationService.JoinMatch(sessionToken, matchCode));
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
        return MatchContractMapper.MapLobby(RequireValue(result));
    }

    /// <inheritdoc />
    public MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId)
    {
        Match match = RequireValue(
            _applicationService.StartSystemMatch(sessionToken, matchId));
        return MatchContractMapper.MapState(match);
    }

    /// <inheritdoc />
    public void CancelMatch(string sessionToken, long matchId)
    {
        ThrowFaultIfFailed(_applicationService.CancelMatch(sessionToken, matchId));
        _callbackSubscriptions.Publish(
            matchId,
            (token, callback) => PublishClosedToParticipant(token, callback, matchId));
    }

    private void PublishMatchStarted(
        string token,
        IMatchLobbyServiceCallback callback,
        long matchId)
    {
        MatchResult<Match> result = _applicationService.GetParticipantMatch(token, matchId);
        if (result.Status == MatchStatus.Success)
        {
            Match match = RequireValue(result);
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
                ?? throw new InvalidOperationException(
                    "An available match must have a join code."),
            CreatorDisplayName = match.Participants[0].DisplayName,
            TurnDurationSeconds = match.TurnDurationSeconds
        };
        return available;
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

    private static T RequireValue<T>(MatchResult<T> result)
        where T : class
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
            throw ServiceFaultFactory.CreateSessionFault(SessionFailure.MissingSessionToken);
        }
        if (status == MatchStatus.InvalidSession)
        {
            throw ServiceFaultFactory.CreateSessionFault(SessionFailure.InvalidSession);
        }
        if (status == MatchStatus.InvalidFriendAccountId)
        {
            throw ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.InvalidAccountId);
        }
        if (status == MatchStatus.FriendAccountUnavailable)
        {
            throw ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.AccountUnavailable);
        }
        if (status == MatchStatus.FriendshipNotFound)
        {
            throw ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.FriendshipNotFound);
        }

        throw ServiceFaultFactory.CreateMatchFault(MapMatchFailure(status));
    }

    private static MatchFailure MapMatchFailure(MatchStatus status)
    {
        MatchFailure failure = status switch
        {
            MatchStatus.InactiveAccount => MatchFailure.InactiveAccount,
            MatchStatus.InvalidMatchId => MatchFailure.InvalidMatchId,
            MatchStatus.InvalidConfiguration => MatchFailure.InvalidConfiguration,
            MatchStatus.InvalidTurnDuration => MatchFailure.InvalidTurnDuration,
            MatchStatus.MatchNotFound => MatchFailure.MatchNotFound,
            MatchStatus.CannotJoinOwnMatch => MatchFailure.CannotJoinOwnMatch,
            MatchStatus.InvalidMatchCode => MatchFailure.InvalidMatchCode,
            MatchStatus.MatchCodeUnavailable => MatchFailure.MatchCodeUnavailable,
            MatchStatus.MatchNotWaiting => MatchFailure.MatchNotWaiting,
            MatchStatus.MatchNotInProgress => MatchFailure.MatchNotInProgress,
            MatchStatus.Unauthorized => MatchFailure.Unauthorized,
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unsupported match status.")
        };

        return failure;
    }
}
