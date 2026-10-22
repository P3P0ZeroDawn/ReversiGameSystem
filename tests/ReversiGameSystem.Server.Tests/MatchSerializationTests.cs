using System;
using System.Linq;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Verifies match state and replay transport without duplicating server validation.
/// </summary>
internal sealed class MatchSerializationTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    internal void MatchStateSnapshot_ActiveTurn_PreservesBoardAndVersion()
    {
        // Arrange
        MatchStateSnapshot state = CreateActiveState();

        // Act
        MatchStateSnapshot result = ContractSerializationTests.RoundTrip(state);

        // Assert
        AssertBoard(state.Board, result.Board);
        TestAssert.AreEqual(7L, result.MatchId);
        TestAssert.AreEqual(4L, result.MatchStateVersion);
        TestAssert.AreEqual(MatchState.InProgress, result.State);
        TestAssert.AreEqual<int?>(2, result.CurrentTurnSeatNumber);
        TestAssert.AreEqual<DateTime?>(RecordedAtUtc, result.TurnStartedAtUtc);
        TestAssert.AreEqual(DateTimeKind.Utc, result.TurnStartedAtUtc.GetValueOrDefault().Kind);
        TestAssert.AreEqual<int?>(1, result.SkippedSeatNumber);
        TestAssert.AreEqual<MoveRecord?>(null, result.LastMove);
        TestAssert.AreEqual<MatchConclusion?>(null, result.Conclusion);
        TestAssert.AreEqual(4, result.BlackPieceCount);
        TestAssert.AreEqual(3, result.WhitePieceCount);
    }

    internal void MatchStateSnapshot_FinishedDraw_PreservesConclusionAndAbsentTurn()
    {
        // Arrange
        MatchStateSnapshot state = CreateActiveState();
        state.State = MatchState.Finished;
        state.CurrentTurnSeatNumber = null;
        state.TurnStartedAtUtc = null;
        state.SkippedSeatNumber = null;
        state.Conclusion = CreateConclusion();
        state.LastMove = CreateMove();

        // Act
        MatchStateSnapshot result = ContractSerializationTests.RoundTrip(state);

        // Assert
        TestAssert.AreEqual(MatchState.Finished, result.State);
        TestAssert.AreEqual<int?>(null, result.CurrentTurnSeatNumber);
        TestAssert.AreEqual<DateTime?>(null, result.TurnStartedAtUtc);
        TestAssert.AreEqual<int?>(null, result.SkippedSeatNumber);
        MatchConclusion conclusion = TestAssert.IsNotNull(result.Conclusion);
        TestAssert.AreEqual(MatchEndReason.NoLegalMoves, conclusion.Reason);
        TestAssert.AreEqual<int?>(null, conclusion.WinnerSeatNumber);
        TestAssert.AreEqual<int?>(null, conclusion.AbandoningSeatNumber);
        TestAssert.AreEqual(7L, conclusion.MatchId);
        TestAssert.AreEqual(4L, conclusion.MatchStateVersion);
        TestAssert.AreEqual(RecordedAtUtc, conclusion.EndedAtUtc);
        TestAssert.AreEqual(MoveOrigin.Timeout, TestAssert.IsNotNull(result.LastMove).Origin);
    }

    internal void MatchInformation_ObserverAndGuest_PreservesOptionalIdentities()
    {
        // Arrange
        MatchInformation information = new MatchInformation
        {
            MatchId = 7,
            AssignedSeatNumber = null,
            TurnDurationSeconds = 30,
            Participants = CreateParticipants()
        };

        // Act
        MatchInformation result = ContractSerializationTests.RoundTrip(information);

        // Assert
        TestAssert.AreEqual<int?>(null, result.AssignedSeatNumber);
        TestAssert.AreEqual(30, result.TurnDurationSeconds);
        TestAssert.AreEqual(2, result.Participants.Length);
        TestAssert.AreEqual(ParticipantKind.Registered, result.Participants[0].Kind);
        TestAssert.AreEqual<long?>(42, result.Participants[0].AccountId);
        TestAssert.AreEqual(ParticipantKind.Guest, result.Participants[1].Kind);
        TestAssert.AreEqual<long?>(null, result.Participants[1].AccountId);
        TestAssert.AreEqual("Guest-two", result.Participants[1].DisplayName);
    }

    internal void MatchDetails_ReplayFrames_PreservesMoveAndFrameOrder()
    {
        // Arrange
        BoardCellState[] initialBoard = CreateBoard();
        BoardCellState[] nextBoard = (BoardCellState[])initialBoard.Clone();
        nextBoard[19] = BoardCellState.Black;
        nextBoard[27] = BoardCellState.Black;
        MatchDetails details = new MatchDetails
        {
            MatchId = 7,
            MatchStateVersion = 4,
            Participants = CreateParticipants(),
            TurnDurationSeconds = 30,
            State = MatchState.InProgress,
            RecordedAtUtc = RecordedAtUtc,
            Moves = new[] { CreateMove() },
            BoardFrames = new[] { initialBoard, nextBoard }
        };

        // Act
        MatchDetails result = ContractSerializationTests.RoundTrip(details);

        // Assert
        TestAssert.AreEqual(2, result.BoardFrames.Length);
        AssertBoard(initialBoard, result.BoardFrames[0]);
        AssertBoard(nextBoard, result.BoardFrames[1]);
        TestAssert.AreEqual(1, result.Moves.Length);
        MoveRecord move = result.Moves[0];
        TestAssert.AreEqual(1, move.MoveNumber);
        TestAssert.AreEqual(1, move.SeatNumber);
        TestAssert.AreEqual(2, move.Row);
        TestAssert.AreEqual(3, move.Column);
        TestAssert.AreEqual(1, move.FlippedPieceCount);
        TestAssert.AreEqual(RecordedAtUtc, move.PlayedAtUtc);
        TestAssert.AreEqual(MoveOrigin.Timeout, move.Origin);
        TestAssert.AreEqual<MatchConclusion?>(null, result.Conclusion);
    }

    internal void MatchSummary_ActiveAndFinishedHistory_PreservesOptionalConclusion()
    {
        // Arrange
        MatchSummary[] history = new[]
        {
            CreateSummary(MatchState.InProgress),
            CreateSummary(MatchState.Finished)
        };
        history[1].Conclusion = CreateConclusion();

        // Act
        MatchSummary[] result = ContractSerializationTests.RoundTrip(history);

        // Assert
        TestAssert.AreEqual(MatchState.InProgress, result[0].State);
        TestAssert.AreEqual<MatchConclusion?>(null, result[0].Conclusion);
        TestAssert.AreEqual(MatchState.Finished, result[1].State);
        TestAssert.AreEqual(RecordedAtUtc, TestAssert.IsNotNull(result[1].Conclusion).EndedAtUtc);
        TestAssert.AreEqual(1, result[0].AssignedSeatNumber);
        TestAssert.AreEqual(RecordedAtUtc, result[0].RecordedAtUtc);
    }

    internal void MatchDetails_EmptyHistory_PreservesEmptyArrays()
    {
        // Arrange
        MatchDetails details = new MatchDetails
        {
            MatchId = 7,
            Participants = CreateParticipants(),
            State = MatchState.InProgress,
            RecordedAtUtc = RecordedAtUtc,
            TurnDurationSeconds = 30,
            Moves = Array.Empty<MoveRecord>(),
            BoardFrames = new[] { CreateBoard() }
        };

        // Act
        MatchDetails result = ContractSerializationTests.RoundTrip(details);

        // Assert
        TestAssert.AreEqual(0, TestAssert.IsNotNull(result.Moves).Length);
        TestAssert.AreEqual(1, result.BoardFrames.Length);
        TestAssert.AreEqual(64, result.BoardFrames[0].Length);
    }

    private static MatchStateSnapshot CreateActiveState()
    {
        return new MatchStateSnapshot
        {
            MatchId = 7,
            MatchStateVersion = 4,
            State = MatchState.InProgress,
            Board = CreateBoard(),
            BlackPieceCount = 4,
            WhitePieceCount = 3,
            CurrentTurnSeatNumber = 2,
            TurnStartedAtUtc = RecordedAtUtc,
            SkippedSeatNumber = 1
        };
    }

    private static BoardCellState[] CreateBoard()
    {
        BoardCellState[] board = new BoardCellState[64];
        board[27] = BoardCellState.White;
        board[28] = BoardCellState.Black;
        board[35] = BoardCellState.Black;
        board[36] = BoardCellState.White;
        return board;
    }

    private static MoveRecord CreateMove()
    {
        return new MoveRecord
        {
            MoveNumber = 1,
            SeatNumber = 1,
            Row = 2,
            Column = 3,
            PlayedAtUtc = RecordedAtUtc,
            FlippedPieceCount = 1,
            Origin = MoveOrigin.Timeout
        };
    }

    private static MatchConclusion CreateConclusion()
    {
        return new MatchConclusion
        {
            MatchId = 7,
            MatchStateVersion = 4,
            Reason = MatchEndReason.NoLegalMoves,
            BlackPieceCount = 32,
            WhitePieceCount = 32,
            EndedAtUtc = RecordedAtUtc
        };
    }

    private static MatchSummary CreateSummary(MatchState state)
    {
        return new MatchSummary
        {
            MatchId = 7,
            Participants = CreateParticipants(),
            AssignedSeatNumber = 1,
            State = state,
            RecordedAtUtc = RecordedAtUtc
        };
    }

    private static MatchParticipantSummary[] CreateParticipants()
    {
        return new[]
        {
            new MatchParticipantSummary
            {
                SeatNumber = 1,
                Kind = ParticipantKind.Registered,
                AccountId = 42,
                DisplayName = "player_one"
            },
            new MatchParticipantSummary
            {
                SeatNumber = 2,
                Kind = ParticipantKind.Guest,
                DisplayName = "Guest-two"
            }
        };
    }

    private static void AssertBoard(BoardCellState[] expected, BoardCellState[] actual)
    {
        TestAssert.AreEqual(64, actual.Length);
        TestAssert.IsTrue(expected.SequenceEqual(actual), "Board cells changed during transport.");
    }
}
