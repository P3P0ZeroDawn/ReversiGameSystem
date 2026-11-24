using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Verifies the stateless Reversi rules engine in <see cref="ReversiEngine"/>.
/// </summary>
[TestClass]
public sealed class ReversiDomainTests
{
    [TestMethod]
    public void CreateInitialBoard_StandardSetup_PlacesFourCenteredDiscs()
    {
        // Arrange & Act
        int[] board = ReversiEngine.CreateInitialBoard();

        // Assert
        Assert.AreEqual(ReversiEngine.CellCount, board.Length);
        Assert.AreEqual(ReversiEngine.White, board[(3 * 8) + 3]);
        Assert.AreEqual(ReversiEngine.Black, board[(3 * 8) + 4]);
        Assert.AreEqual(ReversiEngine.Black, board[(4 * 8) + 3]);
        Assert.AreEqual(ReversiEngine.White, board[(4 * 8) + 4]);
    }

    [TestMethod]
    public void ApplyMove_InvalidBoardArray_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(
            () =>
            {
                ReversiEngine.ApplyMove(new int[10], 2, 3, ReversiEngine.Black);
            });
    }

    [TestMethod]
    public void ApplyMove_IllegalPlacements_ReturnsEmptyAndLeavesBoardUnchanged()
    {
        // Arrange
        int[] board = ReversiEngine.CreateInitialBoard();

        // Act
        int[] invalidDisc = ReversiEngine.ApplyMove(board, 2, 3, ReversiEngine.Empty);
        int[] outOfBounds = ReversiEngine.ApplyMove(board, -1, 3, ReversiEngine.Black);
        int[] occupied = ReversiEngine.ApplyMove(board, 3, 3, ReversiEngine.Black);
        int[] noCaptures = ReversiEngine.ApplyMove(board, 0, 0, ReversiEngine.Black);

        // Assert
        Assert.AreEqual(0, invalidDisc.Length);
        Assert.AreEqual(0, outOfBounds.Length);
        Assert.AreEqual(0, occupied.Length);
        Assert.AreEqual(0, noCaptures.Length);
        Assert.AreEqual(ReversiEngine.Empty, board[(2 * 8) + 3]);
        Assert.AreEqual(ReversiEngine.White, board[(3 * 8) + 3]);
    }

    [TestMethod]
    public void ApplyMove_ValidMove_PlacesDiscAndFlipsBracketedCells()
    {
        // Arrange
        int[] board = ReversiEngine.CreateInitialBoard();

        // Act: Black plays at (2, 3), capturing White at (3, 3)
        int[] flipped = ReversiEngine.ApplyMove(board, 2, 3, ReversiEngine.Black);

        // Assert
        Assert.AreEqual(1, flipped.Length);
        Assert.AreEqual((3 * 8) + 3, flipped[0]);
        Assert.AreEqual(ReversiEngine.Black, board[(2 * 8) + 3]);
        Assert.AreEqual(ReversiEngine.Black, board[(3 * 8) + 3]);
    }

    [TestMethod]
    public void ApplyMove_MultiDirectionalCaptures_FlipsAllLines()
    {
        // Arrange
        int[] board = new int[ReversiEngine.CellCount];
        board[(3 * 8) + 4] = ReversiEngine.White;
        board[(3 * 8) + 5] = ReversiEngine.Black;
        board[(4 * 8) + 3] = ReversiEngine.White;
        board[(5 * 8) + 3] = ReversiEngine.Black;
        board[(4 * 8) + 4] = ReversiEngine.White;
        board[(5 * 8) + 5] = ReversiEngine.Black;

        // Act
        int[] flipped = ReversiEngine.ApplyMove(board, 3, 3, ReversiEngine.Black);

        // Assert
        Assert.AreEqual(3, flipped.Length);
        Assert.AreEqual(ReversiEngine.Black, board[(3 * 8) + 3]);
        Assert.AreEqual(ReversiEngine.Black, board[(3 * 8) + 4]);
        Assert.AreEqual(ReversiEngine.Black, board[(4 * 8) + 3]);
        Assert.AreEqual(ReversiEngine.Black, board[(4 * 8) + 4]);
    }

    [TestMethod]
    public void HasValidMoves_EvaluatesBoardAvailabilityAccurately()
    {
        // Arrange
        int[] initialBoard = ReversiEngine.CreateInitialBoard();
        int[] emptyBoard = new int[ReversiEngine.CellCount];

        // Act & Assert
        Assert.IsTrue(
            ReversiEngine.HasValidMoves(initialBoard, ReversiEngine.Black),
            "Expected Black to have valid opening moves.");
        Assert.IsTrue(
            ReversiEngine.HasValidMoves(initialBoard, ReversiEngine.White),
            "Expected White to have valid moves on initial board.");
        Assert.IsTrue(
            !ReversiEngine.HasValidMoves(emptyBoard, ReversiEngine.Black),
            "Expected no valid moves on an empty board.");
    }

    [TestMethod]
    public void IsLegalMove_BoardEvaluation_DetectsLegalityWithoutMutatingBoard()
    {
        // Arrange
        int[] board = ReversiEngine.CreateInitialBoard();

        // Act
        bool legalOpening = ReversiEngine.IsLegalMove(board, 2, 3, ReversiEngine.Black);
        bool illegalCorner = ReversiEngine.IsLegalMove(board, 0, 0, ReversiEngine.Black);

        // Assert
        Assert.IsTrue(legalOpening, "Expected (2, 3) to be a legal move for Black.");
        Assert.IsTrue(!illegalCorner, "Expected (0, 0) to be illegal on initial board.");
        Assert.AreEqual(ReversiEngine.Empty, board[(2 * 8) + 3]);
    }
}
