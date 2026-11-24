using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class GameMoveServiceTests
{
    [TestMethod]
    public void TryMakeMove_ValidMove_RecordsAndAdvancesTurn()
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
        Assert.AreEqual(GameMoveResult.Success, result);
        Assert.AreEqual(true, move != null);
        Assert.AreEqual(false, TestAssert.IsNotNull(move).WasAutomatic);
        Assert.AreEqual<DiscColor?>(null, skippedColor);
        Assert.AreEqual(1, session.Moves.Count);
        Assert.AreEqual(4, session.Board.Count(DiscColor.Black));
        Assert.AreEqual(1, session.Board.Count(DiscColor.White));
        Assert.AreEqual(DiscColor.White, session.CurrentTurnColor);
        Assert.AreEqual(0, session.GetConsecutiveTimeouts(DiscColor.Black));
    }

    [TestMethod]
    public void TryMakeMove_OpponentHasNoMove_KeepsCurrentPlayerTurn()
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
        Assert.AreEqual(GameMoveResult.Success, result);
        Assert.AreEqual<DiscColor?>(DiscColor.White, skippedColor);
        Assert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
        Assert.AreEqual(MatchState.InProgress, session.State);
    }

    [TestMethod]
    public void TryMakeMove_InvalidPosition_PreservesSession()
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
        Assert.AreEqual(GameMoveResult.InvalidPosition, result);
        Assert.AreEqual<MatchMove?>(null, move);
        Assert.AreEqual(true, ReferenceEquals(originalBoard, session.Board));
        Assert.AreEqual(0, session.Moves.Count);
        Assert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
    }

    [TestMethod]
    public void TryMakeMove_WrongTurn_PreservesSession()
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
        Assert.AreEqual(GameMoveResult.NotPlayersTurn, result);
        Assert.AreEqual(0, session.Moves.Count);
        Assert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
    }

    [TestMethod]
    public void PrepareCurrentTurn_NoMove_PassesTurn()
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
        Assert.AreEqual(TurnPreparationResult.TurnPassed, result);
        Assert.AreEqual<DiscColor?>(DiscColor.Black, skippedColor);
        Assert.AreEqual(DiscColor.White, session.CurrentTurnColor);
        Assert.AreEqual(MatchState.InProgress, session.State);
    }

    [TestMethod]
    public void PrepareCurrentTurn_NoMovesForEitherPlayer_FinishesMatch()
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
        Assert.AreEqual(TurnPreparationResult.MatchFinished, result);
        Assert.AreEqual(MatchState.Finished, session.State);
        Assert.AreEqual("Pedro", session.WinnerName);
        Assert.AreEqual<MatchEndReason?>(MatchEndReason.BoardCompleted, session.EndReason);
        Assert.AreEqual(true, session.FinishedAtUtc.HasValue);
    }

    [TestMethod]
    public void PrepareCurrentTurn_EqualFullBoard_FinishesWithDraw()
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
        Assert.AreEqual(TurnPreparationResult.MatchFinished, result);
        Assert.AreEqual(MatchState.Finished, session.State);
        Assert.AreEqual<string?>(null, session.WinnerName);
        Assert.AreEqual<MatchEndReason?>(
            MatchEndReason.BoardCompleted,
            session.EndReason);
    }

    [TestMethod]
    public void HandleTimeout_FirstTimeout_MakesAutomaticMove()
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
        Assert.AreEqual(TurnTimeoutResult.AutomaticMove, result);
        Assert.AreEqual(true, move != null);
        MatchMove actualMove = TestAssert.IsNotNull(move);
        Assert.AreEqual(true, actualMove.WasAutomatic);
        Assert.AreEqual(MatchMoveOrigin.Timeout, actualMove.Origin);
        Assert.AreEqual(1, session.Moves.Count);
        Assert.AreEqual(1, session.GetConsecutiveTimeouts(DiscColor.Black));
        Assert.AreEqual(DiscColor.White, session.CurrentTurnColor);
    }

    [TestMethod]
    public void HandleTimeout_ThirdTimeout_AwardsOpponentVictory()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        session.RegisterTurnTimeout(DiscColor.Black);
        session.RegisterTurnTimeout(DiscColor.Black);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnTimeoutResult result = service.HandleTimeout(session, out _, out _);

        // Assert
        Assert.AreEqual(TurnTimeoutResult.MatchFinishedByInactivity, result);
        Assert.AreEqual(MatchState.Finished, session.State);
        Assert.AreEqual("System", session.WinnerName);
        Assert.AreEqual<MatchEndReason?>(MatchEndReason.Inactivity, session.EndReason);
        Assert.AreEqual(0, session.Moves.Count);

        GameMoveResult moveAfterFinish = service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(2, 3),
            out _,
            out _);
        Assert.AreEqual(GameMoveResult.MatchNotInProgress, moveAfterFinish);
    }

    [TestMethod]
    public void TryMakeSystemMove_ValidTurn_RecordsSystemOrigin()
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
        Assert.AreEqual(GameMoveResult.Success, result);
        Assert.AreEqual(true, move != null);
        Assert.AreEqual(MatchMoveOrigin.System, TestAssert.IsNotNull(move).Origin);
        Assert.AreEqual(2, session.Moves.Count);
        Assert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
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
