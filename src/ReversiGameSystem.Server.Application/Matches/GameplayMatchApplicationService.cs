using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Matches;

public sealed class GameplayMatchApplicationService
{
    private const string StateInProgress = "InProgress";
    private const string StateFinished = "Finished";
    private static readonly TimeSpan PersistenceRetryInterval = TimeSpan.FromSeconds(5);

    private readonly ISessionRepository _sessions;
    private readonly IGameplayRepository _matches;
    private readonly IUtcClock _clock;
    private readonly object _activeMatchesSync = new object();
    private readonly Dictionary<long, Match> _activeMatches = new Dictionary<long, Match>();
    private readonly HashSet<long> _matchesPendingPersistence = new HashSet<long>();
    private DateTime _nextPersistenceRetryUtc;

    public GameplayMatchApplicationService(
        ISessionRepository sessions,
        IGameplayRepository matches,
        IUtcClock clock)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public void TrackStartedMatch(Match match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        DateTime startedAtUtc = _clock.UtcNow;
        lock (match.SyncRoot)
        {
            GameplayMatchRules.InitializeStartedMatch(match, startedAtUtc);
        }

        lock (_activeMatchesSync)
        {
            _activeMatches[match.Id] = match;
        }
    }

    public MatchResult<Match> PlacePiece(string sessionToken, MatchPlacement? placement)
    {
        if (placement == null)
        {
            return new MatchResult<Match>(MatchStatus.MissingPlacement);
        }

        if (placement.ExpectedVersion < 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchStateVersion);
        }

        MatchResult<MatchSessionContext> resolved = ResolveSessionAndMatch(
            sessionToken,
            placement.MatchId);
        if (resolved.Value == null)
        {
            return new MatchResult<Match>(resolved.Status);
        }

        Match match = resolved.Value.Match;
        MatchResult<Match> transitionResult = ApplyPlayerMoveUnderLock(
            match,
            resolved.Value.Player.AccountId,
            placement);
        if (transitionResult.Status == MatchStatus.Success)
        {
            MarkPersistencePending(match.Id);
        }

        return transitionResult;
    }

    /// <summary>
    /// Evaluates expired turns in memory without blocking on SQL.
    /// </summary>
    /// <returns>Isolated snapshots of matches that transitioned.</returns>
    public Match[] ProcessTurnTimeouts()
    {
        DateTime now = _clock.UtcNow;
        List<Match> expiredMatches = CollectExpiredMatches(now);
        List<Match> updatedSnapshots = new List<Match>();

        for (int i = 0; i < expiredMatches.Count; i++)
        {
            Match? snapshot = ApplyTimeoutIfExpired(expiredMatches[i], now);
            if (snapshot != null)
            {
                updatedSnapshots.Add(snapshot);
            }
        }

        return updatedSnapshots.ToArray();
    }

    /// <summary>
    /// Retries pending snapshot writes, backing off for five seconds after any persistence failure.
    /// </summary>
    public void SynchronizePendingMatches()
    {
        DateTime now = _clock.UtcNow;
        long[] pendingMatchIds;
        lock (_activeMatchesSync)
        {
            if (now < _nextPersistenceRetryUtc || _matchesPendingPersistence.Count == 0)
            {
                return;
            }

            pendingMatchIds = _matchesPendingPersistence.ToArray();
        }

        for (int index = 0; index < pendingMatchIds.Length; index++)
        {
            MatchResult<Match> lookup = FindTrackedMatch(pendingMatchIds[index]);
            if (lookup.Value is not Match match)
            {
                continue;
            }

            Match snapshot;
            lock (match.SyncRoot)
            {
                snapshot = match.CreateSnapshot();
            }

            if (!SynchronizeMatch(snapshot, now))
            {
                break;
            }
        }
    }

    public MatchResult<Match> ForfeitMatch(string sessionToken, long matchId)
    {
        MatchResult<MatchSessionContext> resolved = ResolveSessionAndMatch(sessionToken, matchId);
        if (resolved.Value == null)
        {
            return new MatchResult<Match>(resolved.Status);
        }

        return ConcludeForfeit(resolved.Value.Match, resolved.Value.Player.AccountId, true);
    }

    public bool IsTrackedMatchInProgress(long matchId)
    {
        MatchResult<Match> lookup = FindTrackedMatch(matchId);
        if (lookup.Value is not Match match)
        {
            return false;
        }

        lock (match.SyncRoot)
        {
            return string.Equals(match.State, StateInProgress, StringComparison.Ordinal);
        }
    }

    public MatchResult<Match> ForfeitDisconnectedParticipant(long matchId, int seatNumber)
    {
        MatchResult<Match> lookup = FindTrackedMatch(matchId);
        if (lookup.Value is not Match match)
        {
            return lookup;
        }

        MatchParticipant? participant = Array.Find(
            match.Participants, player => player.SeatNumber == seatNumber);
        if (participant == null)
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        return ConcludeForfeit(match, participant.AccountId, false);
    }

    private MatchResult<Match> ConcludeForfeit(
        Match match, long accountId, bool includeFinishedMatch)
    {
        DateTime now = _clock.UtcNow;
        MatchStatus forfeitStatus;
        Match? snapshot = null;

        lock (match.SyncRoot)
        {
            forfeitStatus = GameplayMatchRules.ForfeitByParticipant(
                match,
                accountId,
                now);
            bool hasConcludedSnapshot = forfeitStatus == MatchStatus.Success
                || (includeFinishedMatch && forfeitStatus == MatchStatus.MatchFinished);
            if (hasConcludedSnapshot)
            {
                snapshot = match.CreateSnapshot();
            }
        }

        if (snapshot == null)
        {
            return new MatchResult<Match>(forfeitStatus);
        }

        if (forfeitStatus == MatchStatus.Success)
        {
            MarkPersistencePending(match.Id);
        }

        return new MatchResult<Match>(snapshot);
    }

    public MatchResult<Match> GetAuthorizedMatchSnapshot(string sessionToken, long matchId)
    {
        MatchResult<MatchSessionContext> resolved = ResolveSessionAndMatch(sessionToken, matchId);
        if (resolved.Value == null)
        {
            return new MatchResult<Match>(resolved.Status);
        }

        Match match = resolved.Value.Match;
        Match? snapshot = null;
        lock (match.SyncRoot)
        {
            if (GameplayMatchRules.IsParticipant(match, resolved.Value.Player.AccountId))
            {
                snapshot = match.CreateSnapshot();
            }
        }

        if (snapshot == null)
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        return new MatchResult<Match>(snapshot);
    }

    public MatchResult<Match> GetMatchState(string sessionToken, long matchId)
    {
        MatchResult<MatchSessionContext> resolved = ResolveSessionAndMatch(sessionToken, matchId);
        if (resolved.Value == null)
        {
            return new MatchResult<Match>(resolved.Status);
        }

        Match match = resolved.Value.Match;
        Match snapshot;
        lock (match.SyncRoot)
        {
            snapshot = match.CreateSnapshot();
        }

        return new MatchResult<Match>(snapshot);
    }

    public MatchResult<Match> ObserveMatch(string sessionToken, long matchId)
    {
        MatchResult<MatchSessionContext> resolved = ResolveSessionAndMatch(sessionToken, matchId);
        if (resolved.Value == null)
        {
            return new MatchResult<Match>(resolved.Status);
        }

        Match match = resolved.Value.Match;
        Match? snapshot = null;
        lock (match.SyncRoot)
        {
            if (string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
            {
                snapshot = match.CreateSnapshot();
            }
        }

        if (snapshot == null)
        {
            return new MatchResult<Match>(MatchStatus.SpectatingUnavailable);
        }

        return new MatchResult<Match>(snapshot);
    }

    public MatchResult<Match[]> GetMatchHistory(string sessionToken)
    {
        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(sessionToken);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match[]>(playerResult.Status);
        }

        return _matches.LoadMatchHistory(playerResult.Value.AccountId);
    }

    public MatchResult<MatchMove[]> GetMoveHistory(string sessionToken, long matchId)
    {
        MatchResult<MatchSessionContext> resolved = ResolveSessionAndMatch(sessionToken, matchId);
        if (resolved.Value == null)
        {
            return new MatchResult<MatchMove[]>(resolved.Status);
        }

        Match match = resolved.Value.Match;
        MatchMove[]? moveSnapshots = null;
        lock (match.SyncRoot)
        {
            if (GameplayMatchRules.IsParticipant(match, resolved.Value.Player.AccountId))
            {
                moveSnapshots = match.Moves
                    .Select(move => move.CreateSnapshot())
                    .ToArray();
            }
        }

        if (moveSnapshots == null)
        {
            return new MatchResult<MatchMove[]>(MatchStatus.Unauthorized);
        }

        return new MatchResult<MatchMove[]>(moveSnapshots);
    }

    private MatchResult<Match> ApplyPlayerMoveUnderLock(
        Match match,
        long accountId,
        MatchPlacement placement)
    {
        MatchStatus moveStatus;
        Match? snapshot = null;

        lock (match.SyncRoot)
        {
            moveStatus = ValidateAndApplyPlayerMove(match, accountId, placement);
            if (moveStatus == MatchStatus.Success)
            {
                snapshot = match.CreateSnapshot();
            }
        }

        if (snapshot == null)
        {
            return new MatchResult<Match>(moveStatus);
        }

        return new MatchResult<Match>(snapshot);
    }

    private MatchStatus ValidateAndApplyPlayerMove(
        Match match,
        long accountId,
        MatchPlacement placement)
    {
        MatchResult<MatchParticipant> validationResult = GameplayMatchRules.ValidatePlacement(
            match,
            accountId,
            placement);
        if (validationResult.Status != MatchStatus.Success)
        {
            return validationResult.Status;
        }

        return GameplayMatchRules.ApplyPlayerMove(match, placement, _clock.UtcNow);
    }

    private List<Match> CollectExpiredMatches(DateTime now)
    {
        List<Match> expiredMatches = new List<Match>();
        lock (_activeMatchesSync)
        {
            foreach (Match match in _activeMatches.Values)
            {
                if (!string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
                {
                    continue;
                }

                DateTime turnStart = match.TurnStartedAtUtc ?? match.StartedAtUtc ?? now;
                if (now >= turnStart.AddSeconds(match.TurnDurationSeconds))
                {
                    expiredMatches.Add(match);
                }
            }
        }

        return expiredMatches;
    }

    private Match? ApplyTimeoutIfExpired(Match match, DateTime now)
    {
        Match? snapshot = null;
        lock (match.SyncRoot)
        {
            if (GameplayMatchRules.ApplyTimeoutIfExpired(match, now))
            {
                snapshot = match.CreateSnapshot();
            }
        }

        if (snapshot == null)
        {
            return null;
        }

        MarkPersistencePending(match.Id);
        return snapshot;
    }

    private MatchResult<Match> FindTrackedMatch(long matchId)
    {
        Match? match;
        lock (_activeMatchesSync)
        {
            _activeMatches.TryGetValue(matchId, out match);
        }

        MatchResult<Match> result = match == null
            ? new MatchResult<Match>(MatchStatus.MatchNotFound)
            : new MatchResult<Match>(match);
        return result;
    }

    private void MarkPersistencePending(long matchId)
    {
        lock (_activeMatchesSync)
        {
            _matchesPendingPersistence.Add(matchId);
        }
    }

    private bool SynchronizeMatch(Match snapshot, DateTime attemptUtc)
    {
        if (!_matches.SynchronizeMatch(snapshot))
        {
            lock (_activeMatchesSync)
            {
                _matchesPendingPersistence.Add(snapshot.Id);
                _nextPersistenceRetryUtc = attemptUtc.Add(PersistenceRetryInterval);
            }

            return false;
        }

        MatchResult<Match> lookup = FindTrackedMatch(snapshot.Id);
        if (lookup.Value is not Match currentMatch)
        {
            return true;
        }

        bool sameState;
        bool isFinished;
        lock (currentMatch.SyncRoot)
        {
            sameState = RepresentsSameState(currentMatch, snapshot);
            isFinished = string.Equals(
                currentMatch.State,
                StateFinished,
                StringComparison.Ordinal);
        }

        if (!sameState)
        {
            return true;
        }

        lock (_activeMatchesSync)
        {
            if (_activeMatches.TryGetValue(snapshot.Id, out Match? trackedMatch)
                && ReferenceEquals(trackedMatch, currentMatch))
            {
                _matchesPendingPersistence.Remove(snapshot.Id);
                if (isFinished)
                {
                    _activeMatches.Remove(snapshot.Id);
                }
            }
        }

        return true;
    }

    private MatchResult<MatchSessionContext> ResolveSessionAndMatch(
        string sessionToken,
        long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(sessionToken);
        if (playerResult.Value == null)
        {
            return new MatchResult<MatchSessionContext>(playerResult.Status);
        }

        MatchResult<Match> loadResult = GetOrLoadActiveMatch(matchId);
        if (loadResult.Value == null)
        {
            return new MatchResult<MatchSessionContext>(loadResult.Status);
        }

        MatchSessionContext context = new MatchSessionContext(
            playerResult.Value,
            loadResult.Value);
        return new MatchResult<MatchSessionContext>(context);
    }

    private MatchResult<Match> GetOrLoadActiveMatch(long matchId)
    {
        if (matchId <= 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchId);
        }

        Match? cachedMatch;
        lock (_activeMatchesSync)
        {
            _activeMatches.TryGetValue(matchId, out cachedMatch);
        }

        if (cachedMatch != null)
        {
            return new MatchResult<Match>(cachedMatch);
        }

        MatchResult<Match> loadResult = _matches.LoadMatch(matchId);
        if (loadResult.Value == null)
        {
            return loadResult;
        }

        Match loaded = loadResult.Value;
        if (!string.Equals(loaded.State, StateInProgress, StringComparison.Ordinal))
        {
            return new MatchResult<Match>(loaded);
        }

        Match activeMatch;
        lock (_activeMatchesSync)
        {
            if (_activeMatches.TryGetValue(loaded.Id, out Match? existingMatch))
            {
                activeMatch = existingMatch;
            }
            else
            {
                _activeMatches.Add(loaded.Id, loaded);
                activeMatch = loaded;
            }
        }

        return new MatchResult<Match>(activeMatch);
    }

    private MatchResult<AuthenticatedSession> ResolveActiveSession(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return new MatchResult<AuthenticatedSession>(MatchStatus.MissingSessionToken);
        }

        if (!_sessions.TryGet(sessionToken, out AuthenticatedSession? player)
            || player == null
            || player.ExpiresAtUtc <= _clock.UtcNow)
        {
            return new MatchResult<AuthenticatedSession>(MatchStatus.InvalidSession);
        }

        return new MatchResult<AuthenticatedSession>(player);
    }

    private static bool RepresentsSameState(Match currentMatch, Match snapshot)
    {
        bool sameVersion = currentMatch.MatchStateVersion == snapshot.MatchStateVersion;
        bool sameLifecycleState = string.Equals(
            currentMatch.State,
            snapshot.State,
            StringComparison.Ordinal);
        bool sameTurn = currentMatch.CurrentTurnSeatNumber == snapshot.CurrentTurnSeatNumber
            && currentMatch.TurnStartedAtUtc == snapshot.TurnStartedAtUtc;
        bool sameEndTimestamp = currentMatch.EndedAtUtc == snapshot.EndedAtUtc;

        return sameVersion && sameLifecycleState && sameTurn && sameEndTimestamp;
    }

    private sealed class MatchSessionContext
    {
        internal MatchSessionContext(AuthenticatedSession player, Match match)
        {
            Player = player;
            Match = match;
        }

        internal AuthenticatedSession Player { get; }

        internal Match Match { get; }
    }
}
