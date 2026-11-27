using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Contracts;

/// <summary>
/// Verifies match state and replay transport without duplicating server validation.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class MatchSerializationTests
{
    private const long SampleMatchId = 7L;
    private const long SampleVersion = 4L;
    private const string GuestDisplayName = "Guest-two";

    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void MatchStateSnapshot_ActiveTurn_PreservesBoardAndVersion()
    {
        MatchStateSnapshot state = CreateActiveState();
        MatchState expectedState = MatchState.InProgress;
        int? expectedTurnSeat = 2;
        DateTime? expectedTurnStart = RecordedAtUtc;
        DateTimeKind expectedKind = DateTimeKind.Utc;
        int? expectedSkippedSeat = 1;
        int expectedBlackCount = 4;
        int expectedWhiteCount = 3;

        MatchStateSnapshot result = ContractSerializationTests.RoundTrip(state);

        AssertBoard(state.Board, result.Board);
        Assert.AreEqual(SampleMatchId, result.MatchId);
        Assert.AreEqual(SampleVersion, result.MatchStateVersion);
        Assert.AreEqual(expectedState, result.State);
        Assert.AreEqual(expectedTurnSeat, result.CurrentTurnSeatNumber);
        Assert.AreEqual(expectedTurnStart, result.TurnStartedAtUtc);
        Assert.AreEqual(expectedKind, result.TurnStartedAtUtc.GetValueOrDefault().Kind);
        Assert.AreEqual(expectedSkippedSeat, result.SkippedSeatNumber);
        Assert.IsNull(result.LastMove);
        Assert.IsNull(result.Conclusion);
        Assert.AreEqual(expectedBlackCount, result.BlackPieceCount);
        Assert.AreEqual(expectedWhiteCount, result.WhitePieceCount);
    }

    [TestMethod]
    public void MatchStateSnapshot_FinishedDraw_PreservesConclusionAndAbsentTurn()
    {
        MatchStateSnapshot state = CreateActiveState();
        state.State = MatchState.Finished;
        state.CurrentTurnSeatNumber = null;
        state.TurnStartedAtUtc = null;
        state.SkippedSeatNumber = null;
        state.Conclusion = CreateConclusion();
        state.LastMove = CreateMove();
        MatchState expectedState = MatchState.Finished;
        MatchEndReason expectedReason = MatchEndReason.NoLegalMoves;
        MoveOrigin expectedOrigin = MoveOrigin.Timeout;

        MatchStateSnapshot result = ContractSerializationTests.RoundTrip(state);

        MatchConclusion conclusion = TestAssert.IsNotNull(result.Conclusion);
        Assert.AreEqual(expectedState, result.State);
        Assert.IsNull(result.CurrentTurnSeatNumber);
        Assert.IsNull(result.TurnStartedAtUtc);
        Assert.IsNull(result.SkippedSeatNumber);
        Assert.AreEqual(expectedReason, conclusion.Reason);
        Assert.IsNull(conclusion.WinnerSeatNumber);
        Assert.IsNull(conclusion.AbandoningSeatNumber);
        Assert.AreEqual(SampleMatchId, conclusion.MatchId);
        Assert.AreEqual(SampleVersion, conclusion.MatchStateVersion);
        Assert.AreEqual(RecordedAtUtc, conclusion.EndedAtUtc);
        Assert.AreEqual(expectedOrigin, TestAssert.IsNotNull(result.LastMove).Origin);
    }

    [TestMethod]
    public void MatchInformation_ObserverAndGuest_PreservesOptionalIdentities()
    {
        int expectedTurnDuration = 30;
        int expectedParticipantCount = 2;
        ParticipantKind expectedRegisteredKind = ParticipantKind.Registered;
        long? expectedAccountId = 42L;
        ParticipantKind expectedGuestKind = ParticipantKind.Guest;
        MatchInformation information = new MatchInformation
        {
            MatchId = SampleMatchId,
            AssignedSeatNumber = null,
            TurnDurationSeconds = expectedTurnDuration,
            Participants = CreateParticipants()
        };

        MatchInformation result = ContractSerializationTests.RoundTrip(information);

        Assert.IsNull(result.AssignedSeatNumber);
        Assert.AreEqual(expectedTurnDuration, result.TurnDurationSeconds);
        Assert.AreEqual(expectedParticipantCount, result.Participants.Length);
        Assert.AreEqual(expectedRegisteredKind, result.Participants[0].Kind);
        Assert.AreEqual(expectedAccountId, result.Participants[0].AccountId);
        Assert.AreEqual(expectedGuestKind, result.Participants[1].Kind);
        Assert.IsNull(result.Participants[1].AccountId);
        Assert.AreEqual(GuestDisplayName, result.Participants[1].DisplayName);
    }

    [TestMethod]
    public void MatchDetails_ReplayFrames_PreservesMoveAndFrameOrder()
    {
        BoardCellState[] initialBoard = CreateBoard();
        BoardCellState[] nextBoard = (BoardCellState[])initialBoard.Clone();
        nextBoard[19] = BoardCellState.Black;
        nextBoard[27] = BoardCellState.Black;
        int expectedFrameCount = 2;
        int expectedMoveCount = 1;
        int expectedMoveNumber = 1;
        int expectedSeatNumber = 1;
        int expectedRow = 2;
        int expectedColumn = 3;
        int expectedFlipped = 1;
        MoveOrigin expectedOrigin = MoveOrigin.Timeout;
        MatchDetails details = new MatchDetails
        {
            MatchId = SampleMatchId,
            MatchStateVersion = SampleVersion,
            Participants = CreateParticipants(),
            TurnDurationSeconds = 30,
            State = MatchState.InProgress,
            RecordedAtUtc = RecordedAtUtc,
            Moves = new[] { CreateMove() },
            BoardFrames = new[] { initialBoard, nextBoard }
        };

        MatchDetails result = ContractSerializationTests.RoundTrip(details);

        Assert.AreEqual(expectedFrameCount, result.BoardFrames.Length);
        AssertBoard(initialBoard, result.BoardFrames[0]);
        AssertBoard(nextBoard, result.BoardFrames[1]);
        Assert.AreEqual(expectedMoveCount, result.Moves.Length);
        MoveRecord move = result.Moves[0];
        Assert.AreEqual(expectedMoveNumber, move.MoveNumber);
        Assert.AreEqual(expectedSeatNumber, move.SeatNumber);
        Assert.AreEqual(expectedRow, move.Row);
        Assert.AreEqual(expectedColumn, move.Column);
        Assert.AreEqual(expectedFlipped, move.FlippedPieceCount);
        Assert.AreEqual(RecordedAtUtc, move.PlayedAtUtc);
        Assert.AreEqual(expectedOrigin, move.Origin);
        Assert.IsNull(result.Conclusion);
    }

    [TestMethod]
    public void MatchSummary_ActiveAndFinishedHistory_PreservesOptionalConclusion()
    {
        MatchState expectedActiveState = MatchState.InProgress;
        MatchState expectedFinishedState = MatchState.Finished;
        int expectedSeatNumber = 1;
        MatchSummary[] history = new[]
        {
            CreateSummary(expectedActiveState),
            CreateSummary(expectedFinishedState)
        };
        history[1].Conclusion = CreateConclusion();

        MatchSummary[] result = ContractSerializationTests.RoundTrip(history);

        Assert.AreEqual(expectedActiveState, result[0].State);
        Assert.IsNull(result[0].Conclusion);
        Assert.AreEqual(expectedFinishedState, result[1].State);
        Assert.AreEqual(RecordedAtUtc, TestAssert.IsNotNull(result[1].Conclusion).EndedAtUtc);
        Assert.AreEqual(expectedSeatNumber, result[0].AssignedSeatNumber);
        Assert.AreEqual(RecordedAtUtc, result[0].RecordedAtUtc);
    }

    [TestMethod]
    public void MatchDetails_EmptyHistory_PreservesEmptyArrays()
    {
        int expectedMoveCount = 0;
        int expectedFrameCount = 1;
        int expectedBoardLength = 64;
        MatchDetails details = new MatchDetails
        {
            MatchId = SampleMatchId,
            Participants = CreateParticipants(),
            State = MatchState.InProgress,
            RecordedAtUtc = RecordedAtUtc,
            TurnDurationSeconds = 30,
            Moves = Array.Empty<MoveRecord>(),
            BoardFrames = new[] { CreateBoard() }
        };

        MatchDetails result = ContractSerializationTests.RoundTrip(details);

        Assert.AreEqual(expectedMoveCount, TestAssert.IsNotNull(result.Moves).Length);
        Assert.AreEqual(expectedFrameCount, result.BoardFrames.Length);
        Assert.AreEqual(expectedBoardLength, result.BoardFrames[0].Length);
    }

    private static MatchStateSnapshot CreateActiveState()
    {
        return new MatchStateSnapshot
        {
            MatchId = SampleMatchId,
            MatchStateVersion = SampleVersion,
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
            MatchId = SampleMatchId,
            MatchStateVersion = SampleVersion,
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
            MatchId = SampleMatchId,
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
                DisplayName = GuestDisplayName
            }
        };
    }

    private static void AssertBoard(BoardCellState[] expected, BoardCellState[] actual)
    {
        int expectedLength = 64;
        Assert.AreEqual(expectedLength, actual.Length);
        Assert.IsTrue(expected.SequenceEqual(actual), "Board cells changed during transport.");
    }
}
