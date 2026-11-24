using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Verifies match state and replay transport without duplicating server validation.
/// </summary>
[TestClass]
public sealed class MatchSerializationTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void MatchStateSnapshot_ActiveTurn_PreservesBoardAndVersion()
    {
        // Arrange
        MatchStateSnapshot state = CreateActiveState();

        // Act
        MatchStateSnapshot result = ContractSerializationTests.RoundTrip(state);

        // Assert
        AssertBoard(state.Board, result.Board);
        Assert.AreEqual(7L, result.MatchId);
        Assert.AreEqual(4L, result.MatchStateVersion);
        Assert.AreEqual(MatchState.InProgress, result.State);
        Assert.AreEqual<int?>(2, result.CurrentTurnSeatNumber);
        Assert.AreEqual<DateTime?>(RecordedAtUtc, result.TurnStartedAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, result.TurnStartedAtUtc.GetValueOrDefault().Kind);
        Assert.AreEqual<int?>(1, result.SkippedSeatNumber);
        Assert.AreEqual<MoveRecord?>(null, result.LastMove);
        Assert.AreEqual<MatchConclusion?>(null, result.Conclusion);
        Assert.AreEqual(4, result.BlackPieceCount);
        Assert.AreEqual(3, result.WhitePieceCount);
    }

    [TestMethod]
    public void MatchStateSnapshot_FinishedDraw_PreservesConclusionAndAbsentTurn()
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
        Assert.AreEqual(MatchState.Finished, result.State);
        Assert.AreEqual<int?>(null, result.CurrentTurnSeatNumber);
        Assert.AreEqual<DateTime?>(null, result.TurnStartedAtUtc);
        Assert.AreEqual<int?>(null, result.SkippedSeatNumber);
        MatchConclusion conclusion = TestAssert.IsNotNull(result.Conclusion);
        Assert.AreEqual(MatchEndReason.NoLegalMoves, conclusion.Reason);
        Assert.AreEqual<int?>(null, conclusion.WinnerSeatNumber);
        Assert.AreEqual<int?>(null, conclusion.AbandoningSeatNumber);
        Assert.AreEqual(7L, conclusion.MatchId);
        Assert.AreEqual(4L, conclusion.MatchStateVersion);
        Assert.AreEqual(RecordedAtUtc, conclusion.EndedAtUtc);
        Assert.AreEqual(MoveOrigin.Timeout, TestAssert.IsNotNull(result.LastMove).Origin);
    }

    [TestMethod]
    public void MatchInformation_ObserverAndGuest_PreservesOptionalIdentities()
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
        Assert.AreEqual<int?>(null, result.AssignedSeatNumber);
        Assert.AreEqual(30, result.TurnDurationSeconds);
        Assert.AreEqual(2, result.Participants.Length);
        Assert.AreEqual(ParticipantKind.Registered, result.Participants[0].Kind);
        Assert.AreEqual<long?>(42, result.Participants[0].AccountId);
        Assert.AreEqual(ParticipantKind.Guest, result.Participants[1].Kind);
        Assert.AreEqual<long?>(null, result.Participants[1].AccountId);
        Assert.AreEqual("Guest-two", result.Participants[1].DisplayName);
    }

    [TestMethod]
    public void MatchDetails_ReplayFrames_PreservesMoveAndFrameOrder()
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
        Assert.AreEqual(2, result.BoardFrames.Length);
        AssertBoard(initialBoard, result.BoardFrames[0]);
        AssertBoard(nextBoard, result.BoardFrames[1]);
        Assert.AreEqual(1, result.Moves.Length);
        MoveRecord move = result.Moves[0];
        Assert.AreEqual(1, move.MoveNumber);
        Assert.AreEqual(1, move.SeatNumber);
        Assert.AreEqual(2, move.Row);
        Assert.AreEqual(3, move.Column);
        Assert.AreEqual(1, move.FlippedPieceCount);
        Assert.AreEqual(RecordedAtUtc, move.PlayedAtUtc);
        Assert.AreEqual(MoveOrigin.Timeout, move.Origin);
        Assert.AreEqual<MatchConclusion?>(null, result.Conclusion);
    }

    [TestMethod]
    public void MatchSummary_ActiveAndFinishedHistory_PreservesOptionalConclusion()
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
        Assert.AreEqual(MatchState.InProgress, result[0].State);
        Assert.AreEqual<MatchConclusion?>(null, result[0].Conclusion);
        Assert.AreEqual(MatchState.Finished, result[1].State);
        Assert.AreEqual(RecordedAtUtc, TestAssert.IsNotNull(result[1].Conclusion).EndedAtUtc);
        Assert.AreEqual(1, result[0].AssignedSeatNumber);
        Assert.AreEqual(RecordedAtUtc, result[0].RecordedAtUtc);
    }

    [TestMethod]
    public void MatchDetails_EmptyHistory_PreservesEmptyArrays()
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
        Assert.AreEqual(0, TestAssert.IsNotNull(result.Moves).Length);
        Assert.AreEqual(1, result.BoardFrames.Length);
        Assert.AreEqual(64, result.BoardFrames[0].Length);
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
        Assert.AreEqual(64, actual.Length);
        Assert.IsTrue(expected.SequenceEqual(actual), "Board cells changed during transport.");
    }
}
