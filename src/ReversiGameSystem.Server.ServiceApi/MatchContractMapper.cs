using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Translates internal match domain representations into WCF contract snapshots.</summary>
internal static class MatchContractMapper
{
    private const string StateFinished = "Finished";
    private const string StateCancelled = "Cancelled";
    private const string StateInProgress = "InProgress";
    private const string OriginTimeout = "Timeout";
    private const string OriginSystem = "System";
    private const string EndReasonInactivity = "INACTIVITY";
    private const string EndReasonAbandonment = "ABANDONMENT";
    private const int DefaultSeatNumber = 1;

    internal static MatchParticipantSummary[] Participants(Match match)
    {
        return match.Participants.Select(player => new MatchParticipantSummary
        {
            SeatNumber = player.SeatNumber,
            AccountId = player.AccountId,
            DisplayName = player.DisplayName,
            Kind = ParticipantKind.Registered
        }).ToArray();
    }

    internal static LobbySnapshot Lobby(Match match)
    {
        return new LobbySnapshot
        {
            MatchId = match.Id,
            MatchCode = match.Code,
            State = MapMatchState(match.State),
            Participants = Participants(match),
            Configuration = new MatchConfiguration
            {
                TurnDurationSeconds = match.TurnDurationSeconds,
                OpponentKind = OpponentKind.Human,
                CountsTowardStatistics = match.CountsTowardStatistics
            }
        };
    }

    internal static MatchStateSnapshot State(Match match)
    {
        MatchConclusion? conclusion = null;
        if (IsConcluded(match))
        {
            conclusion = Conclusion(match);
        }

        int[] board = match.Board ?? ReversiEngine.CreateInitialBoard();
        if (match.Board == null)
        {
            match.Board = board;
        }

        MoveRecord? lastMove = null;
        if (match.LastMove != null)
        {
            lastMove = Move(match.LastMove);
        }

        return new MatchStateSnapshot
        {
            MatchId = match.Id,
            MatchStateVersion = match.MatchStateVersion,
            State = MapMatchState(match.State),
            Board = board.Select(cell => (BoardCellState)cell).ToArray(),
            BlackPieceCount = board.Count(cell => cell == ReversiEngine.Black),
            WhitePieceCount = board.Count(cell => cell == ReversiEngine.White),
            CurrentTurnSeatNumber = match.CurrentTurnSeatNumber,
            TurnStartedAtUtc = match.TurnStartedAtUtc,
            LastMove = lastMove,
            SkippedSeatNumber = match.SkippedSeatNumber,
            Conclusion = conclusion
        };
    }

    internal static MatchStateSnapshot InitialState(Match match)
    {
        RequireStarted(match);
        return State(match);
    }

    internal static MatchConclusion Conclusion(Match match)
    {
        int[] board = match.Board ?? ReversiEngine.CreateInitialBoard();
        return new MatchConclusion
        {
            MatchId = match.Id,
            MatchStateVersion = match.MatchStateVersion,
            Reason = MapEndReason(match.EndReason),
            WinnerSeatNumber = match.WinnerSeatNumber,
            AbandoningSeatNumber = match.AbandoningSeatNumber,
            BlackPieceCount = board.Count(cell => cell == ReversiEngine.Black),
            WhitePieceCount = board.Count(cell => cell == ReversiEngine.White),
            EndedAtUtc = match.EndedAtUtc ?? DateTime.UtcNow
        };
    }

    internal static MoveRecord Move(MatchMove move)
    {
        MoveOrigin origin = MoveOrigin.Manual;
        if (string.Equals(move.Origin, OriginTimeout, StringComparison.OrdinalIgnoreCase))
        {
            origin = MoveOrigin.Timeout;
        }
        else if (string.Equals(move.Origin, OriginSystem, StringComparison.OrdinalIgnoreCase))
        {
            origin = MoveOrigin.System;
        }

        return new MoveRecord
        {
            MoveNumber = move.MoveNumber,
            SeatNumber = move.SeatNumber,
            Row = move.Row,
            Column = move.Column,
            PlayedAtUtc = move.PlayedAtUtc,
            FlippedPieceCount = move.FlippedPieceCount,
            Origin = origin
        };
    }

    internal static MatchDetails Details(Match match)
    {
        MatchConclusion? conclusion = null;
        if (IsConcluded(match))
        {
            conclusion = Conclusion(match);
        }

        MoveRecord[] moves = match.Moves.Select(Move).ToArray();
        BoardCellState[][] boardFrames = GenerateBoardFrames(match.Moves.ToArray());

        return new MatchDetails
        {
            MatchId = match.Id,
            MatchStateVersion = match.MatchStateVersion,
            Participants = Participants(match),
            TurnDurationSeconds = match.TurnDurationSeconds,
            State = MapMatchState(match.State),
            RecordedAtUtc = match.StartedAtUtc ?? DateTime.UtcNow,
            Conclusion = conclusion,
            Moves = moves,
            BoardFrames = boardFrames
        };
    }

    internal static MatchSummary Summary(Match match, long accountId)
    {
        MatchParticipant? participant = match.Participants
            .FirstOrDefault(p => p.AccountId == accountId);
        int assignedSeat = participant != null ? participant.SeatNumber : DefaultSeatNumber;
        int[] board = match.Board ?? ReversiEngine.CreateInitialBoard();

        MatchConclusion? conclusion = null;
        if (IsConcluded(match))
        {
            conclusion = Conclusion(match);
        }

        return new MatchSummary
        {
            MatchId = match.Id,
            Participants = Participants(match),
            AssignedSeatNumber = assignedSeat,
            State = MapMatchState(match.State),
            RecordedAtUtc = match.StartedAtUtc ?? DateTime.UtcNow,
            BlackPieceCount = board.Count(cell => cell == ReversiEngine.Black),
            WhitePieceCount = board.Count(cell => cell == ReversiEngine.White),
            Conclusion = conclusion
        };
    }

    internal static BoardCellState[][] GenerateBoardFrames(MatchMove[] moves)
    {
        List<BoardCellState[]> frames = new List<BoardCellState[]>();
        int[] currentBoard = ReversiEngine.CreateInitialBoard();
        frames.Add(currentBoard.Select(cell => (BoardCellState)cell).ToArray());

        for (int i = 0; i < moves.Length; i++)
        {
            MatchMove move = moves[i];
            ReversiEngine.ApplyMove(currentBoard, move.Row, move.Column, move.SeatNumber);
            frames.Add(currentBoard.Select(cell => (BoardCellState)cell).ToArray());
        }

        return frames.ToArray();
    }

    internal static MatchState MapMatchState(string? state)
    {
        if (string.Equals(state, StateFinished, StringComparison.OrdinalIgnoreCase))
        {
            return MatchState.Finished;
        }

        if (string.Equals(state, StateCancelled, StringComparison.OrdinalIgnoreCase))
        {
            return MatchState.Cancelled;
        }

        if (string.Equals(state, StateInProgress, StringComparison.OrdinalIgnoreCase))
        {
            return MatchState.InProgress;
        }

        return MatchState.Waiting;
    }

    internal static MatchEndReason MapEndReason(string? endReason)
    {
        if (string.Equals(endReason, EndReasonInactivity, StringComparison.OrdinalIgnoreCase))
        {
            return MatchEndReason.Inactivity;
        }

        if (string.Equals(endReason, EndReasonAbandonment, StringComparison.OrdinalIgnoreCase))
        {
            return MatchEndReason.Abandonment;
        }

        return MatchEndReason.NoLegalMoves;
    }

    /// <summary>Ensures that the match is in the active InProgress state.</summary>
    internal static void RequireStarted(Match match)
    {
        if (string.Equals(match.State, StateCancelled, StringComparison.Ordinal))
        {
            throw new FaultException<MatchFault>(new MatchFault
            {
                Reason = MatchFailure.MatchNotWaiting
            }, "The match request was rejected.");
        }

        if (!string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
        {
            throw new FaultException<MatchFault>(new MatchFault
            {
                Reason = MatchFailure.MatchNotInProgress
            }, "The match request was rejected.");
        }
    }

    private static bool IsConcluded(Match match)
    {
        return string.Equals(match.State, StateFinished, StringComparison.Ordinal)
            || match.EndedAtUtc.HasValue;
    }
}
