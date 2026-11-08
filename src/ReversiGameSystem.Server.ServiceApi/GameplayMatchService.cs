using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
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
    private readonly MatchBoundary _boundary;
    private readonly MatchCallbackSubscriptions<IGameplayMatchServiceCallback>
        _callbackSubscriptions;
    private readonly object _presenceSync = new object();
    private readonly Dictionary<long, Dictionary<int, HashSet<string>>> _participantPresence =
        new Dictionary<long, Dictionary<int, HashSet<string>>>();

    /// <summary>Creates the initial match boundary.</summary>
    public GameplayMatchService(
        MatchApplicationService applicationService,
        ISessionRepository sessions,
        ILogger<GameplayMatchService> logger)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _boundary = new MatchBoundary(logger);
        _callbackSubscriptions =
            new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
            AuthorizeChannel,
            RemovePresence,
            logger);
    }

    /// <inheritdoc />
    public void OpenMatchChannel(string sessionToken, long matchId)
    {
        _boundary.Execute(nameof(OpenMatchChannel), () =>
        {
            Match match = GetMatch(sessionToken, matchId);
            MatchContractMapper.RequireStarted(match);
            AuthenticatedSession session = GetSession(sessionToken);
            MatchParticipant participant = match.Participants.Single(
                current => current.AccountId == session.AccountId);
            _callbackSubscriptions.Register(sessionToken, matchId);
            AddPresence(
                sessionToken,
                matchId,
                participant.SeatNumber,
                match.Participants);
            return true;
        });
    }

    /// <inheritdoc />
    public MatchInformation GetMatchInformation(string sessionToken, long matchId)
    {
        return _boundary.Execute(nameof(GetMatchInformation), () =>
        {
            Match match = GetMatch(sessionToken, matchId);
            MatchContractMapper.RequireStarted(match);
            AuthenticatedSession session = GetSession(sessionToken);
            return new MatchInformation
            {
                MatchId = match.Id,
                Participants = MatchContractMapper.Participants(match),
                TurnDurationSeconds = match.TurnDurationSeconds,
                AssignedSeatNumber = match.Participants
                    .Single(player => player.AccountId == session.AccountId)
                    .SeatNumber
            };
        });
    }

    /// <inheritdoc />
    public MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
    {
        return _boundary.Execute(nameof(GetMatchState), () =>
            MatchContractMapper.InitialState(GetMatch(sessionToken, matchId)));
    }

    /// <inheritdoc />
    public MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement)
    {
        throw MatchBoundary.Unsupported(nameof(PlacePiece));
    }

    /// <inheritdoc />
    public MatchConclusion ForfeitMatch(string sessionToken, long matchId)
    {
        throw MatchBoundary.Unsupported(nameof(ForfeitMatch));
    }

    /// <inheritdoc />
    public MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)
    {
        throw MatchBoundary.Unsupported(nameof(ObserveMatch));
    }

    /// <inheritdoc />
    public MatchSummary[] GetMatchHistory(string sessionToken)
    {
        throw MatchBoundary.Unsupported(nameof(GetMatchHistory));
    }

    /// <inheritdoc />
    public MatchDetails GetMatchDetails(string sessionToken, long matchId)
    {
        throw MatchBoundary.Unsupported(nameof(GetMatchDetails));
    }

    /// <inheritdoc />
    public MoveRecord[] GetMoveHistory(string sessionToken, long matchId)
    {
        throw MatchBoundary.Unsupported(nameof(GetMoveHistory));
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
        MatchOperationResult<Match> result = _applicationService.Get(sessionToken, matchId);
        if (result.Status == MatchOperationStatus.MissingSessionToken)
        {
            throw new SessionRejectedException(SessionRejection.MissingSessionToken);
        }
        if (result.Status == MatchOperationStatus.InvalidSession)
        {
            throw new SessionRejectedException();
        }
        if (result.Status != MatchOperationStatus.Success)
        {
            throw new MatchRejectedException(GetLegacyRejection(result.Status));
        }

        return result.GetValue();
    }

    private bool AuthorizeChannel(string token, long matchId)
    {
        bool isAuthorized = false;
        try
        {
            Match match = GetMatch(token, matchId);
            MatchContractMapper.RequireStarted(match);
            isAuthorized = true;
        }
        catch (SessionRejectedException)
        {
        }
        catch (MatchRejectedException)
        {
        }

        return isAuthorized;
    }

    private AuthenticatedSession GetSession(string sessionToken)
    {
        if (!_sessions.TryGet(sessionToken, out AuthenticatedSession? session)
            || session == null)
        {
            throw new SessionRejectedException();
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

    private static MatchRejection GetLegacyRejection(MatchOperationStatus status)
    {
        MatchRejection rejection;
        if (status == MatchOperationStatus.InvalidMatchId)
        {
            rejection = MatchRejection.InvalidMatchId;
        }
        else if (status == MatchOperationStatus.MatchNotFound)
        {
            rejection = MatchRejection.MatchNotFound;
        }
        else if (status == MatchOperationStatus.MatchNotWaiting)
        {
            rejection = MatchRejection.MatchNotWaiting;
        }
        else if (status == MatchOperationStatus.MatchNotInProgress)
        {
            rejection = MatchRejection.MatchNotInProgress;
        }
        else if (status == MatchOperationStatus.InactiveAccount)
        {
            rejection = MatchRejection.InactiveAccount;
        }
        else if (status == MatchOperationStatus.Unauthorized)
        {
            rejection = MatchRejection.Unauthorized;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return rejection;
    }
}
