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
    private const string MatchRequestRejectedMessage = "The match request was rejected.";
    private const int DefaultSeatNumber = 1;

    internal static MatchParticipantSummary[] MapParticipants(Match match)
    {
        MatchParticipantSummary[] participants = match.Participants
            .Select(MapParticipant)
            .ToArray();
        return participants;
    }

    internal static LobbySnapshot MapLobby(Match match)
    {
        MatchConfiguration configuration = new MatchConfiguration
        {
            TurnDurationSeconds = match.TurnDurationSeconds,
            OpponentKind = match.Participants.Any(participant => participant.IsSystem)
                ? OpponentKind.System
                : OpponentKind.Human,
            CountsTowardStatistics = match.CountsTowardStatistics
        };

        LobbySnapshot lobby = new LobbySnapshot
        {
            MatchId = match.Id,
            MatchCode = match.Code,
            State = MapMatchState(match.State),
            Participants = MapParticipants(match),
            Configuration = configuration
        };
        return lobby;
    }

    internal static MatchStateSnapshot MapState(Match match)
    {
        MatchConclusion? conclusion = null;
        if (IsConcluded(match))
        {
            conclusion = MapConclusion(match);
        }

        int[] board = match.Board ?? ReversiEngine.CreateInitialBoard();
        if (match.Board == null)
        {
            match.Board = board;
        }

        MoveRecord? lastMove = null;
        if (match.LastMove != null)
        {
            lastMove = MapMove(match.LastMove);
        }

        MatchStateSnapshot snapshot = new MatchStateSnapshot
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
        return snapshot;
    }

    internal static MatchStateSnapshot MapInitialState(Match match)
    {
        RequireStarted(match);
        return MapState(match);
    }

    internal static MatchConclusion MapConclusion(Match match)
    {
        int[] board = match.Board ?? ReversiEngine.CreateInitialBoard();
        MatchConclusion conclusion = new MatchConclusion
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
        return conclusion;
    }

    internal static MoveRecord MapMove(MatchMove move)
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

        MoveRecord record = new MoveRecord
        {
            MoveNumber = move.MoveNumber,
            SeatNumber = move.SeatNumber,
            Row = move.Row,
            Column = move.Column,
            PlayedAtUtc = move.PlayedAtUtc,
            FlippedPieceCount = move.FlippedPieceCount,
            Origin = origin
        };
        return record;
    }

    internal static MatchDetails MapDetails(Match match)
    {
        MatchConclusion? conclusion = null;
        if (IsConcluded(match))
        {
            conclusion = MapConclusion(match);
        }

        MoveRecord[] moves = match.Moves.Select(MapMove).ToArray();
        BoardCellState[][] boardFrames = GenerateBoardFrames(match.Moves.ToArray());

        MatchDetails details = new MatchDetails
        {
            MatchId = match.Id,
            MatchStateVersion = match.MatchStateVersion,
            Participants = MapParticipants(match),
            TurnDurationSeconds = match.TurnDurationSeconds,
            State = MapMatchState(match.State),
            RecordedAtUtc = match.StartedAtUtc ?? DateTime.UtcNow,
            Conclusion = conclusion,
            Moves = moves,
            BoardFrames = boardFrames
        };
        return details;
    }

    internal static MatchSummary MapSummary(Match match, long accountId)
    {
        MatchParticipant? participant = match.Participants
            .FirstOrDefault(p => p.AccountId == accountId);
        int assignedSeat = participant != null ? participant.SeatNumber : DefaultSeatNumber;
        int[] board = match.Board ?? ReversiEngine.CreateInitialBoard();

        MatchConclusion? conclusion = null;
        if (IsConcluded(match))
        {
            conclusion = MapConclusion(match);
        }

        MatchSummary summary = new MatchSummary
        {
            MatchId = match.Id,
            Participants = MapParticipants(match),
            AssignedSeatNumber = assignedSeat,
            State = MapMatchState(match.State),
            RecordedAtUtc = match.StartedAtUtc ?? DateTime.UtcNow,
            BlackPieceCount = board.Count(cell => cell == ReversiEngine.Black),
            WhitePieceCount = board.Count(cell => cell == ReversiEngine.White),
            Conclusion = conclusion
        };
        return summary;
    }

    private static MatchParticipantSummary MapParticipant(MatchParticipant participant)
    {
        MatchParticipantSummary summary = new MatchParticipantSummary
        {
            SeatNumber = participant.SeatNumber,
            AccountId = participant.AccountId,
            DisplayName = participant.DisplayName,
            Kind = participant.IsSystem ? ParticipantKind.System : ParticipantKind.Registered
        };
        return summary;
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
            }, MatchRequestRejectedMessage);
        }

        if (!string.Equals(match.State, StateInProgress, StringComparison.Ordinal))
        {
            throw new FaultException<MatchFault>(new MatchFault
            {
                Reason = MatchFailure.MatchNotInProgress
            }, MatchRequestRejectedMessage);
        }
    }

    private static bool IsConcluded(Match match)
    {
        return string.Equals(match.State, StateFinished, StringComparison.Ordinal)
            || match.EndedAtUtc.HasValue;
    }
}
