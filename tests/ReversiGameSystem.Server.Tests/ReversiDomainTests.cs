using System;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Verifies the stateless Reversi rules engine in <see cref="ReversiEngine"/>.
/// </summary>
internal sealed class ReversiDomainTests
{
    /// <summary>Verifies that the canonical initial board contains 2 black and 2 white discs centered.</summary>
    internal void CreateInitialBoard_StandardSetup_PlacesFourCenteredDiscs()
    {
        // Arrange & Act
        int[] board = ReversiEngine.CreateInitialBoard();

        // Assert
        TestAssert.AreEqual(ReversiEngine.CellCount, board.Length);
        TestAssert.AreEqual(ReversiEngine.White, board[(3 * 8) + 3]);
        TestAssert.AreEqual(ReversiEngine.Black, board[(3 * 8) + 4]);
        TestAssert.AreEqual(ReversiEngine.Black, board[(4 * 8) + 3]);
        TestAssert.AreEqual(ReversiEngine.White, board[(4 * 8) + 4]);
    }

    /// <summary>Verifies that malformed board arrays are rejected.</summary>
    internal void ApplyMove_InvalidBoardArray_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        TestAssert.Throws<ArgumentException>(
            () =>
            {
                ReversiEngine.ApplyMove(new int[10], 2, 3, ReversiEngine.Black);
            });
    }

    /// <summary>Verifies that out-of-bounds, occupied, or non-capturing moves leave the board untouched and return empty.</summary>
    internal void ApplyMove_IllegalPlacements_ReturnsEmptyAndLeavesBoardUnchanged()
    {
        // Arrange
        int[] board = ReversiEngine.CreateInitialBoard();

        // Act
        int[] invalidDisc = ReversiEngine.ApplyMove(board, 2, 3, ReversiEngine.Empty);
        int[] outOfBounds = ReversiEngine.ApplyMove(board, -1, 3, ReversiEngine.Black);
        int[] occupied = ReversiEngine.ApplyMove(board, 3, 3, ReversiEngine.Black);
        int[] noCaptures = ReversiEngine.ApplyMove(board, 0, 0, ReversiEngine.Black);

        // Assert
        TestAssert.AreEqual(0, invalidDisc.Length);
        TestAssert.AreEqual(0, outOfBounds.Length);
        TestAssert.AreEqual(0, occupied.Length);
        TestAssert.AreEqual(0, noCaptures.Length);
        TestAssert.AreEqual(ReversiEngine.Empty, board[(2 * 8) + 3]);
        TestAssert.AreEqual(ReversiEngine.White, board[(3 * 8) + 3]);
    }

    /// <summary>Verifies that a valid placement mutates the board in place and returns the flipped indices.</summary>
    internal void ApplyMove_ValidMove_PlacesDiscAndFlipsBracketedCells()
    {
        // Arrange
        int[] board = ReversiEngine.CreateInitialBoard();

        // Act: Black plays at (2, 3), capturing White at (3, 3)
        int[] flipped = ReversiEngine.ApplyMove(board, 2, 3, ReversiEngine.Black);

        // Assert
        TestAssert.AreEqual(1, flipped.Length);
        TestAssert.AreEqual((3 * 8) + 3, flipped[0]);
        TestAssert.AreEqual(ReversiEngine.Black, board[(2 * 8) + 3]);
        TestAssert.AreEqual(ReversiEngine.Black, board[(3 * 8) + 3]);
    }

    /// <summary>Verifies multi-directional flips in horizontal, vertical, and diagonal lines simultaneously.</summary>
    internal void ApplyMove_MultiDirectionalCaptures_FlipsAllLines()
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
        TestAssert.AreEqual(3, flipped.Length);
        TestAssert.AreEqual(ReversiEngine.Black, board[(3 * 8) + 3]);
        TestAssert.AreEqual(ReversiEngine.Black, board[(3 * 8) + 4]);
        TestAssert.AreEqual(ReversiEngine.Black, board[(4 * 8) + 3]);
        TestAssert.AreEqual(ReversiEngine.Black, board[(4 * 8) + 4]);
    }

    /// <summary>Verifies legal move detection when moves exist and when no moves exist.</summary>
    internal void HasValidMoves_EvaluatesBoardAvailabilityAccurately()
    {
        // Arrange
        int[] initialBoard = ReversiEngine.CreateInitialBoard();
        int[] emptyBoard = new int[ReversiEngine.CellCount];

        // Act & Assert
        TestAssert.IsTrue(
            ReversiEngine.HasValidMoves(initialBoard, ReversiEngine.Black),
            "Expected Black to have valid opening moves.");
        TestAssert.IsTrue(
            ReversiEngine.HasValidMoves(initialBoard, ReversiEngine.White),
            "Expected White to have valid moves on initial board.");
        TestAssert.IsTrue(
            !ReversiEngine.HasValidMoves(emptyBoard, ReversiEngine.Black),
            "Expected no valid moves on an empty board.");
    }
}
