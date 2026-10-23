using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class ReversiBoardTests
{
    internal void CreateInitial_HasStandardFourCenterDiscs()
    {
        // Arrange

        // Act
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Assert
        TestAssert.AreEqual(DiscColor.White, board[3, 3]);
        TestAssert.AreEqual(DiscColor.Black, board[3, 4]);
        TestAssert.AreEqual(DiscColor.Black, board[4, 3]);
        TestAssert.AreEqual(DiscColor.White, board[4, 4]);
    }

    internal void CreateInitial_HasTwoDiscsOfEachColor()
    {
        // Arrange

        // Act
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Assert
        TestAssert.AreEqual(2, board.Count(DiscColor.Black));
        TestAssert.AreEqual(2, board.Count(DiscColor.White));
        TestAssert.AreEqual(60, board.Count(DiscColor.Empty));
    }

    internal void GetValidMoves_InitialBlackTurn_ReturnsFourPositions()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Act
        IReadOnlyList<BoardPosition> moves = board.GetValidMoves(DiscColor.Black);

        // Assert
        TestAssert.AreEqual(4, moves.Count);
        TestAssert.AreEqual(true, Contains(moves, new BoardPosition(2, 3)));
        TestAssert.AreEqual(true, Contains(moves, new BoardPosition(3, 2)));
        TestAssert.AreEqual(true, Contains(moves, new BoardPosition(4, 5)));
        TestAssert.AreEqual(true, Contains(moves, new BoardPosition(5, 4)));
    }

    internal void TryApplyMove_ValidMove_FlipsBracketedPiece()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Act
        bool applied = board.TryApplyMove(
            DiscColor.Black,
            new BoardPosition(2, 3),
            out ReversiBoard? updatedBoard);

        // Assert
        TestAssert.AreEqual(true, applied);
        TestAssert.AreEqual(true, updatedBoard != null);
        ReversiBoard actualBoard = TestAssert.IsNotNull(updatedBoard);
        TestAssert.AreEqual(4, actualBoard.Count(DiscColor.Black));
        TestAssert.AreEqual(1, actualBoard.Count(DiscColor.White));
        TestAssert.AreEqual(2, board.Count(DiscColor.Black));
        TestAssert.AreEqual(2, board.Count(DiscColor.White));
    }

    internal void TryApplyMove_MultipleDirections_FlipsEveryLine()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            "........", ".B.B.B..", "..WWW...", ".BW.WB..",
            "..WWW...", ".B.B.B..", "........", "........");

        // Act
        bool applied = board.TryApplyMove(
            DiscColor.Black,
            new BoardPosition(3, 3),
            out ReversiBoard? updatedBoard);

        // Assert
        TestAssert.AreEqual(true, applied);
        TestAssert.AreEqual(true, updatedBoard != null);
        ReversiBoard actualBoard = TestAssert.IsNotNull(updatedBoard);
        TestAssert.AreEqual(17, actualBoard.Count(DiscColor.Black));
        TestAssert.AreEqual(0, actualBoard.Count(DiscColor.White));
    }

    internal void FromRows_ValidSnapshot_ParsesEveryCell()
    {
        // Arrange

        // Act
        ReversiBoard board = ReversiBoard.FromRows(
            "B.......", ".W......", "........", "........",
            "........", "........", "........", ".......B");

        // Assert
        TestAssert.AreEqual(2, board.Count(DiscColor.Black));
        TestAssert.AreEqual(1, board.Count(DiscColor.White));
        TestAssert.AreEqual(61, board.Count(DiscColor.Empty));
    }

    internal void FromRows_InvalidCell_ThrowsArgumentException()
    {
        // Arrange

        // Act
        bool threw = false;
        try
        {
            ReversiBoard.FromRows(
                "X.......", "........", "........", "........",
                "........", "........", "........", "........");
        }
        catch (ArgumentException)
        {
            threw = true;
        }

        // Assert
        TestAssert.AreEqual(true, threw);
    }

    private static bool Contains(
        IReadOnlyList<BoardPosition> positions,
        BoardPosition expected)
    {
        foreach (BoardPosition position in positions)
        {
            if (position.Equals(expected))
            {
                return true;
            }
        }

        return false;
    }
}
