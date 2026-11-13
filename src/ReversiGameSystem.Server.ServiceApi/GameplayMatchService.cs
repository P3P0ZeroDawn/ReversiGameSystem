using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Publishes authoritative active match gameplay and handles client placement requests.
/// </summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class GameplayMatchService : IGameplayMatchService, IDisposable
{
    private const string InProgressMatchState = "InProgress";
    private static readonly TimeSpan TimeoutTickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PersistenceTickInterval = TimeSpan.FromSeconds(1);
    private readonly GameplayMatchApplicationService _applicationService;
    private readonly ISessionRepository _sessions;
    private readonly ILogger<GameplayMatchService> _logger;
    private readonly MatchCallbackSubscriptions<IGameplayMatchServiceCallback>
        _callbackSubscriptions;
    private readonly object _presenceSync = new object();
    private readonly Dictionary<long, Dictionary<int, HashSet<string>>> _participantPresence =
        new Dictionary<long, Dictionary<int, HashSet<string>>>();
    private readonly Timer _timeoutTimer;
    private readonly Timer _persistenceTimer;
    private int _timeoutTickRunning;
    private int _persistenceSyncRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameplayMatchService"/> class.
    /// </summary>
    public GameplayMatchService(
        GameplayMatchApplicationService applicationService,
        ISessionRepository sessions,
        ILogger<GameplayMatchService> logger)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _callbackSubscriptions =
            new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
            AuthorizeChannel,
            RemovePresence,
            _logger);
        _timeoutTimer = new Timer(OnTimeoutTick, null, TimeoutTickInterval, TimeoutTickInterval);
        _persistenceTimer = new Timer(
            OnPersistenceTick,
            null,
            PersistenceTickInterval,
            PersistenceTickInterval);
    }

    /// <inheritdoc />
    public void OpenMatchChannel(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetMatchInformation(
            sessionToken,
            matchId);
        ThrowFailure(result.Status);

        Match match = RequireValue(result);
        MatchContractMapper.RequireStarted(match);

        AuthenticatedSession session = GetSession(sessionToken);
        MatchParticipant? participant = match.Participants.FirstOrDefault(
            current => current.AccountId == session.AccountId);
        if (participant == null)
        {
            throw CreateMatchFault(MatchFailure.Unauthorized);
        }

        _callbackSubscriptions.Register(sessionToken, matchId);
        AddPresence(
            sessionToken,
            matchId,
            participant.SeatNumber,
            match.Participants);
    }

    /// <inheritdoc />
    public MatchInformation GetMatchInformation(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetMatchInformation(
            sessionToken,
            matchId);
        ThrowFailure(result.Status);

        Match match = RequireValue(result);
        AuthenticatedSession session = GetSession(sessionToken);
        MatchParticipant? participant = match.Participants.FirstOrDefault(
            player => player.AccountId == session.AccountId);
        if (participant == null)
        {
            throw CreateMatchFault(MatchFailure.Unauthorized);
        }

        return new MatchInformation
        {
            MatchId = match.Id,
            Participants = MatchContractMapper.Participants(match),
            TurnDurationSeconds = match.TurnDurationSeconds,
            AssignedSeatNumber = participant.SeatNumber
        };
    }

    /// <inheritdoc />
    public MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetMatchState(sessionToken, matchId);
        ThrowFailure(result.Status);

        Match match = RequireValue(result);
        return MatchContractMapper.State(match);
    }

    /// <inheritdoc />
    public MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement)
    {
        MatchPlacement? domainPlacement = MapPlacement(placement);
        if (domainPlacement == null)
        {
            throw CreateMatchFault(MatchFailure.MissingPlacement);
        }

        MatchResult<Match> result = _applicationService.PlacePiece(
            sessionToken,
            domainPlacement);
        ThrowFailure(result.Status);

        Match match = RequireValue(result);
        MatchStateSnapshot snapshot = MatchContractMapper.State(match);
        PublishMatchStateChanged(match.Id, snapshot);
        return snapshot;
    }

    /// <inheritdoc />
    public MatchConclusion ForfeitMatch(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.ForfeitMatch(sessionToken, matchId);
        ThrowFailure(result.Status);

        Match match = RequireValue(result);
        MatchStateSnapshot snapshot = MatchContractMapper.State(match);
        PublishMatchStateChanged(match.Id, snapshot);
        return MatchContractMapper.Conclusion(match);
    }

    /// <inheritdoc />
    public MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.ObserveMatch(sessionToken, matchId);
        ThrowFailure(result.Status);

        Match match = RequireValue(result);
        _callbackSubscriptions.Register(sessionToken, matchId);
        return MatchContractMapper.State(match);
    }

    /// <inheritdoc />
    public MatchSummary[] GetMatchHistory(string sessionToken)
    {
        AuthenticatedSession session = GetSession(sessionToken);
        MatchResult<Match[]> result = _applicationService.GetMatchHistory(sessionToken);
        ThrowFailure(result.Status);

        Match[] matches = RequireValue(result);
        return matches
            .Select(match => MatchContractMapper.Summary(match, session.AccountId))
            .ToArray();
    }

    /// <inheritdoc />
    public MatchDetails GetMatchDetails(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetMatchDetails(sessionToken, matchId);
        ThrowFailure(result.Status);

        Match match = RequireValue(result);
        return MatchContractMapper.Details(match);
    }

    /// <inheritdoc />
    public MoveRecord[] GetMoveHistory(string sessionToken, long matchId)
    {
        MatchResult<MatchMove[]> result = _applicationService.GetMoveHistory(
            sessionToken,
            matchId);
        ThrowFailure(result.Status);

        MatchMove[] moves = RequireValue(result);
        return moves.Select(MatchContractMapper.Move).ToArray();
    }

    /// <summary>Removes all match subscriptions for a revoked session.</summary>
    public void RemoveSession(string sessionToken)
    {
        _callbackSubscriptions.RemoveSession(sessionToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timeoutTimer.Dispose();
        _persistenceTimer.Dispose();
        _callbackSubscriptions.Dispose();
    }

    private void OnTimeoutTick(object? state)
    {
        if (Interlocked.Exchange(ref _timeoutTickRunning, 1) != 0)
        {
            return;
        }

        try
        {
            Match[] transitionedMatches = _applicationService.ProcessTurnTimeouts();
            for (int i = 0; i < transitionedMatches.Length; i++)
            {
                Match match = transitionedMatches[i];
                MatchStateSnapshot snapshot = MatchContractMapper.State(match);
                PublishMatchStateChanged(match.Id, snapshot);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while processing turn timeouts.");
        }
        finally
        {
            Volatile.Write(ref _timeoutTickRunning, 0);
        }
    }

    private void OnPersistenceTick(object? state)
    {
        if (Interlocked.Exchange(ref _persistenceSyncRunning, 1) != 0)
        {
            return;
        }

        try
        {
            _applicationService.SynchronizePendingMatches();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An error occurred while synchronizing pending matches.");
        }
        finally
        {
            Volatile.Write(ref _persistenceSyncRunning, 0);
        }
    }

    private void PublishMatchStateChanged(long matchId, MatchStateSnapshot snapshot)
    {
        _callbackSubscriptions.Publish(matchId, (token, callback) =>
        {
            callback.OnMatchStateChanged(snapshot);
        });
    }

    private bool AuthorizeChannel(string token, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetMatchState(token, matchId);
        if (result.Status != MatchStatus.Success || result.Value == null)
        {
            return false;
        }

        return string.Equals(
            result.Value.State,
            InProgressMatchState,
            StringComparison.Ordinal);
    }

    private static T RequireValue<T>(MatchResult<T> result)
        where T : class
    {
        return result.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
    }

    private AuthenticatedSession GetSession(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            throw SessionFaultFactory.Create(SessionFailure.MissingSessionToken);
        }

        if (!_sessions.TryGet(sessionToken, out AuthenticatedSession? session)
            || session == null)
        {
            throw SessionFaultFactory.Create(SessionFailure.InvalidSession);
        }

        return session;
    }

    private void AddPresence(
        string sessionToken,
        long matchId,
        int seatNumber,
        MatchParticipant[] participants)
    {
        KeyValuePair<int, bool>[]? snapshot = null;
        lock (_presenceSync)
        {
            if (!_participantPresence.TryGetValue(
                matchId,
                out Dictionary<int, HashSet<string>>? matchPresence))
            {
                matchPresence = new Dictionary<int, HashSet<string>>();
                for (int index = 0; index < participants.Length; index++)
                {
                    matchPresence.Add(
                        participants[index].SeatNumber,
                        new HashSet<string>(StringComparer.Ordinal));
                }

                _participantPresence.Add(matchId, matchPresence);
            }

            if (matchPresence.TryGetValue(seatNumber, out HashSet<string>? seatTokens))
            {
                seatTokens.Add(sessionToken);
                snapshot = CreatePresenceSnapshot(matchPresence);
            }
        }

        if (snapshot != null)
        {
            PublishPresence(matchId, snapshot);
        }
    }

    private void RemovePresence(string sessionToken, long matchId)
    {
        KeyValuePair<int, bool>[]? snapshot = null;
        lock (_presenceSync)
        {
            if (_participantPresence.TryGetValue(
                matchId,
                out Dictionary<int, HashSet<string>>? matchPresence))
            {
                foreach (HashSet<string> tokens in matchPresence.Values)
                {
                    tokens.Remove(sessionToken);
                }

                snapshot = CreatePresenceSnapshot(matchPresence);
                if (!matchPresence.Values.Any(tokens => tokens.Count > 0))
                {
                    _participantPresence.Remove(matchId);
                }
            }
        }

        if (snapshot != null)
        {
            PublishPresence(matchId, snapshot);
        }
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

    private static KeyValuePair<int, bool>[] CreatePresenceSnapshot(
        Dictionary<int, HashSet<string>> matchPresence)
    {
        return matchPresence
            .OrderBy(entry => entry.Key)
            .Select(entry => new KeyValuePair<int, bool>(entry.Key, entry.Value.Count > 0))
            .ToArray();
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

        MatchFailure failure = GetMatchFailure(status);
        throw CreateMatchFault(failure);
    }

    private static MatchFailure GetMatchFailure(MatchStatus status)
    {
        switch (status)
        {
            case MatchStatus.InactiveAccount:
                return MatchFailure.InactiveAccount;
            case MatchStatus.InvalidMatchId:
                return MatchFailure.InvalidMatchId;
            case MatchStatus.MatchNotFound:
                return MatchFailure.MatchNotFound;
            case MatchStatus.MatchNotWaiting:
                return MatchFailure.MatchNotWaiting;
            case MatchStatus.MatchNotInProgress:
                return MatchFailure.MatchNotInProgress;
            case MatchStatus.MissingPlacement:
                return MatchFailure.MissingPlacement;
            case MatchStatus.InvalidMatchStateVersion:
                return MatchFailure.InvalidMatchStateVersion;
            case MatchStatus.StaleMatchStateVersion:
                return MatchFailure.StaleMatchStateVersion;
            case MatchStatus.NotPlayersTurn:
                return MatchFailure.NotPlayersTurn;
            case MatchStatus.InvalidPosition:
                return MatchFailure.InvalidPosition;
            case MatchStatus.SpectatingUnavailable:
                return MatchFailure.SpectatingUnavailable;
            default:
                return MatchFailure.Unauthorized;
        }
    }

    private static FaultException<MatchFault> CreateMatchFault(MatchFailure failure)
    {
        return new FaultException<MatchFault>(new MatchFault
        {
            Reason = failure
        }, "The match request was rejected.");
    }

}
