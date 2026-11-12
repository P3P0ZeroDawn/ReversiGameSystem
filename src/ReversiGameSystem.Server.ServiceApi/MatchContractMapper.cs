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
    /// <summary>Maps internal participant domain entities into contract summaries.</summary>
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

    /// <summary>Maps internal match state into a lobby snapshot.</summary>
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

    /// <summary>Creates the dynamic state snapshot for an active or concluded match.</summary>
    internal static MatchStateSnapshot State(Match match)
    {
        MatchConclusion? conclusion = null;
        if (string.Equals(match.State, "Finished", StringComparison.Ordinal)
            || match.EndedAtUtc.HasValue)
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

    /// <summary>Creates the initial snapshot for a started match.</summary>
    internal static MatchStateSnapshot InitialState(Match match)
    {
        RequireStarted(match);
        return State(match);
    }

    /// <summary>Creates the confirmed match conclusion snapshot.</summary>
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

    /// <summary>Translates an internal placement move into a contract move record.</summary>
    internal static MoveRecord Move(MatchMove move)
    {
        MoveOrigin origin = MoveOrigin.Manual;
        if (string.Equals(move.Origin, "Timeout", StringComparison.OrdinalIgnoreCase))
        {
            origin = MoveOrigin.Timeout;
        }
        else if (string.Equals(move.Origin, "System", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>Builds full match details including historical move records and board replay frames.</summary>
    internal static MatchDetails Details(Match match)
    {
        MatchConclusion? conclusion = null;
        if (string.Equals(match.State, "Finished", StringComparison.Ordinal)
            || match.EndedAtUtc.HasValue)
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

    /// <summary>Builds a summary of a match from the perspective of an authorized participant account.</summary>
    internal static MatchSummary Summary(Match match, long accountId)
    {
        MatchParticipant? participant = match.Participants.FirstOrDefault(p => p.AccountId == accountId);
        int assignedSeat = participant != null ? participant.SeatNumber : 1;
        int[] board = match.Board ?? ReversiEngine.CreateInitialBoard();

        MatchConclusion? conclusion = null;
        if (string.Equals(match.State, "Finished", StringComparison.Ordinal)
            || match.EndedAtUtc.HasValue)
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

    /// <summary>Generates sequential board frames for replay beginning with the initial board.</summary>
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

    /// <summary>Maps string match state representation to the contract enum.</summary>
    internal static MatchState MapMatchState(string? state)
    {
        if (string.Equals(state, "Finished", StringComparison.OrdinalIgnoreCase))
        {
            return MatchState.Finished;
        }

        if (string.Equals(state, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return MatchState.Cancelled;
        }

        if (string.Equals(state, "InProgress", StringComparison.OrdinalIgnoreCase))
        {
            return MatchState.InProgress;
        }

        return MatchState.Waiting;
    }

    /// <summary>Maps string end reason representation to the contract enum.</summary>
    internal static MatchEndReason MapEndReason(string? endReason)
    {
        if (string.Equals(endReason, "INACTIVITY", StringComparison.OrdinalIgnoreCase))
        {
            return MatchEndReason.Inactivity;
        }

        if (string.Equals(endReason, "ABANDONMENT", StringComparison.OrdinalIgnoreCase))
        {
            return MatchEndReason.Abandonment;
        }

        return MatchEndReason.NoLegalMoves;
    }

    /// <summary>Ensures that the match is in the active InProgress state.</summary>
    internal static void RequireStarted(Match match)
    {
        if (string.Equals(match.State, "Cancelled", StringComparison.Ordinal))
        {
            throw new FaultException<MatchFault>(new MatchFault
            {
                Reason = MatchFailure.MatchNotWaiting
            }, "The match request was rejected.");
        }

        if (!string.Equals(match.State, "InProgress", StringComparison.Ordinal))
        {
            throw new FaultException<MatchFault>(new MatchFault
            {
                Reason = MatchFailure.MatchNotInProgress
            }, "The match request was rejected.");
        }
    }
}
