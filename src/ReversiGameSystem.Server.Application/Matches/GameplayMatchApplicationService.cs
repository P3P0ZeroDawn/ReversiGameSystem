using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>
/// Orchestrates authoritative in-memory gameplay, turn timeouts, and asynchronous persistence sync.
/// </summary>
public sealed class GameplayMatchApplicationService
{
    private const int MaximumConsecutiveTimeouts = 3;
    private const int BlackSeatNumber = ReversiEngine.Black;
    private const int WhiteSeatNumber = ReversiEngine.White;
    private const string StateInProgress = "InProgress";
    private const string StateFinished = "Finished";
    private const string MoveOriginPlayer = "Player";
    private const string MoveOriginTimeout = "Timeout";
    private const string EndReasonNormal = "NORMAL";
    private const string EndReasonInactivity = "INACTIVITY";
    private const string EndReasonAbandonment = "ABANDONMENT";
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

    /// <summary>Initializes active board state and turn tracking in memory for a started match.</summary>
    public void TrackStartedMatch(Match match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        lock (match.SyncRoot)
        {
            match.Board = ReversiEngine.CreateInitialBoard();
            match.MatchStateVersion = 0;
            match.CurrentTurnSeatNumber = BlackSeatNumber;
            match.TurnStartedAtUtc = match.StartedAtUtc ?? _clock.UtcNow;
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

        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success || playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(placement.MatchId);
        if (loadResult.Status != MatchStatus.Success || loadResult.Value == null)
        {
            return loadResult;
        }

        Match match = loadResult.Value;
        MatchResult<Match> transitionResult = ExecuteMoveUnderLock(
            match,
            playerResult.Value.AccountId,
            placement);
        if (transitionResult.Status == MatchStatus.Success)
        {
            MarkPersistencePending(match.Id);
        }

        return transitionResult;
    }

    /// <summary>
    /// Evaluates expired turns in memory without blocking on persistence and returns transition snapshots.
    /// </summary>
    public Match[] ProcessTurnTimeouts()
    {
        DateTime now = _clock.UtcNow;
        List<Match> expiredMatches = CollectExpiredMatches(now);
        List<Match> updatedSnapshots = new List<Match>();

        for (int i = 0; i < expiredMatches.Count; i++)
        {
            Match? snapshot = ProcessSingleTimeout(expiredMatches[i], now);
            if (snapshot != null)
            {
                updatedSnapshots.Add(snapshot);
            }
        }

        return updatedSnapshots.ToArray();
    }

    /// <summary>
    /// Retries pending snapshot writes, backing off for 5 seconds after any persistence failure.
    /// </summary>
    public void SynchronizePendingMatches()
    {
        SynchronizePendingMatches(_clock.UtcNow);
    }

    public MatchResult<Match> ForfeitMatch(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success || playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(matchId);
        if (loadResult.Status != MatchStatus.Success || loadResult.Value == null)
        {
            return loadResult;
        }

        AuthenticatedSession player = playerResult.Value;
        Match match = loadResult.Value;

        DateTime now = _clock.UtcNow;
        bool newlyFinished = false;
        Match snapshot;

        lock (match.SyncRoot)
        {
            MatchParticipant? participant = FindParticipant(match, player.AccountId);
            if (participant == null)
            {
                return new MatchResult<Match>(MatchStatus.Unauthorized);
            }

            if (string.Equals(match.State, StateFinished, StringComparison.Ordinal))
            {
                return new MatchResult<Match>(CopyMatch(match));
            }

            if (!string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
            {
                return new MatchResult<Match>(MatchStatus.MatchNotInProgress);
            }

            int winnerSeat = GetOpponentSeat(participant.SeatNumber);
            ConcludeMatch(
                match,
                EndReasonAbandonment,
                now,
                winnerSeat,
                participant.SeatNumber);
            newlyFinished = true;
            snapshot = CopyMatch(match);
        }

        if (newlyFinished)
        {
            MarkPersistencePending(match.Id);
        }

        return new MatchResult<Match>(snapshot);
    }

    public MatchResult<Match> GetMatchInformation(string sessionToken, long matchId)
    {
        return GetAuthorizedMatchSnapshot(sessionToken, matchId);
    }

    public MatchResult<Match> GetMatchState(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(matchId);
        if (loadResult.Status != MatchStatus.Success || loadResult.Value == null)
        {
            return loadResult;
        }

        return new MatchResult<Match>(SnapshotMatch(loadResult.Value));
    }

    public MatchResult<Match> ObserveMatch(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(matchId);
        if (loadResult.Status != MatchStatus.Success)
        {
            return loadResult;
        }

        Match match = loadResult.Value
            ?? throw new InvalidOperationException(
                "A loaded match result must contain its value.");

        lock (match.SyncRoot)
        {
            if (!string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
            {
                return new MatchResult<Match>(MatchStatus.SpectatingUnavailable);
            }

            return new MatchResult<Match>(CopyMatch(match));
        }
    }

    public MatchResult<Match[]> GetMatchHistory(string sessionToken)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match[]>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException(
                "A successful player result must contain its value.");

        return _matches.LoadMatchHistory(player.AccountId);
    }

    public MatchResult<Match> GetMatchDetails(string sessionToken, long matchId)
    {
        return GetAuthorizedMatchSnapshot(sessionToken, matchId);
    }

    public MatchResult<MatchMove[]> GetMoveHistory(string sessionToken, long matchId)
    {
        MatchResult<Match> snapshotResult = GetAuthorizedMatchSnapshot(sessionToken, matchId);
        if (snapshotResult.Status != MatchStatus.Success || snapshotResult.Value == null)
        {
            return new MatchResult<MatchMove[]>(snapshotResult.Status);
        }

        return new MatchResult<MatchMove[]>(snapshotResult.Value.Moves.ToArray());
    }

    private MatchResult<Match> ExecuteMoveUnderLock(
        Match match,
        long accountId,
        MatchPlacement placement)
    {
        DateTime now = _clock.UtcNow;

        lock (match.SyncRoot)
        {
            MatchResult<MatchParticipant> validationResult = ValidatePlacement(
                match,
                accountId,
                placement);
            if (validationResult.Status != MatchStatus.Success || validationResult.Value == null)
            {
                return new MatchResult<Match>(validationResult.Status);
            }

            MatchParticipant participant = validationResult.Value;
            int[] flipped = ReversiEngine.ApplyMove(
                match.Board,
                placement.Row,
                placement.Column,
                participant.SeatNumber);
            if (flipped.Length == 0)
            {
                return new MatchResult<Match>(MatchStatus.InvalidPosition);
            }

            participant.ConsecutiveTimeoutCount = 0;
            RecordCommittedMove(
                match,
                participant.SeatNumber,
                placement.Row,
                placement.Column,
                flipped.Length,
                now,
                MoveOriginPlayer);
            AdvanceTurnOrConclude(match, participant.SeatNumber, now);

            return new MatchResult<Match>(CopyMatch(match));
        }
    }

    private static MatchResult<MatchParticipant> ValidatePlacement(
        Match match,
        long accountId,
        MatchPlacement placement)
    {
        if (!string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
        {
            return new MatchResult<MatchParticipant>(MatchStatus.MatchNotInProgress);
        }

        MatchParticipant? participant = FindParticipant(match, accountId);
        if (participant == null)
        {
            return new MatchResult<MatchParticipant>(MatchStatus.Unauthorized);
        }

        if (match.CurrentTurnSeatNumber != participant.SeatNumber)
        {
            return new MatchResult<MatchParticipant>(MatchStatus.NotPlayersTurn);
        }

        if (match.MatchStateVersion != placement.ExpectedVersion)
        {
            return new MatchResult<MatchParticipant>(MatchStatus.StaleMatchStateVersion);
        }

        return new MatchResult<MatchParticipant>(participant);
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

    private Match? ProcessSingleTimeout(Match match, DateTime now)
    {
        Match snapshot;
        lock (match.SyncRoot)
        {
            MatchParticipant? actingParticipant = FindExpiredTurnParticipant(match, now);
            if (actingParticipant == null)
            {
                return null;
            }

            actingParticipant.ConsecutiveTimeoutCount++;
            if (actingParticipant.ConsecutiveTimeoutCount >= MaximumConsecutiveTimeouts)
            {
                int winnerSeat = GetOpponentSeat(actingParticipant.SeatNumber);
                ConcludeMatch(
                    match,
                    EndReasonInactivity,
                    now,
                    winnerSeat,
                    actingParticipant.SeatNumber);
            }
            else
            {
                ApplyTimeoutAutoMove(match, actingParticipant.SeatNumber, now);
            }

            snapshot = CopyMatch(match);
        }

        MarkPersistencePending(match.Id);
        return snapshot;
    }

    private static MatchParticipant? FindExpiredTurnParticipant(Match match, DateTime now)
    {
        if (!string.Equals(match.State, StateInProgress, StringComparison.Ordinal)
            || !match.CurrentTurnSeatNumber.HasValue)
        {
            return null;
        }

        DateTime turnStartedAtUtc = match.TurnStartedAtUtc ?? match.StartedAtUtc ?? now;
        if (now < turnStartedAtUtc.AddSeconds(match.TurnDurationSeconds))
        {
            return null;
        }

        int currentSeat = match.CurrentTurnSeatNumber.Value;
        return match.Participants.FirstOrDefault(p => p.SeatNumber == currentSeat);
    }

    private static void ApplyTimeoutAutoMove(Match match, int currentSeat, DateTime now)
    {
        KeyValuePair<int, int>? autoCoord = FindLegalPlacement(match.Board, currentSeat);
        if (!autoCoord.HasValue)
        {
            ConcludeByBoardScore(match, now);
            return;
        }

        int row = autoCoord.Value.Key;
        int col = autoCoord.Value.Value;
        int[] flipped = ReversiEngine.ApplyMove(match.Board, row, col, currentSeat);
        RecordCommittedMove(
            match,
            currentSeat,
            row,
            col,
            flipped.Length,
            now,
            MoveOriginTimeout);
        AdvanceTurnOrConclude(match, currentSeat, now);
    }

    private static void RecordCommittedMove(
        Match match,
        int seatNumber,
        int row,
        int column,
        int flippedPieceCount,
        DateTime playedAtUtc,
        string origin)
    {
        match.MatchStateVersion++;
        MatchMove committedMove = new MatchMove
        {
            MoveNumber = (int)match.MatchStateVersion,
            SeatNumber = seatNumber,
            Row = row,
            Column = column,
            PlayedAtUtc = playedAtUtc,
            FlippedPieceCount = flippedPieceCount,
            Origin = origin
        };

        match.Moves.Add(committedMove);
        match.LastMove = committedMove;
    }

    private static void AdvanceTurnOrConclude(Match match, int actingSeat, DateTime now)
    {
        int opponentSeat = GetOpponentSeat(actingSeat);
        if (ReversiEngine.HasValidMoves(match.Board, opponentSeat))
        {
            match.CurrentTurnSeatNumber = opponentSeat;
            match.TurnStartedAtUtc = now;
            match.SkippedSeatNumber = null;
            return;
        }

        if (ReversiEngine.HasValidMoves(match.Board, actingSeat))
        {
            match.CurrentTurnSeatNumber = actingSeat;
            match.TurnStartedAtUtc = now;
            match.SkippedSeatNumber = opponentSeat;
            return;
        }

        ConcludeByBoardScore(match, now);
    }

    private static void ConcludeByBoardScore(Match match, DateTime now)
    {
        int blackPieceCount = CountPieces(match.Board, ReversiEngine.Black);
        int whitePieceCount = CountPieces(match.Board, ReversiEngine.White);
        int? winnerSeat = DetermineWinner(blackPieceCount, whitePieceCount);
        ConcludeMatch(match, EndReasonNormal, now, winnerSeat, null);
    }

    private void SynchronizePendingMatches(DateTime now)
    {
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
            Match? match = GetTrackedMatchOrNull(pendingMatchIds[index]);
            if (match != null)
            {
                SynchronizeMatch(SnapshotMatch(match), now);
            }
        }
    }

    private Match? GetTrackedMatchOrNull(long matchId)
    {
        lock (_activeMatchesSync)
        {
            _activeMatches.TryGetValue(matchId, out Match? match);
            return match;
        }
    }

    private void MarkPersistencePending(long matchId)
    {
        lock (_activeMatchesSync)
        {
            _matchesPendingPersistence.Add(matchId);
        }
    }

    private void SynchronizeMatch(Match snapshot, DateTime attemptUtc)
    {
        bool synchronized = _matches.SynchronizeMatch(snapshot);
        lock (_activeMatchesSync)
        {
            if (!synchronized)
            {
                _matchesPendingPersistence.Add(snapshot.Id);
                _nextPersistenceRetryUtc = attemptUtc.Add(PersistenceRetryInterval);
                return;
            }

            if (_activeMatches.TryGetValue(snapshot.Id, out Match? currentMatch))
            {
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

                if (sameState)
                {
                    _matchesPendingPersistence.Remove(snapshot.Id);
                    if (isFinished)
                    {
                        _activeMatches.Remove(snapshot.Id);
                    }
                }
            }
        }
    }

    private MatchResult<Match> GetAuthorizedMatchSnapshot(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success || playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(matchId);
        if (loadResult.Status != MatchStatus.Success || loadResult.Value == null)
        {
            return loadResult;
        }

        AuthenticatedSession player = playerResult.Value;
        Match match = loadResult.Value;

        lock (match.SyncRoot)
        {
            if (FindParticipant(match, player.AccountId) == null)
            {
                return new MatchResult<Match>(MatchStatus.Unauthorized);
            }

            return new MatchResult<Match>(CopyMatch(match));
        }
    }

    private MatchResult<Match> GetOrCreateActiveMatch(long matchId)
    {
        if (matchId <= 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchId);
        }

        lock (_activeMatchesSync)
        {
            if (_activeMatches.TryGetValue(matchId, out Match? cachedMatch))
            {
                return new MatchResult<Match>(cachedMatch);
            }
        }

        MatchResult<Match> loadResult = _matches.LoadMatch(matchId);
        if (loadResult.Status != MatchStatus.Success || loadResult.Value == null)
        {
            return loadResult;
        }

        Match loaded = loadResult.Value;
        if (!string.Equals(loaded.State, StateInProgress, StringComparison.Ordinal))
        {
            return new MatchResult<Match>(loaded);
        }

        lock (_activeMatchesSync)
        {
            if (_activeMatches.TryGetValue(loaded.Id, out Match? existingMatch))
            {
                return new MatchResult<Match>(existingMatch);
            }

            _activeMatches.Add(loaded.Id, loaded);
        }

        return new MatchResult<Match>(loaded);
    }

    private MatchResult<AuthenticatedSession> GetPlayer(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new MatchResult<AuthenticatedSession>(MatchStatus.MissingSessionToken);
        }

        if (!_sessions.TryGet(token, out AuthenticatedSession? player)
            || player == null
            || player.ExpiresAtUtc <= _clock.UtcNow)
        {
            return new MatchResult<AuthenticatedSession>(MatchStatus.InvalidSession);
        }

        return new MatchResult<AuthenticatedSession>(player);
    }

    private static Match SnapshotMatch(Match match)
    {
        lock (match.SyncRoot)
        {
            return CopyMatch(match);
        }
    }

    private static bool RepresentsSameState(Match currentMatch, Match snapshot)
    {
        return currentMatch.MatchStateVersion == snapshot.MatchStateVersion
            && string.Equals(currentMatch.State, snapshot.State, StringComparison.Ordinal)
            && currentMatch.CurrentTurnSeatNumber == snapshot.CurrentTurnSeatNumber
            && currentMatch.TurnStartedAtUtc == snapshot.TurnStartedAtUtc
            && currentMatch.EndedAtUtc == snapshot.EndedAtUtc;
    }

    private static Match CopyMatch(Match source)
    {
        return new Match
        {
            Id = source.Id,
            Code = source.Code,
            State = source.State,
            TurnDurationSeconds = source.TurnDurationSeconds,
            CountsTowardStatistics = source.CountsTowardStatistics,
            StartedAtUtc = source.StartedAtUtc,
            Participants = source.Participants
                .Select(participant => new MatchParticipant
                {
                    SeatNumber = participant.SeatNumber,
                    AccountId = participant.AccountId,
                    DisplayName = participant.DisplayName,
                    ConsecutiveTimeoutCount = participant.ConsecutiveTimeoutCount
                })
                .ToArray(),
            IsPrivate = source.IsPrivate,
            Board = (int[])source.Board.Clone(),
            MatchStateVersion = source.MatchStateVersion,
            CurrentTurnSeatNumber = source.CurrentTurnSeatNumber,
            TurnStartedAtUtc = source.TurnStartedAtUtc,
            LastMove = source.LastMove == null ? null : CopyMove(source.LastMove),
            SkippedSeatNumber = source.SkippedSeatNumber,
            Moves = source.Moves.Select(CopyMove).ToList(),
            EndReason = source.EndReason,
            WinnerSeatNumber = source.WinnerSeatNumber,
            AbandoningSeatNumber = source.AbandoningSeatNumber,
            EndedAtUtc = source.EndedAtUtc
        };
    }

    private static MatchMove CopyMove(MatchMove source)
    {
        return new MatchMove
        {
            MoveNumber = source.MoveNumber,
            SeatNumber = source.SeatNumber,
            Row = source.Row,
            Column = source.Column,
            PlayedAtUtc = source.PlayedAtUtc,
            FlippedPieceCount = source.FlippedPieceCount,
            Origin = source.Origin
        };
    }

    private static void ConcludeMatch(
        Match match,
        string endReason,
        DateTime endedAtUtc,
        int? winnerSeatNumber,
        int? abandoningSeatNumber)
    {
        match.State = StateFinished;
        match.EndReason = endReason;
        match.EndedAtUtc = endedAtUtc;
        match.CurrentTurnSeatNumber = null;
        match.TurnStartedAtUtc = null;
        match.SkippedSeatNumber = null;
        match.WinnerSeatNumber = winnerSeatNumber;
        match.AbandoningSeatNumber = abandoningSeatNumber;
    }

    private static MatchParticipant? FindParticipant(Match match, long accountId)
    {
        return match.Participants.FirstOrDefault(current => current.AccountId == accountId);
    }

    private static int GetOpponentSeat(int seatNumber)
    {
        return seatNumber == BlackSeatNumber ? WhiteSeatNumber : BlackSeatNumber;
    }

    private static KeyValuePair<int, int>? FindLegalPlacement(int[] board, int disc)
    {
        for (int row = 0; row < ReversiEngine.BoardSize; row++)
        {
            for (int col = 0; col < ReversiEngine.BoardSize; col++)
            {
                if (ReversiEngine.IsLegalMove(board, row, col, disc))
                {
                    return new KeyValuePair<int, int>(row, col);
                }
            }
        }

        return null;
    }

    private static int? DetermineWinner(int blackCount, int whiteCount)
    {
        if (blackCount > whiteCount)
        {
            return BlackSeatNumber;
        }

        if (whiteCount > blackCount)
        {
            return WhiteSeatNumber;
        }

        return null;
    }

    private static int CountPieces(int[] board, int disc)
    {
        int count = 0;
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == disc)
            {
                count++;
            }
        }

        return count;
    }
}
