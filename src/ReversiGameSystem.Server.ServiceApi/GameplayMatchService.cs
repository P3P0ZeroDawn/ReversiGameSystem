using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Publishes authoritative active match gameplay and handles client placement requests.
/// </summary>
[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple,
    UseSynchronizationContext = false)]
public sealed class GameplayMatchService : IGameplayMatchService
{
    private readonly GameplayMatchApplicationService _applicationService;
    private readonly GameplayMatchPresence _presence;
    private readonly MatchCallbackSubscriptions<IGameplayMatchServiceCallback>
        _callbackSubscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameplayMatchService"/> class.
    /// </summary>
    /// <param name="applicationService">The gameplay application service.</param>
    /// <param name="presence">The active match participant presence tracker.</param>
    /// <param name="callbackSubscriptions">The gameplay duplex callback subscriptions.</param>
    /// <exception cref="ArgumentNullException">A required dependency is null.</exception>
    public GameplayMatchService(
        GameplayMatchApplicationService applicationService,
        GameplayMatchPresence presence,
        MatchCallbackSubscriptions<IGameplayMatchServiceCallback> callbackSubscriptions)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _presence = presence ?? throw new ArgumentNullException(nameof(presence));
        _callbackSubscriptions = callbackSubscriptions
            ?? throw new ArgumentNullException(nameof(callbackSubscriptions));
    }

    /// <inheritdoc />
    public void OpenMatchChannel(string sessionToken, long matchId)
    {
        Match match = RequireAuthorizedMatch(sessionToken, matchId);
        MatchParticipant participant = RequireParticipant(sessionToken, matchId);
        if (!_applicationService.IsTrackedMatchInProgress(matchId))
        {
            throw ServiceFaultFactory.CreateMatchFault(MatchFailure.MatchNotInProgress);
        }

        _callbackSubscriptions.Register(sessionToken, matchId);
        KeyValuePair<int, bool>[]? snapshot = _presence.RegisterConnection(
            match,
            participant.SeatNumber,
            sessionToken);
        if (snapshot == null)
        {
            throw ServiceFaultFactory.CreateMatchFault(MatchFailure.MatchNotInProgress);
        }

        PublishPresence(matchId, snapshot);
    }

    /// <inheritdoc />
    public MatchInformation GetMatchInformation(string sessionToken, long matchId)
    {
        Match match = RequireAuthorizedMatch(sessionToken, matchId);
        MatchParticipant participant = RequireParticipant(sessionToken, matchId);

        return new MatchInformation
        {
            MatchId = match.Id,
            Participants = MatchContractMapper.MapParticipants(match),
            TurnDurationSeconds = match.TurnDurationSeconds,
            AssignedSeatNumber = participant.SeatNumber
        };
    }

    /// <inheritdoc />
    public MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetMatchState(sessionToken, matchId);
        ThrowFaultIfFailed(result.Status);

        Match match = RequireValue(result);
        return MatchContractMapper.MapState(match);
    }

    /// <inheritdoc />
    public MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement)
    {
        MatchPlacement? domainPlacement = MapPlacement(placement);
        if (domainPlacement == null)
        {
            throw ServiceFaultFactory.CreateMatchFault(MatchFailure.MissingPlacement);
        }

        MatchResult<Match> result = _applicationService.PlacePiece(
            sessionToken,
            domainPlacement);
        ThrowFaultIfFailed(result.Status);

        Match match = RequireValue(result);
        MatchStateSnapshot snapshot = MatchContractMapper.MapState(match);
        PublishMatchStateChanged(match.Id, snapshot);
        return snapshot;
    }

    /// <inheritdoc />
    public MatchConclusion ForfeitMatch(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.ForfeitMatch(sessionToken, matchId);
        ThrowFaultIfFailed(result.Status);

        Match match = RequireValue(result);
        MatchStateSnapshot snapshot = MatchContractMapper.MapState(match);
        PublishMatchStateChanged(match.Id, snapshot);
        return MatchContractMapper.MapConclusion(match);
    }

    /// <inheritdoc />
    public MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.ObserveMatch(sessionToken, matchId);
        ThrowFaultIfFailed(result.Status);

        Match match = RequireValue(result);
        _callbackSubscriptions.Register(sessionToken, matchId);
        return MatchContractMapper.MapState(match);
    }

    /// <inheritdoc />
    public MatchSummary[] GetMatchHistory(string sessionToken)
    {
        MatchResult<long> accountResult = _applicationService.GetSessionAccountId(sessionToken);
        ThrowFaultIfFailed(accountResult.Status);

        MatchResult<Match[]> result = _applicationService.GetMatchHistory(sessionToken);
        ThrowFaultIfFailed(result.Status);

        Match[] matches = RequireValue(result);
        long accountId = accountResult.Value;
        return matches
            .Select(match => MatchContractMapper.MapSummary(match, accountId))
            .ToArray();
    }

    /// <inheritdoc />
    public MatchDetails GetMatchDetails(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetAuthorizedMatchSnapshot(
            sessionToken,
            matchId);
        ThrowFaultIfFailed(result.Status);

        Match match = RequireValue(result);
        return MatchContractMapper.MapDetails(match);
    }

    /// <inheritdoc />
    public MoveRecord[] GetMoveHistory(string sessionToken, long matchId)
    {
        MatchResult<MatchMove[]> result = _applicationService.GetMoveHistory(
            sessionToken,
            matchId);
        ThrowFaultIfFailed(result.Status);

        MatchMove[] moves = RequireValue(result);
        return moves.Select(MatchContractMapper.MapMove).ToArray();
    }

    private void PublishMatchStateChanged(long matchId, MatchStateSnapshot snapshot)
    {
        _callbackSubscriptions.Publish(
            matchId,
            (token, callback) => callback.OnMatchStateChanged(snapshot));
    }

    private void PublishPresence(long matchId, KeyValuePair<int, bool>[] snapshot)
    {
        _callbackSubscriptions.Publish(matchId, (token, callback) =>
        {
            for (int index = 0; index < snapshot.Length; index++)
            {
                callback.OnParticipantConnectionChanged(
                    matchId,
                    snapshot[index].Key,
                    snapshot[index].Value);
            }
        });
    }

    private static T RequireValue<T>(MatchResult<T> result)
        where T : class
    {
        return result.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
    }

    private static MatchPlacement? MapPlacement(PiecePlacement? placement)
    {
        if (placement == null)
        {
            return null;
        }

        return new MatchPlacement(
            placement.MatchId,
            placement.ExpectedMatchStateVersion,
            placement.Row,
            placement.Column);
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

        throw ServiceFaultFactory.CreateMatchFault(MapMatchFailure(status));
    }

    private static MatchFailure MapMatchFailure(MatchStatus status)
    {
        MatchFailure failure = status switch
        {
            MatchStatus.InactiveAccount => MatchFailure.InactiveAccount,
            MatchStatus.InvalidMatchId => MatchFailure.InvalidMatchId,
            MatchStatus.MatchNotFound => MatchFailure.MatchNotFound,
            MatchStatus.MatchNotWaiting => MatchFailure.MatchNotWaiting,
            MatchStatus.MatchNotInProgress => MatchFailure.MatchNotInProgress,
            MatchStatus.MissingPlacement => MatchFailure.MissingPlacement,
            MatchStatus.InvalidMatchStateVersion => MatchFailure.InvalidMatchStateVersion,
            MatchStatus.StaleMatchStateVersion => MatchFailure.StaleMatchStateVersion,
            MatchStatus.NotPlayersTurn => MatchFailure.NotPlayersTurn,
            MatchStatus.InvalidPosition => MatchFailure.InvalidPosition,
            MatchStatus.SpectatingUnavailable => MatchFailure.SpectatingUnavailable,
            MatchStatus.Unauthorized => MatchFailure.Unauthorized,
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unsupported match status.")
        };

        return failure;
    }

    private Match RequireAuthorizedMatch(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetAuthorizedMatchSnapshot(
            sessionToken,
            matchId);
        ThrowFaultIfFailed(result.Status);

        return RequireValue(result);
    }

    private MatchParticipant RequireParticipant(string sessionToken, long matchId)
    {
        MatchResult<MatchParticipant> result =
            _applicationService.GetAuthorizedParticipant(sessionToken, matchId);
        ThrowFaultIfFailed(result.Status);
        return RequireValue(result);
    }
}
