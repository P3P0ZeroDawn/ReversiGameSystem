using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class ReversiBoardTests
{
    [TestMethod]
    public void CreateInitial_HasStandardFourCenterDiscs()
    {
        // Arrange

        // Act
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Assert
        Assert.AreEqual(DiscColor.White, board[3, 3]);
        Assert.AreEqual(DiscColor.Black, board[3, 4]);
        Assert.AreEqual(DiscColor.Black, board[4, 3]);
        Assert.AreEqual(DiscColor.White, board[4, 4]);
    }

    [TestMethod]
    public void CreateInitial_HasTwoDiscsOfEachColor()
    {
        // Arrange

        // Act
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Assert
        Assert.AreEqual(2, board.Count(DiscColor.Black));
        Assert.AreEqual(2, board.Count(DiscColor.White));
        Assert.AreEqual(60, board.Count(DiscColor.Empty));
    }

    [TestMethod]
    public void GetValidMoves_InitialBlackTurn_ReturnsFourPositions()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Act
        IReadOnlyList<BoardPosition> moves = board.GetValidMoves(DiscColor.Black);

        // Assert
        Assert.AreEqual(4, moves.Count);
        Assert.AreEqual(true, Contains(moves, new BoardPosition(2, 3)));
        Assert.AreEqual(true, Contains(moves, new BoardPosition(3, 2)));
        Assert.AreEqual(true, Contains(moves, new BoardPosition(4, 5)));
        Assert.AreEqual(true, Contains(moves, new BoardPosition(5, 4)));
    }

    [TestMethod]
    public void TryApplyMove_ValidMove_FlipsBracketedPiece()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Act
        bool applied = board.TryApplyMove(
            DiscColor.Black,
            new BoardPosition(2, 3),
            out ReversiBoard? updatedBoard);

        // Assert
        Assert.AreEqual(true, applied);
        Assert.AreEqual(true, updatedBoard != null);
        ReversiBoard actualBoard = TestAssert.IsNotNull(updatedBoard);
        Assert.AreEqual(4, actualBoard.Count(DiscColor.Black));
        Assert.AreEqual(1, actualBoard.Count(DiscColor.White));
        Assert.AreEqual(2, board.Count(DiscColor.Black));
        Assert.AreEqual(2, board.Count(DiscColor.White));
    }

    [TestMethod]
    public void TryApplyMove_MultipleDirections_FlipsEveryLine()
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
        Assert.AreEqual(true, applied);
        Assert.AreEqual(true, updatedBoard != null);
        ReversiBoard actualBoard = TestAssert.IsNotNull(updatedBoard);
        Assert.AreEqual(17, actualBoard.Count(DiscColor.Black));
        Assert.AreEqual(0, actualBoard.Count(DiscColor.White));
    }

    [TestMethod]
    public void FromRows_ValidSnapshot_ParsesEveryCell()
    {
        // Arrange

        // Act
        ReversiBoard board = ReversiBoard.FromRows(
            "B.......", ".W......", "........", "........",
            "........", "........", "........", ".......B");

        // Assert
        Assert.AreEqual(2, board.Count(DiscColor.Black));
        Assert.AreEqual(1, board.Count(DiscColor.White));
        Assert.AreEqual(61, board.Count(DiscColor.Empty));
    }

    [TestMethod]
    public void FromRows_InvalidCell_ThrowsArgumentException()
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
        Assert.AreEqual(true, threw);
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
