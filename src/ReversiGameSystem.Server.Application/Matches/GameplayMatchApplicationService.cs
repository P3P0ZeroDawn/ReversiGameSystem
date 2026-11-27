using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Application.Matches;

public sealed class GameplayMatchApplicationService
{
    private const string StateInProgress = "InProgress";
    private const string StateFinished = "Finished";
    private static readonly TimeSpan PersistenceRetryInterval = TimeSpan.FromSeconds(5);

    private readonly SessionStore _sessions;
    private readonly IGameplayRepository _matches;
    private readonly IUtcClock _clock;
    private readonly ILogger<GameplayMatchApplicationService> _logger;
    private readonly object _activeMatchesSync = new object();
    private readonly Dictionary<long, Match> _activeMatches = new Dictionary<long, Match>();
    private readonly HashSet<long> _matchesPendingPersistence = new HashSet<long>();
    private readonly object _observationsSync = new object();
    private readonly Dictionary<string, HashSet<long>> _observations =
        new Dictionary<string, HashSet<long>>(StringComparer.Ordinal);
    private DateTime _nextPersistenceRetryUtc;

    public GameplayMatchApplicationService(
        SessionStore sessions,
        IGameplayRepository matches,
        IUtcClock clock,
        ILogger<GameplayMatchApplicationService> logger)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        _logger.LogInformation("Gameplay tracking started for match {MatchId}.", match.Id);
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
            resolved.Value.Player,
            placement);
        if (transitionResult.Status == MatchStatus.Success)
        {
            MarkPersistencePending(match.Id);
            if (transitionResult.Value != null
                && string.Equals(transitionResult.Value.State, StateFinished, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "Match {MatchId} finished with reason {EndReason} and winner seat {WinnerSeatNumber}.",
                    transitionResult.Value.Id,
                    transitionResult.Value.EndReason,
                    transitionResult.Value.WinnerSeatNumber);
            }
        }
        else if (transitionResult.Status == MatchStatus.Unauthorized)
        {
            _logger.LogWarning(
                "Piece placement rejected for match {MatchId} by {Actor}: {Reason}.",
                placement.MatchId,
                FormatActor(resolved.Value.Player),
                transitionResult.Status);
        }

        return transitionResult;
    }

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

        Match match = resolved.Value.Match;
        MatchParticipant? participant;
        lock (match.SyncRoot)
        {
            participant = Array.Find(match.Participants, candidate =>
                resolved.Value.Player.IsParticipant(matchId, candidate));
        }
        if (participant == null)
        {
            _logger.LogWarning(
                "Match forfeit rejected for match {MatchId} by {Actor}: {Reason}.",
                matchId,
                FormatActor(resolved.Value.Player),
                MatchStatus.Unauthorized);
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }
        return ConcludeForfeit(match, participant.SeatNumber, true);
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

        return ConcludeForfeit(match, participant.SeatNumber, false);
    }

    private MatchResult<Match> ConcludeForfeit(
        Match match,
        int seatNumber,
        bool includeFinishedMatch)
    {
        DateTime now = _clock.UtcNow;
        MatchStatus forfeitStatus;
        Match? snapshot = null;

        lock (match.SyncRoot)
        {
            forfeitStatus = GameplayMatchRules.ForfeitByParticipant(
                match,
                seatNumber,
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
            if (includeFinishedMatch)
            {
                _logger.LogInformation(
                    "Match {MatchId} forfeited by seat {SeatNumber}; winner seat {WinnerSeatNumber}.",
                    snapshot.Id,
                    seatNumber,
                    snapshot.WinnerSeatNumber);
            }
            else
            {
                _logger.LogWarning(
                    "Match {MatchId} forfeited after 30s disconnection timeout for seat {SeatNumber}; winner seat {WinnerSeatNumber}.",
                    snapshot.Id,
                    seatNumber,
                    snapshot.WinnerSeatNumber);
            }
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
            if (CanReadMatch(match, resolved.Value.Player, sessionToken))
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
        return GetAuthorizedMatchSnapshot(sessionToken, matchId);
    }

    public MatchResult<Match> ObserveMatch(string sessionToken, long matchId)
    {
        MatchResult<MatchSessionContext> resolved = ResolveSessionAndMatch(sessionToken, matchId);
        if (resolved.Value == null)
        {
            return new MatchResult<Match>(resolved.Status);
        }

        if (resolved.Value.Player.IsGuest)
        {
            _logger.LogWarning(
                "Match observation rejected for guest {Actor} on match {MatchId}.",
                FormatActor(resolved.Value.Player),
                matchId);
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }
        MatchStatus access = _matches.EvaluateLobbyAccess(resolved.Value.Player.AccountId!.Value);
        if (access != MatchStatus.Success)
        {
            return new MatchResult<Match>(access);
        }

        PruneObservations();
        Match match = resolved.Value.Match;
        Match? snapshot = null;
        lock (match.SyncRoot)
        {
            if (string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
            {
                RegisterObservation(sessionToken, matchId);
                snapshot = match.CreateSnapshot();
            }
        }

        if (snapshot == null)
        {
            return new MatchResult<Match>(MatchStatus.SpectatingUnavailable);
        }

        _logger.LogInformation(
            "Observer {Actor} joined match {MatchId}.",
            FormatActor(resolved.Value.Player),
            matchId);
        return new MatchResult<Match>(snapshot);
    }

    public MatchResult<Match[]> GetObservableMatches(string sessionToken)
    {
        MatchResult<AuthenticatedSession> player = ResolveActiveSession(sessionToken);
        if (player.Value == null)
        {
            return new MatchResult<Match[]>(player.Status);
        }

        Match[] trackedBeforeLoad;
        lock (_activeMatchesSync)
        {
            trackedBeforeLoad = _activeMatches.Values.ToArray();
        }

        MatchResult<Match[]> persisted = _matches.GetObservableMatches(player.Value.AccountId);
        if (persisted.Value == null)
        {
            return persisted;
        }

        Dictionary<long, Match> candidates = persisted.Value.ToDictionary(match => match.Id);
        Match[] tracked;
        lock (_activeMatchesSync)
        {
            tracked = trackedBeforeLoad.Concat(_activeMatches.Values).Distinct().ToArray();
        }

        foreach (Match match in tracked)
        {
            lock (match.SyncRoot)
            {
                candidates[match.Id] = match.CreateSnapshot();
            }
        }

        Match[] snapshots = candidates.Values
            .Where(match => string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
            .OrderBy(match => match.Id)
            .ToArray();
        return new MatchResult<Match[]>(snapshots);
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

    public MatchResult<long> GetSessionAccountId(string sessionToken)
    {
        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(sessionToken);
        if (playerResult.Value == null)
        {
            return new MatchResult<long>(playerResult.Status);
        }

        MatchResult<long> result = new MatchResult<long>(playerResult.Value.AccountId);
        return result;
    }

    public MatchResult<MatchParticipant> GetAuthorizedParticipant(
        string sessionToken,
        long matchId)
    {
        MatchResult<MatchSessionContext> resolved = ResolveSessionAndMatch(sessionToken, matchId);
        if (resolved.Value == null)
        {
            return new MatchResult<MatchParticipant>(resolved.Status);
        }

        Match match = resolved.Value.Match;
        MatchPlayer player = resolved.Value.Player;
        MatchParticipant? participant = null;
        lock (match.SyncRoot)
        {
            participant = Array.Find(
                match.Participants,
                candidate => player.IsParticipant(matchId, candidate));
        }

        if (participant == null)
        {
            return new MatchResult<MatchParticipant>(MatchStatus.Unauthorized);
        }

        MatchResult<MatchParticipant> result = new MatchResult<MatchParticipant>(participant);
        return result;
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
            if (CanReadMatch(match, resolved.Value.Player, sessionToken))
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
        MatchPlayer player,
        MatchPlacement placement)
    {
        MatchStatus moveStatus;
        Match? snapshot = null;

        lock (match.SyncRoot)
        {
            moveStatus = ValidateAndApplyPlayerMove(match, player, placement);
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
        MatchPlayer player,
        MatchPlacement placement)
    {
        MatchParticipant? participant = Array.Find(match.Participants,
            candidate => player.IsParticipant(match.Id, candidate));
        if (participant == null)
        {
            return MatchStatus.Unauthorized;
        }
        MatchResult<MatchParticipant> validationResult = GameplayMatchRules.ValidatePlacement(
            match,
            participant.SeatNumber,
            placement);
        if (validationResult.Status != MatchStatus.Success)
        {
            return validationResult.Status;
        }

        return GameplayMatchRules.ApplyPlayerMove(match, placement, _clock.UtcNow);
    }

    private List<Match> CollectExpiredMatches(DateTime now)
    {
        Match[] activeMatches;
        lock (_activeMatchesSync)
        {
            activeMatches = _activeMatches.Values.ToArray();
        }

        List<Match> expiredMatches = new List<Match>();
        for (int index = 0; index < activeMatches.Length; index++)
        {
            Match match = activeMatches[index];
            bool isExpired = false;
            lock (match.SyncRoot)
            {
                if (string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
                {
                    DateTime turnStart = match.TurnStartedAtUtc ?? match.StartedAtUtc ?? now;
                    MatchParticipant? participant = Array.Find(
                        match.Participants,
                        player => player.SeatNumber == match.CurrentTurnSeatNumber);
                    TimeSpan duration = participant != null && participant.IsSystem
                        ? GameplayMatchRules.SystemMoveDelay
                        : TimeSpan.FromSeconds(match.TurnDurationSeconds);
                    isExpired = now >= turnStart.Add(duration);
                }
            }

            if (isExpired)
            {
                expiredMatches.Add(match);
            }
        }

        return expiredMatches;
    }

    private Match? ApplyTimeoutIfExpired(Match match, DateTime now)
    {
        Match? snapshot = null;
        lock (match.SyncRoot)
        {
            if (GameplayMatchRules.ApplySystemMoveIfReady(match, now)
                || GameplayMatchRules.ApplyTimeoutIfExpired(match, now))
            {
                snapshot = match.CreateSnapshot();
            }
        }

        if (snapshot == null)
        {
            return null;
        }

        MarkPersistencePending(match.Id);
        if (string.Equals(snapshot.State, StateFinished, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Match {MatchId} finished on timeout with reason {EndReason} and winner seat {WinnerSeatNumber}.",
                snapshot.Id,
                snapshot.EndReason,
                snapshot.WinnerSeatNumber);
        }

        return snapshot;
    }

    private static string FormatActor(MatchPlayer player)
    {
        return LogSanitizer.FormatMatchActor(
            player.AccountId,
            player.DisplayName,
            player.IsGuest);
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

    private bool CanReadMatch(Match match, MatchPlayer player, string sessionToken)
    {
        bool participant = match.Participants.Any(
            candidate => player.IsParticipant(match.Id, candidate));
        if (player.IsGuest)
        {
            return participant;
        }
        if (participant || string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
        {
            return true;
        }

        lock (_observationsSync)
        {
            HashSet<long>? observed = _observations.ContainsKey(sessionToken)
                ? _observations[sessionToken]
                : null;
            return observed != null && observed.Contains(match.Id);
        }
    }

    private void PruneObservations()
    {
        string[] tokens;
        lock (_observationsSync)
        {
            tokens = _observations.Keys.ToArray();
        }

        string[] expiredTokens = tokens
            .Where(token => _sessions.FindAuthenticated(token).Status != SessionStatus.Success)
            .ToArray();
        lock (_observationsSync)
        {
            foreach (string token in expiredTokens)
            {
                _observations.Remove(token);
            }
        }
    }

    private void RegisterObservation(string sessionToken, long matchId)
    {
        lock (_observationsSync)
        {
            if (!_observations.ContainsKey(sessionToken))
            {
                _observations.Add(sessionToken, new HashSet<long>());
            }

            _observations[sessionToken].Add(matchId);
        }
    }

    private MatchResult<MatchSessionContext> ResolveSessionAndMatch(
        string sessionToken,
        long matchId)
    {
        MatchResult<MatchPlayer> playerResult = MatchPlayer.ResolveSession(_sessions, sessionToken);
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
        SessionResult<AuthenticatedSession> session =
            _sessions.FindAuthenticated(sessionToken);
        if (session.Status == SessionStatus.MissingSessionToken)
        {
            return new MatchResult<AuthenticatedSession>(MatchStatus.MissingSessionToken);
        }

        if (session.Status != SessionStatus.Success || session.Value == null)
        {
            return new MatchResult<AuthenticatedSession>(MatchStatus.InvalidSession);
        }

        return new MatchResult<AuthenticatedSession>(session.Value);
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
        internal MatchSessionContext(MatchPlayer player, Match match)
        {
            Player = player;
            Match = match;
        }

        internal MatchPlayer Player { get; }

        internal Match Match { get; }
    }
}
