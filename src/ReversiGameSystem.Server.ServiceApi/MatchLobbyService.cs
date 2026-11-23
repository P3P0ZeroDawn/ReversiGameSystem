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

    /// <summary>
    /// Authorizes the caller and opens the duplex callback channel for lobby notifications.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public void OpenLobbyChannel(string sessionToken)
    {
        ThrowFaultIfFailed(_applicationService.EvaluateLobbyAccess(sessionToken));
        _callbackSubscriptions.RegisterForAllMatches(sessionToken);
    }

    /// <summary>
    /// Creates a new waiting match lobby with the requested configuration.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="configuration">The desired match rules and turn duration.</param>
    /// <returns>The created waiting lobby snapshot.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public LobbySnapshot CreateMatch(string sessionToken, MatchConfiguration configuration)
    {
        MatchSettings? settings = configuration == null ? null : MapSettings(configuration);
        Match match = RequireValue(_applicationService.CreateMatch(sessionToken, settings));
        return MatchContractMapper.MapLobby(match);
    }

    /// <summary>
    /// Creates a private match lobby and emails an invitation code to a confirmed friend.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="friendAccountId">The invited friend's account identifier.</param>
    /// <param name="configuration">The desired match rules and turn duration.</param>
    /// <returns>The created private lobby snapshot.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{FriendshipFault}">
    /// A declared friendship rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
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

    /// <summary>
    /// Lists public waiting matches available for the caller to join.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <returns>The available waiting matches.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public AvailableMatch[] GetAvailableMatches(string sessionToken)
    {
        Match[] matches = RequireValue(_applicationService.GetAvailableMatches(sessionToken));
        return matches.Select(MapAvailableMatch).ToArray();
    }

    /// <summary>
    /// Joins a waiting match by join code and notifies participants that the match has started.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="matchCode">The match join code.</param>
    /// <returns>The updated lobby snapshot.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public LobbySnapshot JoinMatch(string sessionToken, string matchCode)
    {
        Match match = RequireValue(_applicationService.JoinMatch(sessionToken, matchCode));
        _callbackSubscriptions.Publish(match.Id, (recipientToken, callback) =>
            PublishMatchStarted(recipientToken, callback, match.Id));
        return MatchContractMapper.MapLobby(match);
    }

    /// <summary>
    /// Updates the turn duration of a waiting match created by the caller.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="matchId">The persistent match identifier.</param>
    /// <param name="turnDurationSeconds">The proposed turn duration in seconds.</param>
    /// <returns>The updated lobby snapshot.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
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

    /// <summary>
    /// Starts a waiting match against the system opponent.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="matchId">The persistent match identifier.</param>
    /// <returns>The initial match state snapshot.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId)
    {
        Match match = RequireValue(
            _applicationService.StartSystemMatch(sessionToken, matchId));
        return MatchContractMapper.MapState(match);
    }

    /// <summary>
    /// Cancels a waiting match created by the caller and notifies subscribed participants.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="matchId">The persistent match identifier.</param>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
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
