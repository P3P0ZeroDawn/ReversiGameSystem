using System;
using System.Collections.Generic;
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

/// <summary>Publishes authoritative active match gameplay and handles client placement requests.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class GameplayMatchService : IGameplayMatchService, IDisposable
{
    private static readonly TimeSpan TimeoutTickInterval = TimeSpan.FromSeconds(1);
    private readonly GameplayMatchApplicationService _applicationService;
    private readonly ISessionRepository _sessions;
    private readonly ILogger<GameplayMatchService> _logger;
    private readonly MatchCallbackSubscriptions<IGameplayMatchServiceCallback> _callbackSubscriptions;
    private readonly object _presenceSync = new object();
    private readonly Dictionary<long, Dictionary<int, HashSet<string>>> _participantPresence =
        new Dictionary<long, Dictionary<int, HashSet<string>>>();
    private readonly Timer _timeoutTimer;
    private int _timeoutTickRunning;

    /// <summary>Initializes a new instance of the <see cref="GameplayMatchService"/> class.</summary>
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
    }

    /// <inheritdoc />
    public void OpenMatchChannel(string sessionToken, long matchId)
    {
        try
        {
            MatchResult<Match> result = _applicationService.GetMatchInformation(sessionToken, matchId);
            ThrowFailure(result.Status);

            Match match = result.Value
                ?? throw new InvalidOperationException("A successful match result must contain its value.");
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
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A technical failure occurred in OpenMatchChannel.");
            throw CreateServiceFault(nameof(OpenMatchChannel));
        }
    }

    /// <inheritdoc />
    public MatchInformation GetMatchInformation(string sessionToken, long matchId)
    {
        try
        {
            MatchResult<Match> result = _applicationService.GetMatchInformation(sessionToken, matchId);
            ThrowFailure(result.Status);

            Match match = result.Value
                ?? throw new InvalidOperationException("A successful match result must contain its value.");
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
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A technical failure occurred in GetMatchInformation.");
            throw CreateServiceFault(nameof(GetMatchInformation));
        }
    }

    /// <inheritdoc />
    public MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
    {
        try
        {
            MatchResult<Match> result = _applicationService.GetMatchState(sessionToken, matchId);
            ThrowFailure(result.Status);

            Match match = result.Value
                ?? throw new InvalidOperationException("A successful match result must contain its value.");
            return MatchContractMapper.State(match);
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A technical failure occurred in GetMatchState.");
            throw CreateServiceFault(nameof(GetMatchState));
        }
    }

    /// <inheritdoc />
    public MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement)
    {
        try
        {
            MatchPlacement? domainPlacement = MapPlacement(placement);
            if (domainPlacement == null)
            {
                throw CreateMatchFault(MatchFailure.MissingPlacement);
            }

            MatchResult<Match> result = _applicationService.PlacePiece(sessionToken, domainPlacement);
            ThrowFailure(result.Status);

            Match match = result.Value
                ?? throw new InvalidOperationException("A successful match result must contain its value.");
            MatchStateSnapshot snapshot = MatchContractMapper.State(match);
            PublishMatchStateChanged(match.Id, snapshot);
            return snapshot;
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A technical failure occurred in PlacePiece.");
            throw CreateServiceFault(nameof(PlacePiece));
        }
    }

    /// <inheritdoc />
    public MatchConclusion ForfeitMatch(string sessionToken, long matchId)
    {
        try
        {
            MatchResult<Match> result = _applicationService.ForfeitMatch(sessionToken, matchId);
            ThrowFailure(result.Status);

            Match match = result.Value
                ?? throw new InvalidOperationException("A successful match result must contain its value.");
            MatchStateSnapshot snapshot = MatchContractMapper.State(match);
            PublishMatchStateChanged(match.Id, snapshot);
            return MatchContractMapper.Conclusion(match);
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A technical failure occurred in ForfeitMatch.");
            throw CreateServiceFault(nameof(ForfeitMatch));
        }
    }

    /// <inheritdoc />
    public MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)
    {
        try
        {
            MatchResult<Match> result = _applicationService.ObserveMatch(sessionToken, matchId);
            ThrowFailure(result.Status);

            Match match = result.Value
                ?? throw new InvalidOperationException("A successful match result must contain its value.");
            _callbackSubscriptions.Register(sessionToken, matchId);
            return MatchContractMapper.State(match);
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A technical failure occurred in ObserveMatch.");
            throw CreateServiceFault(nameof(ObserveMatch));
        }
    }

    /// <inheritdoc />
    public MatchSummary[] GetMatchHistory(string sessionToken)
    {
        try
        {
            AuthenticatedSession session = GetSession(sessionToken);
            MatchResult<Match[]> result = _applicationService.GetMatchHistory(sessionToken);
            ThrowFailure(result.Status);

            Match[] matches = result.Value
                ?? throw new InvalidOperationException("A successful match result must contain its value.");
            return matches.Select(match => MatchContractMapper.Summary(match, session.AccountId)).ToArray();
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A technical failure occurred in GetMatchHistory.");
            throw CreateServiceFault(nameof(GetMatchHistory));
        }
    }

    /// <inheritdoc />
    public MatchDetails GetMatchDetails(string sessionToken, long matchId)
    {
        try
        {
            MatchResult<Match> result = _applicationService.GetMatchDetails(sessionToken, matchId);
            ThrowFailure(result.Status);

            Match match = result.Value
                ?? throw new InvalidOperationException("A successful match result must contain its value.");
            return MatchContractMapper.Details(match);
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A technical failure occurred in GetMatchDetails.");
            throw CreateServiceFault(nameof(GetMatchDetails));
        }
    }

    /// <inheritdoc />
    public MoveRecord[] GetMoveHistory(string sessionToken, long matchId)
    {
        try
        {
            MatchResult<MatchMove[]> result = _applicationService.GetMoveHistory(sessionToken, matchId);
            ThrowFailure(result.Status);

            MatchMove[] moves = result.Value
                ?? throw new InvalidOperationException("A successful match result must contain its value.");
            return moves.Select(MatchContractMapper.Move).ToArray();
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A technical failure occurred in GetMoveHistory.");
            throw CreateServiceFault(nameof(GetMoveHistory));
        }
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

        return string.Equals(result.Value.State, "InProgress", StringComparison.Ordinal);
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

            matchPresence[seatNumber].Add(sessionToken);
            PublishPresence(matchId, CreatePresenceSnapshot(matchPresence));
        }
    }

    private void RemovePresence(string sessionToken, long matchId)
    {
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

                PublishPresence(matchId, CreatePresenceSnapshot(matchPresence));
                if (!matchPresence.Values.Any(tokens => tokens.Count > 0))
                {
                    _participantPresence.Remove(matchId);
                }
            }
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

    private static FaultException<ServiceFault> CreateServiceFault(string operation)
    {
        return ServiceFaultFactory.Create(operation, CorrelationContext.GetOrCreate());
    }
}
