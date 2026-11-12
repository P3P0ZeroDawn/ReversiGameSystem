using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>
/// Orchestrates authoritative active match gameplay, turn transitions, timeouts and replay queries.
/// </summary>
public sealed class GameplayMatchApplicationService
{
    private readonly ISessionRepository _sessions;
    private readonly IMatchRepository _matches;
    private readonly IUtcClock _clock;
    private readonly object _activeMatchesSync = new object();
    private readonly Dictionary<long, Match> _activeMatches = new Dictionary<long, Match>();

    /// <summary>
    /// Initializes a new instance of the <see cref="GameplayMatchApplicationService"/> class.
    /// </summary>
    /// <param name="sessions">The authenticated session repository.</param>
    /// <param name="matches">The persistent match repository.</param>
    /// <param name="clock">The authoritative UTC clock.</param>
    /// <exception cref="ArgumentNullException">A required dependency is null.</exception>
    public GameplayMatchApplicationService(
        ISessionRepository sessions,
        IMatchRepository matches,
        IUtcClock clock)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Initializes in-memory tracking for a newly started match.
    /// </summary>
    /// <param name="match">The confirmed match transitioning to InProgress.</param>
    /// <exception cref="ArgumentNullException">The match is null.</exception>
    public void TrackStartedMatch(Match match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        match.Board = ReversiEngine.CreateInitialBoard();
        match.MatchStateVersion = 0;
        match.CurrentTurnSeatNumber = 1;
        match.TurnStartedAtUtc = match.StartedAtUtc ?? _clock.UtcNow;

        lock (_activeMatchesSync)
        {
            _activeMatches[match.Id] = match;
        }
    }

    /// <summary>
    /// Validates and executes an authoritative piece placement against the active match state.
    /// </summary>
    /// <param name="sessionToken">The caller session token.</param>
    /// <param name="placement">The placement coordinates and expected match state version.</param>
    /// <returns>A match result containing the updated match state, or a failure status.</returns>
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
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException("A successful player result must contain its value.");

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(placement.MatchId);
        if (loadResult.Status != MatchStatus.Success)
        {
            return loadResult;
        }

        Match match = loadResult.Value
            ?? throw new InvalidOperationException("A loaded match result must contain its value.");

        MatchMove committedMove;
        int nextConsecutiveTimeouts;
        int? nextTurnSeat;
        DateTime now = _clock.UtcNow;
        bool isConcluded = false;
        int finalBlackCount = 0;
        int finalWhiteCount = 0;
        int? winnerSeat = null;

        lock (match.SyncRoot)
        {
            if (!string.Equals(match.State, "InProgress", StringComparison.Ordinal))
            {
                return new MatchResult<Match>(MatchStatus.MatchNotInProgress);
            }

            MatchParticipant? participant = match.Participants.FirstOrDefault(current => current.AccountId == player.AccountId);
            if (participant == null)
            {
                return new MatchResult<Match>(MatchStatus.Unauthorized);
            }

            if (match.CurrentTurnSeatNumber != participant.SeatNumber)
            {
                return new MatchResult<Match>(MatchStatus.NotPlayersTurn);
            }

            if (match.MatchStateVersion != placement.ExpectedVersion)
            {
                return new MatchResult<Match>(MatchStatus.StaleMatchStateVersion);
            }

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
            nextConsecutiveTimeouts = 0;

            match.MatchStateVersion++;
            committedMove = new MatchMove
            {
                MoveNumber = (int)match.MatchStateVersion,
                SeatNumber = participant.SeatNumber,
                Row = placement.Row,
                Column = placement.Column,
                PlayedAtUtc = now,
                FlippedPieceCount = flipped.Length,
                Origin = "Player"
            };

            match.Moves.Add(committedMove);
            match.LastMove = committedMove;

            int opponentSeat = participant.SeatNumber == 1 ? 2 : 1;
            bool opponentHasMoves = ReversiEngine.HasValidMoves(match.Board, opponentSeat);

            if (opponentHasMoves)
            {
                match.CurrentTurnSeatNumber = opponentSeat;
                match.TurnStartedAtUtc = now;
                match.SkippedSeatNumber = null;
                nextTurnSeat = opponentSeat;
            }
            else
            {
                bool playerHasMoves = ReversiEngine.HasValidMoves(match.Board, participant.SeatNumber);
                if (playerHasMoves)
                {
                    match.CurrentTurnSeatNumber = participant.SeatNumber;
                    match.TurnStartedAtUtc = now;
                    match.SkippedSeatNumber = opponentSeat;
                    nextTurnSeat = participant.SeatNumber;
                }
                else
                {
                    isConcluded = true;
                    match.State = "Finished";
                    match.EndReason = "NORMAL";
                    match.EndedAtUtc = now;
                    match.CurrentTurnSeatNumber = null;
                    match.TurnStartedAtUtc = null;
                    match.SkippedSeatNumber = null;
                    nextTurnSeat = null;

                    finalBlackCount = CountPieces(match.Board, ReversiEngine.Black);
                    finalWhiteCount = CountPieces(match.Board, ReversiEngine.White);
                    winnerSeat = DetermineWinner(finalBlackCount, finalWhiteCount);
                    match.WinnerSeatNumber = winnerSeat;
                }
            }
        }

        if (isConcluded)
        {
            _matches.SaveMoveAndConclude(
                match.Id,
                committedMove,
                nextConsecutiveTimeouts,
                "NORMAL",
                winnerSeat,
                finalBlackCount,
                finalWhiteCount,
                now);
        }
        else if (nextTurnSeat.HasValue)
        {
            _matches.SaveMove(
                match.Id,
                committedMove,
                nextConsecutiveTimeouts,
                nextTurnSeat.Value,
                now);
        }

        return new MatchResult<Match>(match);
    }

    /// <summary>
    /// Processes expired turns across active matches, applying automatic placements or inactivity forfeits.
    /// </summary>
    /// <returns>The matches that transitioned state as a result of turn timeouts.</returns>
    public Match[] ProcessTurnTimeouts()
    {
        DateTime now = _clock.UtcNow;
        List<Match> expiredMatches = new List<Match>();

        lock (_activeMatchesSync)
        {
            foreach (Match match in _activeMatches.Values)
            {
                if (!string.Equals(match.State, "InProgress", StringComparison.Ordinal))
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

        List<Match> updatedMatches = new List<Match>();
        for (int i = 0; i < expiredMatches.Count; i++)
        {
            Match match = expiredMatches[i];
            bool transitioned = ProcessSingleTimeout(match, now);
            if (transitioned)
            {
                updatedMatches.Add(match);
            }
        }

        return updatedMatches.ToArray();
    }

    /// <summary>
    /// Concludes an active match by player abandonment idempotently.
    /// </summary>
    /// <param name="sessionToken">The caller session token.</param>
    /// <param name="matchId">The target match identifier.</param>
    /// <returns>A match result containing the concluded match state.</returns>
    public MatchResult<Match> ForfeitMatch(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException("A successful player result must contain its value.");

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(matchId);
        if (loadResult.Status != MatchStatus.Success)
        {
            return loadResult;
        }

        Match match = loadResult.Value
            ?? throw new InvalidOperationException("A loaded match result must contain its value.");

        int? winnerSeat = null;
        int? forfeitingSeat = null;
        DateTime now = _clock.UtcNow;
        int blackCount = 0;
        int whiteCount = 0;
        bool newlyFinished = false;

        lock (match.SyncRoot)
        {
            MatchParticipant? participant = match.Participants.FirstOrDefault(current => current.AccountId == player.AccountId);
            if (participant == null)
            {
                return new MatchResult<Match>(MatchStatus.Unauthorized);
            }

            if (string.Equals(match.State, "Finished", StringComparison.Ordinal))
            {
                return new MatchResult<Match>(match);
            }

            if (!string.Equals(match.State, "InProgress", StringComparison.Ordinal))
            {
                return new MatchResult<Match>(MatchStatus.MatchNotInProgress);
            }

            forfeitingSeat = participant.SeatNumber;
            winnerSeat = forfeitingSeat == 1 ? 2 : 1;
            blackCount = CountPieces(match.Board, ReversiEngine.Black);
            whiteCount = CountPieces(match.Board, ReversiEngine.White);

            match.State = "Finished";
            match.EndReason = "ABANDONMENT";
            match.AbandoningSeatNumber = forfeitingSeat;
            match.WinnerSeatNumber = winnerSeat;
            match.EndedAtUtc = now;
            match.CurrentTurnSeatNumber = null;
            match.TurnStartedAtUtc = null;
            newlyFinished = true;
        }

        if (newlyFinished)
        {
            _matches.ConcludeMatch(
                match.Id,
                "ABANDONMENT",
                winnerSeat,
                forfeitingSeat,
                blackCount,
                whiteCount,
                now);
        }

        return new MatchResult<Match>(match);
    }

    /// <summary>
    /// Retrieves personalized match information for an authorized participant.
    /// </summary>
    /// <param name="sessionToken">The caller session token.</param>
    /// <param name="matchId">The target match identifier.</param>
    /// <returns>A match result containing the match details.</returns>
    public MatchResult<Match> GetMatchInformation(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException("A successful player result must contain its value.");

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(matchId);
        if (loadResult.Status != MatchStatus.Success)
        {
            return loadResult;
        }

        Match match = loadResult.Value
            ?? throw new InvalidOperationException("A loaded match result must contain its value.");

        if (!match.Participants.Any(current => current.AccountId == player.AccountId))
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        return new MatchResult<Match>(match);
    }

    /// <summary>
    /// Retrieves the current dynamic state snapshot of an active or concluded match.
    /// </summary>
    /// <param name="sessionToken">The caller session token.</param>
    /// <param name="matchId">The target match identifier.</param>
    /// <returns>A match result containing the current match state.</returns>
    public MatchResult<Match> GetMatchState(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        return GetOrCreateActiveMatch(matchId);
    }

    /// <summary>
    /// Authorizes observation access and loads current match state without assigning a player seat.
    /// </summary>
    /// <param name="sessionToken">The caller session token.</param>
    /// <param name="matchId">The target match identifier.</param>
    /// <returns>A match result containing the match for observation.</returns>
    public MatchResult<Match> ObserveMatch(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException("A successful player result must contain its value.");

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(matchId);
        if (loadResult.Status != MatchStatus.Success)
        {
            return loadResult;
        }

        Match match = loadResult.Value
            ?? throw new InvalidOperationException("A loaded match result must contain its value.");

        if (!string.Equals(match.State, "InProgress", StringComparison.Ordinal))
        {
            return new MatchResult<Match>(MatchStatus.SpectatingUnavailable);
        }

        return new MatchResult<Match>(match);
    }

    /// <summary>
    /// Retrieves historical concluded matches in which the caller participated.
    /// </summary>
    /// <param name="sessionToken">The caller session token.</param>
    /// <returns>A match result containing the array of finished matches.</returns>
    public MatchResult<Match[]> GetMatchHistory(string sessionToken)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match[]>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException("A successful player result must contain its value.");

        return _matches.GetMatchHistory(player.AccountId);
    }

    /// <summary>
    /// Retrieves coherent match details, move records and replay data for an authorized participant.
    /// </summary>
    /// <param name="sessionToken">The caller session token.</param>
    /// <param name="matchId">The target match identifier.</param>
    /// <returns>A match result containing the match details.</returns>
    public MatchResult<Match> GetMatchDetails(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException("A successful player result must contain its value.");

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(matchId);
        if (loadResult.Status != MatchStatus.Success)
        {
            return loadResult;
        }

        Match match = loadResult.Value
            ?? throw new InvalidOperationException("A loaded match result must contain its value.");

        if (!match.Participants.Any(current => current.AccountId == player.AccountId))
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        return new MatchResult<Match>(match);
    }

    /// <summary>
    /// Retrieves the ordered placements performed in an authorized match.
    /// </summary>
    /// <param name="sessionToken">The caller session token.</param>
    /// <param name="matchId">The target match identifier.</param>
    /// <returns>A match result containing the array of move records.</returns>
    public MatchResult<MatchMove[]> GetMoveHistory(string sessionToken, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(sessionToken);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<MatchMove[]>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException("A successful player result must contain its value.");

        MatchResult<Match> loadResult = GetOrCreateActiveMatch(matchId);
        if (loadResult.Status != MatchStatus.Success)
        {
            return new MatchResult<MatchMove[]>(loadResult.Status);
        }

        Match match = loadResult.Value
            ?? throw new InvalidOperationException("A loaded match result must contain its value.");

        if (!match.Participants.Any(current => current.AccountId == player.AccountId))
        {
            return new MatchResult<MatchMove[]>(MatchStatus.Unauthorized);
        }

        MatchMove[] movesCopy;
        lock (match.SyncRoot)
        {
            movesCopy = match.Moves.ToArray();
        }

        return new MatchResult<MatchMove[]>(movesCopy);
    }

    private bool ProcessSingleTimeout(Match match, DateTime now)
    {
        MatchMove? timeoutMove = null;
        int nextTimeouts = 0;
        int? nextTurnSeat = null;
        bool isConcluded = false;
        string endReason = "INACTIVITY";
        int? winnerSeat = null;
        int blackCount = 0;
        int whiteCount = 0;

        lock (match.SyncRoot)
        {
            if (!string.Equals(match.State, "InProgress", StringComparison.Ordinal)
                || !match.CurrentTurnSeatNumber.HasValue)
            {
                return false;
            }

            int currentSeat = match.CurrentTurnSeatNumber.Value;
            MatchParticipant? actingParticipant = match.Participants.FirstOrDefault(p => p.SeatNumber == currentSeat);
            if (actingParticipant == null)
            {
                return false;
            }

            actingParticipant.ConsecutiveTimeoutCount++;
            nextTimeouts = actingParticipant.ConsecutiveTimeoutCount;

            if (nextTimeouts >= 3)
            {
                isConcluded = true;
                match.State = "Finished";
                match.EndReason = endReason;
                match.EndedAtUtc = now;
                match.CurrentTurnSeatNumber = null;
                match.TurnStartedAtUtc = null;
                match.SkippedSeatNumber = null;

                winnerSeat = currentSeat == 1 ? 2 : 1;
                match.WinnerSeatNumber = winnerSeat;
                match.AbandoningSeatNumber = currentSeat;
                blackCount = CountPieces(match.Board, ReversiEngine.Black);
                whiteCount = CountPieces(match.Board, ReversiEngine.White);
            }
            else
            {
                KeyValuePair<int, int>? autoCoord = FindLegalPlacement(match.Board, currentSeat);
                if (autoCoord.HasValue)
                {
                    int row = autoCoord.Value.Key;
                    int col = autoCoord.Value.Value;
                    int[] flipped = ReversiEngine.ApplyMove(match.Board, row, col, currentSeat);

                    match.MatchStateVersion++;
                    timeoutMove = new MatchMove
                    {
                        MoveNumber = (int)match.MatchStateVersion,
                        SeatNumber = currentSeat,
                        Row = row,
                        Column = col,
                        PlayedAtUtc = now,
                        FlippedPieceCount = flipped.Length,
                        Origin = "Timeout"
                    };

                    match.Moves.Add(timeoutMove);
                    match.LastMove = timeoutMove;

                    int opponentSeat = currentSeat == 1 ? 2 : 1;
                    bool opponentHasMoves = ReversiEngine.HasValidMoves(match.Board, opponentSeat);

                    if (opponentHasMoves)
                    {
                        match.CurrentTurnSeatNumber = opponentSeat;
                        match.TurnStartedAtUtc = now;
                        match.SkippedSeatNumber = null;
                        nextTurnSeat = opponentSeat;
                    }
                    else
                    {
                        bool playerHasMoves = ReversiEngine.HasValidMoves(match.Board, currentSeat);
                        if (playerHasMoves)
                        {
                            match.CurrentTurnSeatNumber = currentSeat;
                            match.TurnStartedAtUtc = now;
                            match.SkippedSeatNumber = opponentSeat;
                            nextTurnSeat = currentSeat;
                        }
                        else
                        {
                            isConcluded = true;
                            endReason = "NORMAL";
                            match.State = "Finished";
                            match.EndReason = endReason;
                            match.EndedAtUtc = now;
                            match.CurrentTurnSeatNumber = null;
                            match.TurnStartedAtUtc = null;
                            match.SkippedSeatNumber = null;

                            blackCount = CountPieces(match.Board, ReversiEngine.Black);
                            whiteCount = CountPieces(match.Board, ReversiEngine.White);
                            winnerSeat = DetermineWinner(blackCount, whiteCount);
                            match.WinnerSeatNumber = winnerSeat;
                        }
                    }
                }
                else
                {
                    isConcluded = true;
                    endReason = "NORMAL";
                    match.State = "Finished";
                    match.EndReason = endReason;
                    match.EndedAtUtc = now;
                    match.CurrentTurnSeatNumber = null;
                    match.TurnStartedAtUtc = null;
                    match.SkippedSeatNumber = null;

                    blackCount = CountPieces(match.Board, ReversiEngine.Black);
                    whiteCount = CountPieces(match.Board, ReversiEngine.White);
                    winnerSeat = DetermineWinner(blackCount, whiteCount);
                    match.WinnerSeatNumber = winnerSeat;
                }
            }
        }

        if (isConcluded)
        {
            if (timeoutMove != null)
            {
                _matches.SaveMoveAndConclude(
                    match.Id,
                    timeoutMove,
                    nextTimeouts,
                    endReason,
                    winnerSeat,
                    blackCount,
                    whiteCount,
                    now);
            }
            else
            {
                _matches.ConcludeMatch(
                    match.Id,
                    endReason,
                    winnerSeat,
                    match.AbandoningSeatNumber,
                    blackCount,
                    whiteCount,
                    now);
            }
        }
        else if (timeoutMove != null && nextTurnSeat.HasValue)
        {
            _matches.SaveMove(
                match.Id,
                timeoutMove,
                nextTimeouts,
                nextTurnSeat.Value,
                now);
        }

        return true;
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

        MatchResult<Match> loadResult = _matches.Get(0, matchId);
        if (loadResult.Status == MatchStatus.MatchNotFound)
        {
            return loadResult;
        }

        if (loadResult.Status == MatchStatus.Success && loadResult.Value != null)
        {
            Match loaded = loadResult.Value;
            lock (_activeMatchesSync)
            {
                _activeMatches[loaded.Id] = loaded;
            }

            return new MatchResult<Match>(loaded);
        }

        return loadResult;
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

    private static KeyValuePair<int, int>? FindLegalPlacement(int[] board, int disc)
    {
        int[] tempBoard = new int[ReversiEngine.CellCount];
        for (int row = 0; row < ReversiEngine.BoardSize; row++)
        {
            for (int col = 0; col < ReversiEngine.BoardSize; col++)
            {
                Array.Copy(board, tempBoard, ReversiEngine.CellCount);
                int[] flipped = ReversiEngine.ApplyMove(tempBoard, row, col, disc);
                if (flipped.Length > 0)
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
            return 1;
        }

        if (whiteCount > blackCount)
        {
            return 2;
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
