using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Exposes authorized initial match information without simulating gameplay writes.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class GameplayMatchService : IGameplayMatchService, IDisposable
{
    private readonly MatchApplicationService _applicationService;
    private readonly ISessionRepository _sessions;
    private readonly ILogger<GameplayMatchService> _logger;
    private readonly MatchCallbackSubscriptions<IGameplayMatchServiceCallback> _callbackSubscriptions;
    private readonly object _presenceSync = new object();
    private readonly Dictionary<long, Dictionary<int, HashSet<string>>> _participantPresence =
        new Dictionary<long, Dictionary<int, HashSet<string>>>();

    /// <summary>Initializes a new instance of the <see cref="GameplayMatchService"/> class.</summary>
    public GameplayMatchService(
        MatchApplicationService applicationService,
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
    }

    /// <inheritdoc />
    public void OpenMatchChannel(string sessionToken, long matchId)
    {
        Match match = GetMatch(sessionToken, matchId);
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
        Match match = GetMatch(sessionToken, matchId);
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
        Match match = GetMatch(sessionToken, matchId);
        return MatchContractMapper.InitialState(match);
    }

    /// <inheritdoc />
    public MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement)
    {
        throw CreateUnsupportedFault(nameof(PlacePiece));
    }

    /// <inheritdoc />
    public MatchConclusion ForfeitMatch(string sessionToken, long matchId)
    {
        throw CreateUnsupportedFault(nameof(ForfeitMatch));
    }

    /// <inheritdoc />
    public MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)
    {
        throw CreateUnsupportedFault(nameof(ObserveMatch));
    }

    /// <inheritdoc />
    public MatchSummary[] GetMatchHistory(string sessionToken)
    {
        throw CreateUnsupportedFault(nameof(GetMatchHistory));
    }

    /// <inheritdoc />
    public MatchDetails GetMatchDetails(string sessionToken, long matchId)
    {
        throw CreateUnsupportedFault(nameof(GetMatchDetails));
    }

    /// <inheritdoc />
    public MoveRecord[] GetMoveHistory(string sessionToken, long matchId)
    {
        throw CreateUnsupportedFault(nameof(GetMoveHistory));
    }

    /// <summary>Removes all match subscriptions for a revoked session.</summary>
    public void RemoveSession(string sessionToken)
    {
        _callbackSubscriptions.RemoveSession(sessionToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _callbackSubscriptions.Dispose();
    }

    private Match GetMatch(string sessionToken, long matchId)
    {
        MatchResult<Match> result = _applicationService.Get(sessionToken, matchId);
        ThrowFailure(result.Status);

        Match match = result.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        MatchContractMapper.RequireStarted(match);
        return match;
    }

    private bool AuthorizeChannel(string token, long matchId)
    {
        MatchResult<Match> result = _applicationService.Get(token, matchId);
        if (result.Status != MatchStatus.Success || result.Value == null)
        {
            return false;
        }

        return string.Equals(result.Value.State, "InProgress", StringComparison.Ordinal);
    }

    private AuthenticatedSession GetSession(string sessionToken)
    {
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
        MatchFailure failure = MatchFailure.Unauthorized;
        if (status == MatchStatus.InactiveAccount)
        {
            failure = MatchFailure.InactiveAccount;
        }
        else if (status == MatchStatus.InvalidMatchId)
        {
            failure = MatchFailure.InvalidMatchId;
        }
        else if (status == MatchStatus.MatchNotFound)
        {
            failure = MatchFailure.MatchNotFound;
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

    private static FaultException<MatchFault> CreateMatchFault(MatchFailure failure)
    {
        return new FaultException<MatchFault>(new MatchFault
        {
            Reason = failure
        }, "The match request was rejected.");
    }

    private static FaultException<ServiceFault> CreateUnsupportedFault(string operation)
    {
        return ServiceFaultFactory.Create(operation, CorrelationContext.GetOrCreate());
    }
}
