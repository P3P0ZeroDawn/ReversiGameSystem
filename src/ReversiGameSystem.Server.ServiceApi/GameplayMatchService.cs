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
    private const string FinishedMatchState = "Finished";
    private const string MissingResultValueMessage =
        "A successful match result must contain its value.";
    private const string RejectedMatchRequestReason = "The match request was rejected.";
    private const int TickIdle = 0;
    private const int TickRunning = 1;
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
        Match match = RequireAuthorizedMatch(sessionToken, matchId);
        MatchContractMapper.RequireStarted(match);
        MatchParticipant participant = RequireParticipant(match, sessionToken);

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
        Match match = RequireAuthorizedMatch(sessionToken, matchId);
        MatchParticipant participant = RequireParticipant(match, sessionToken);

        MatchInformation information = new MatchInformation
        {
            MatchId = match.Id,
            Participants = MatchContractMapper.Participants(match),
            TurnDurationSeconds = match.TurnDurationSeconds,
            AssignedSeatNumber = participant.SeatNumber
        };
        return information;
    }

    /// <inheritdoc />
    public MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetMatchState(sessionToken, matchId);
        ThrowFaultIfFailed(result.Status);

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
        ThrowFaultIfFailed(result.Status);

        Match match = RequireValue(result);
        MatchStateSnapshot snapshot = MatchContractMapper.State(match);
        PublishMatchStateChanged(match.Id, snapshot);
        return snapshot;
    }

    /// <inheritdoc />
    public MatchConclusion ForfeitMatch(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.ForfeitMatch(sessionToken, matchId);
        ThrowFaultIfFailed(result.Status);

        Match match = RequireValue(result);
        MatchStateSnapshot snapshot = MatchContractMapper.State(match);
        PublishMatchStateChanged(match.Id, snapshot);
        return MatchContractMapper.Conclusion(match);
    }

    /// <inheritdoc />
    public MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.ObserveMatch(sessionToken, matchId);
        ThrowFaultIfFailed(result.Status);

        Match match = RequireValue(result);
        _callbackSubscriptions.Register(sessionToken, matchId);
        return MatchContractMapper.State(match);
    }

    /// <inheritdoc />
    public MatchSummary[] GetMatchHistory(string sessionToken)
    {
        AuthenticatedSession session = RequireActiveSession(sessionToken);
        MatchResult<Match[]> result = _applicationService.GetMatchHistory(sessionToken);
        ThrowFaultIfFailed(result.Status);

        Match[] matches = RequireValue(result);
        MatchSummary[] summaries = matches
            .Select(match => MatchContractMapper.Summary(match, session.AccountId))
            .ToArray();
        return summaries;
    }

    /// <inheritdoc />
    public MatchDetails GetMatchDetails(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetAuthorizedMatchSnapshot(
            sessionToken,
            matchId);
        ThrowFaultIfFailed(result.Status);

        Match match = RequireValue(result);
        return MatchContractMapper.Details(match);
    }

    /// <inheritdoc />
    public MoveRecord[] GetMoveHistory(string sessionToken, long matchId)
    {
        MatchResult<MatchMove[]> result = _applicationService.GetMoveHistory(
            sessionToken,
            matchId);
        ThrowFaultIfFailed(result.Status);

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
        if (Interlocked.Exchange(ref _timeoutTickRunning, TickRunning) != TickIdle)
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
            Volatile.Write(ref _timeoutTickRunning, TickIdle);
        }
    }

    private void OnPersistenceTick(object? state)
    {
        if (Interlocked.Exchange(ref _persistenceSyncRunning, TickRunning) != TickIdle)
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
            Volatile.Write(ref _persistenceSyncRunning, TickIdle);
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

        bool isInProgress = string.Equals(
            result.Value.State,
            InProgressMatchState,
            StringComparison.Ordinal);
        bool isFinished = string.Equals(
            result.Value.State,
            FinishedMatchState,
            StringComparison.Ordinal);
        return isInProgress || isFinished;
    }

    private static T RequireValue<T>(MatchResult<T> result)
        where T : class
    {
        if (result.Value == null)
        {
            throw new InvalidOperationException(MissingResultValueMessage);
        }

        return result.Value;
    }

    private AuthenticatedSession RequireActiveSession(string sessionToken)
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
        KeyValuePair<int, bool>[] snapshot = matchPresence
            .OrderBy(entry => entry.Key)
            .Select(entry => new KeyValuePair<int, bool>(entry.Key, entry.Value.Count > 0))
            .ToArray();
        return snapshot;
    }

    private static MatchPlacement? MapPlacement(PiecePlacement? placement)
    {
        if (placement == null)
        {
            return null;
        }

        MatchPlacement domainPlacement = new MatchPlacement(
            placement.MatchId,
            placement.ExpectedMatchStateVersion,
            placement.Row,
            placement.Column);
        return domainPlacement;
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

        MatchFailure failure = MapMatchFailure(status);
        throw CreateMatchFault(failure);
    }

    private static MatchFailure MapMatchFailure(MatchStatus status)
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
        MatchFault detail = new MatchFault
        {
            Reason = failure
        };
        FaultException<MatchFault> fault = new FaultException<MatchFault>(
            detail,
            RejectedMatchRequestReason);
        return fault;
    }

    private Match RequireAuthorizedMatch(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.GetAuthorizedMatchSnapshot(
            sessionToken,
            matchId);
        ThrowFaultIfFailed(result.Status);

        return RequireValue(result);
    }

    private MatchParticipant RequireParticipant(Match match, string sessionToken)
    {
        AuthenticatedSession session = RequireActiveSession(sessionToken);
        MatchParticipant? participant = match.Participants.FirstOrDefault(
            current => current.AccountId == session.AccountId);
        if (participant == null)
        {
            throw CreateMatchFault(MatchFailure.Unauthorized);
        }

        return participant;
    }
}
