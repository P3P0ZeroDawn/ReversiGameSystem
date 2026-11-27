using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Application.Matches;

public static class GameplayMatchRules
{
    private const int MaximumConsecutiveTimeouts = 3;
    private const int BlackSeatNumber = ReversiEngine.Black;
    private const int WhiteSeatNumber = ReversiEngine.White;
    private const string StateInProgress = "InProgress";
    private const string StateFinished = "Finished";
    private const string MoveOriginPlayer = "Player";
    private const string MoveOriginTimeout = "Timeout";
    private const string MoveOriginSystem = "System";
    private const string EndReasonBoardFull = "BOARD_FULL";
    private const string EndReasonNoLegalMoves = "NO_LEGAL_MOVES";
    private const string EndReasonInactivity = "INACTIVITY";
    private const string EndReasonAbandonment = "ABANDONMENT";
    private static readonly object RandomSyncRoot = new object();
    private static readonly Random MoveRandom = new Random();
    public static readonly TimeSpan SystemMoveDelay = TimeSpan.FromSeconds(5);

    public static bool ApplySystemMoveIfReady(Match match, DateTime now)
    {
        MatchParticipant? participant = FindParticipantBySeat(match, match.CurrentTurnSeatNumber);
        if (match.State != StateInProgress || participant == null || !participant.IsSystem
            || !match.TurnStartedAtUtc.HasValue
            || now < match.TurnStartedAtUtc.Value.Add(SystemMoveDelay))
        {
            return false;
        }

        MatchMove move = new MatchMove
        {
            SeatNumber = participant.SeatNumber,
            PlayedAtUtc = now,
            Origin = MoveOriginSystem
        };
        ApplyAutomaticMove(match, move);
        return true;
    }

    public static void InitializeStartedMatch(Match match, DateTime startedAtUtc)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        match.Board = ReversiEngine.CreateInitialBoard();
        match.MatchStateVersion = 0;
        match.CurrentTurnSeatNumber = BlackSeatNumber;
        match.TurnStartedAtUtc = match.StartedAtUtc ?? startedAtUtc;
    }

    public static MatchResult<MatchParticipant> ValidatePlacement(
        Match match,
        long accountId,
        MatchPlacement placement)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        if (placement == null)
        {
            throw new ArgumentNullException(nameof(placement));
        }

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

    public static MatchStatus ApplyPlayerMove(
        Match match,
        MatchPlacement placement,
        DateTime playedAtUtc)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        if (placement == null)
        {
            throw new ArgumentNullException(nameof(placement));
        }

        MatchParticipant? participant = FindParticipantBySeat(match, match.CurrentTurnSeatNumber);
        if (participant == null)
        {
            return MatchStatus.NotPlayersTurn;
        }

        int[] flipped = ReversiEngine.ApplyMove(
            match.Board,
            placement.Row,
            placement.Column,
            participant.SeatNumber);
        if (flipped.Length == 0)
        {
            return MatchStatus.InvalidPosition;
        }

        participant.ConsecutiveTimeoutCount = 0;
        MatchMove committedMove = new MatchMove
        {
            SeatNumber = participant.SeatNumber,
            Row = placement.Row,
            Column = placement.Column,
            PlayedAtUtc = playedAtUtc,
            FlippedPieceCount = flipped.Length,
            Origin = MoveOriginPlayer
        };

        RecordCommittedMove(match, committedMove);
        AdvanceTurnOrConclude(match, participant.SeatNumber, playedAtUtc);

        return MatchStatus.Success;
    }

    public static bool ApplyTimeoutIfExpired(Match match, DateTime now)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        MatchParticipant? actingParticipant = FindExpiredTurnParticipant(match, now);
        if (actingParticipant == null)
        {
            return false;
        }

        actingParticipant.ConsecutiveTimeoutCount++;
        if (actingParticipant.ConsecutiveTimeoutCount >= MaximumConsecutiveTimeouts)
        {
            match.MatchStateVersion++;
            match.WinnerSeatNumber = GetOpponentSeat(actingParticipant.SeatNumber);
            match.AbandoningSeatNumber = actingParticipant.SeatNumber;
            ConcludeMatch(match, EndReasonInactivity, now);
            return true;
        }

        MatchMove move = new MatchMove
        {
            SeatNumber = actingParticipant.SeatNumber,
            PlayedAtUtc = now,
            Origin = MoveOriginTimeout
        };
        ApplyAutomaticMove(match, move);
        return true;
    }

    public static MatchStatus ForfeitByParticipant(
        Match match,
        long accountId,
        DateTime endedAtUtc)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        MatchParticipant? participant = FindParticipant(match, accountId);
        if (participant == null)
        {
            return MatchStatus.Unauthorized;
        }

        if (string.Equals(match.State, StateFinished, StringComparison.Ordinal))
        {
            return MatchStatus.MatchFinished;
        }

        if (!string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
        {
            return MatchStatus.MatchNotInProgress;
        }

        match.MatchStateVersion++;
        match.WinnerSeatNumber = GetOpponentSeat(participant.SeatNumber);
        match.AbandoningSeatNumber = participant.SeatNumber;
        ConcludeMatch(match, EndReasonAbandonment, endedAtUtc);

        return MatchStatus.Success;
    }

    public static bool IsParticipant(Match match, long accountId)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        return FindParticipant(match, accountId) != null;
    }

    private static MatchParticipant? FindExpiredTurnParticipant(Match match, DateTime now)
    {
        if (!string.Equals(match.State, StateInProgress, StringComparison.Ordinal)
            || !match.CurrentTurnSeatNumber.HasValue)
        {
            return null;
        }

        DateTime turnStartedAtUtc = match.TurnStartedAtUtc ?? now;
        if (now < turnStartedAtUtc.AddSeconds(match.TurnDurationSeconds))
        {
            return null;
        }

        return FindParticipantBySeat(match, match.CurrentTurnSeatNumber);
    }

    private static void ApplyAutomaticMove(Match match, MatchMove move)
    {
        int currentSeat = move.SeatNumber;
        DateTime now = move.PlayedAtUtc;
        BoardCoordinate? legalCell = SelectAutomaticPlacement(match.Board, currentSeat);
        if (!legalCell.HasValue)
        {
            match.MatchStateVersion++;
            ConcludeByBoardScore(match, now);
            return;
        }

        int row = legalCell.Value.Row;
        int col = legalCell.Value.Column;
        int[] flipped = ReversiEngine.ApplyMove(match.Board, row, col, currentSeat);
        move.Row = row;
        move.Column = col;
        move.FlippedPieceCount = flipped.Length;
        RecordCommittedMove(match, move);
        AdvanceTurnOrConclude(match, currentSeat, now);
    }

    private static void RecordCommittedMove(Match match, MatchMove committedMove)
    {
        match.MatchStateVersion++;
        committedMove.MoveNumber = (int)match.MatchStateVersion;
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
        match.WinnerSeatNumber = DetermineWinner(blackPieceCount, whitePieceCount);
        match.AbandoningSeatNumber = null;
        string endReason = match.Board.Contains(ReversiEngine.Empty)
            ? EndReasonNoLegalMoves : EndReasonBoardFull;
        ConcludeMatch(match, endReason, now);
    }

    private static void ConcludeMatch(Match match, string endReason, DateTime endedAtUtc)
    {
        match.State = StateFinished;
        match.EndReason = endReason;
        match.EndedAtUtc = endedAtUtc;
        match.CurrentTurnSeatNumber = null;
        match.TurnStartedAtUtc = null;
        match.SkippedSeatNumber = null;
    }

    private static MatchParticipant? FindParticipant(Match match, long accountId)
    {
        return match.Participants.FirstOrDefault(
            current => current.AccountId == accountId);
    }

    private static MatchParticipant? FindParticipantBySeat(Match match, int? seatNumber)
    {
        if (!seatNumber.HasValue)
        {
            return null;
        }

        int targetSeat = seatNumber.Value;
        return match.Participants.FirstOrDefault(
            current => current.SeatNumber == targetSeat);
    }

    private static int GetOpponentSeat(int seatNumber)
    {
        return seatNumber == BlackSeatNumber ? WhiteSeatNumber : BlackSeatNumber;
    }

    private static BoardCoordinate? SelectAutomaticPlacement(int[] board, int disc)
    {
        List<BoardCoordinate> legalCells = new List<BoardCoordinate>();
        for (int row = 0; row < ReversiEngine.BoardSize; row++)
        {
            for (int col = 0; col < ReversiEngine.BoardSize; col++)
            {
                if (ReversiEngine.IsLegalMove(board, row, col, disc))
                {
                    legalCells.Add(new BoardCoordinate(row, col));
                }
            }
        }

        BoardCoordinate? selectedCell = null;
        if (legalCells.Count > 0)
        {
            int selectedIndex;
            lock (RandomSyncRoot)
            {
                selectedIndex = MoveRandom.Next(legalCells.Count);
            }

            selectedCell = legalCells[selectedIndex];
        }

        return selectedCell;
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

    private readonly struct BoardCoordinate
    {
        internal BoardCoordinate(int row, int column)
        {
            Row = row;
            Column = column;
        }

        internal int Row { get; }

        internal int Column { get; }
    }
}
