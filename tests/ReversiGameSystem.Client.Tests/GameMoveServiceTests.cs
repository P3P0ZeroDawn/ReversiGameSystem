using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class GameMoveServiceTests
{
    internal void TryMakeMove_ValidMove_RecordsAndAdvancesTurn()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        session.RegisterTurnTimeout(DiscColor.Black);
        session.RegisterTurnTimeout(DiscColor.Black);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        GameMoveResult result = service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(2, 3),
            out MatchMove? move,
            out DiscColor? skippedColor);

        // Assert
        TestAssert.AreEqual(GameMoveResult.Success, result);
        TestAssert.AreEqual(true, move != null);
        TestAssert.AreEqual(false, TestAssert.IsNotNull(move).WasAutomatic);
        TestAssert.AreEqual<DiscColor?>(null, skippedColor);
        TestAssert.AreEqual(1, session.Moves.Count);
        TestAssert.AreEqual(4, session.Board.Count(DiscColor.Black));
        TestAssert.AreEqual(1, session.Board.Count(DiscColor.White));
        TestAssert.AreEqual(DiscColor.White, session.CurrentTurnColor);
        TestAssert.AreEqual(0, session.GetConsecutiveTimeouts(DiscColor.Black));
    }

    internal void TryMakeMove_OpponentHasNoMove_KeepsCurrentPlayerTurn()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            ".B.BBBB.", "..BBB...", "BB...BBB", ".BBB.B.B",
            "..BBB..B", "B..BBWBB", ".W.BB...", "BBBB.BBB");
        MatchSession session = CreateStartedSession(board);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        GameMoveResult result = service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(4, 6),
            out _,
            out DiscColor? skippedColor);

        // Assert
        TestAssert.AreEqual(GameMoveResult.Success, result);
        TestAssert.AreEqual<DiscColor?>(DiscColor.White, skippedColor);
        TestAssert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
        TestAssert.AreEqual(MatchState.InProgress, session.State);
    }

    internal void TryMakeMove_InvalidPosition_PreservesSession()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        ReversiBoard originalBoard = session.Board;
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        GameMoveResult result = service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(0, 0),
            out MatchMove? move,
            out _);

        // Assert
        TestAssert.AreEqual(GameMoveResult.InvalidPosition, result);
        TestAssert.AreEqual<MatchMove?>(null, move);
        TestAssert.AreEqual(true, ReferenceEquals(originalBoard, session.Board));
        TestAssert.AreEqual(0, session.Moves.Count);
        TestAssert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
    }

    internal void TryMakeMove_WrongTurn_PreservesSession()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        GameMoveResult result = service.TryMakeMove(
            session,
            DiscColor.White,
            new BoardPosition(2, 4),
            out _,
            out _);

        // Assert
        TestAssert.AreEqual(GameMoveResult.NotPlayersTurn, result);
        TestAssert.AreEqual(0, session.Moves.Count);
        TestAssert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
    }

    internal void PrepareCurrentTurn_NoMove_PassesTurn()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            "WB......", "........", "........", "........",
            "........", "........", "........", "........");
        MatchSession session = CreateStartedSession(board);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnPreparationResult result = service.PrepareCurrentTurn(
            session,
            out DiscColor? skippedColor);

        // Assert
        TestAssert.AreEqual(TurnPreparationResult.TurnPassed, result);
        TestAssert.AreEqual<DiscColor?>(DiscColor.Black, skippedColor);
        TestAssert.AreEqual(DiscColor.White, session.CurrentTurnColor);
        TestAssert.AreEqual(MatchState.InProgress, session.State);
    }

    internal void PrepareCurrentTurn_NoMovesForEitherPlayer_FinishesMatch()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            "BBBBBBBB", "BBBBBBBB", "BBBBBBBB", "BBBBBBBB",
            "BBBBBBBB", "BBBBBBBB", "BBBBBBBB", "BBBBBBBB");
        MatchSession session = CreateStartedSession(board);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnPreparationResult result = service.PrepareCurrentTurn(session, out _);

        // Assert
        TestAssert.AreEqual(TurnPreparationResult.MatchFinished, result);
        TestAssert.AreEqual(MatchState.Finished, session.State);
        TestAssert.AreEqual("Pedro", session.WinnerName);
        TestAssert.AreEqual<MatchEndReason?>(MatchEndReason.BoardCompleted, session.EndReason);
        TestAssert.AreEqual(true, session.FinishedAtUtc.HasValue);
    }

    internal void PrepareCurrentTurn_EqualFullBoard_FinishesWithDraw()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            "BBBBWWWW", "BBBBWWWW", "BBBBWWWW", "BBBBWWWW",
            "BBBBWWWW", "BBBBWWWW", "BBBBWWWW", "BBBBWWWW");
        MatchSession session = CreateStartedSession(board);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnPreparationResult result = service.PrepareCurrentTurn(session, out _);

        // Assert
        TestAssert.AreEqual(TurnPreparationResult.MatchFinished, result);
        TestAssert.AreEqual(MatchState.Finished, session.State);
        TestAssert.AreEqual<string?>(null, session.WinnerName);
        TestAssert.AreEqual<MatchEndReason?>(
            MatchEndReason.BoardCompleted,
            session.EndReason);
    }

    internal void HandleTimeout_FirstTimeout_MakesAutomaticMove()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnTimeoutResult result = service.HandleTimeout(
            session,
            out MatchMove? move,
            out _);

        // Assert
        TestAssert.AreEqual(TurnTimeoutResult.AutomaticMove, result);
        TestAssert.AreEqual(true, move != null);
        MatchMove actualMove = TestAssert.IsNotNull(move);
        TestAssert.AreEqual(true, actualMove.WasAutomatic);
        TestAssert.AreEqual(MatchMoveOrigin.Timeout, actualMove.Origin);
        TestAssert.AreEqual(1, session.Moves.Count);
        TestAssert.AreEqual(1, session.GetConsecutiveTimeouts(DiscColor.Black));
        TestAssert.AreEqual(DiscColor.White, session.CurrentTurnColor);
    }

    internal void HandleTimeout_ThirdTimeout_AwardsOpponentVictory()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        session.RegisterTurnTimeout(DiscColor.Black);
        session.RegisterTurnTimeout(DiscColor.Black);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnTimeoutResult result = service.HandleTimeout(session, out _, out _);

        // Assert
        TestAssert.AreEqual(TurnTimeoutResult.MatchFinishedByInactivity, result);
        TestAssert.AreEqual(MatchState.Finished, session.State);
        TestAssert.AreEqual("System", session.WinnerName);
        TestAssert.AreEqual<MatchEndReason?>(MatchEndReason.Inactivity, session.EndReason);
        TestAssert.AreEqual(0, session.Moves.Count);

        GameMoveResult moveAfterFinish = service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(2, 3),
            out _,
            out _);
        TestAssert.AreEqual(GameMoveResult.MatchNotInProgress, moveAfterFinish);
    }

    internal void TryMakeSystemMove_ValidTurn_RecordsSystemOrigin()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        GameMoveService service = new GameMoveService(new Random(1));
        service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(2, 3),
            out _,
            out _);

        // Act
        GameMoveResult result = service.TryMakeSystemMove(
            session,
            out MatchMove? move,
            out _);

        // Assert
        TestAssert.AreEqual(GameMoveResult.Success, result);
        TestAssert.AreEqual(true, move != null);
        TestAssert.AreEqual(MatchMoveOrigin.System, TestAssert.IsNotNull(move).Origin);
        TestAssert.AreEqual(2, session.Moves.Count);
        TestAssert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
    }

    private static MatchSession CreateStartedSession(ReversiBoard? board = null)
    {
        MatchSession session = new MatchSession(
            "MV2T4X",
            40,
            "Pedro",
            null,
            DiscColor.Black,
            board);
        session.StartAgainstSystem("System");
        return session;
    }
}
