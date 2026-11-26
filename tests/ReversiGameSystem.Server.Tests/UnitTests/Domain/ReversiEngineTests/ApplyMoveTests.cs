using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain.ReversiEngineTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ApplyMoveTests
{
    [TestMethod]
    public void ApplyMove_NullBoard_ThrowsArgumentNullException()
    {
        const string expectedParamName = "board";
        Action action = () => ReversiEngine.ApplyMove(null!, 2, 3, ReversiEngine.Black);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        string actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    public void ApplyMove_InvalidBoardLength_ThrowsArgumentException()
    {
        int[] board = new int[10];
        const string expectedParamName = "board";
        Action action = () => ReversiEngine.ApplyMove(board, 2, 3, ReversiEngine.Black);

        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        string actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    [DataRow(2, 3, ReversiEngine.Empty)]
    [DataRow(-1, 3, ReversiEngine.Black)]
    [DataRow(3, 3, ReversiEngine.Black)]
    [DataRow(0, 0, ReversiEngine.Black)]
    public void ApplyMove_IllegalPlacement_ReturnsEmptyAndLeavesBoardUnchanged(
        int row,
        int column,
        int disc)
    {
        int[] board = ReversiEngine.CreateInitialBoard();
        const int expectedFlippedCount = 0;
        int expectedEmptyCell = ReversiEngine.Empty;
        int expectedWhiteCell = ReversiEngine.White;

        int[] flipped = ReversiEngine.ApplyMove(board, row, column, disc);
        int actualFlippedCount = flipped.Length;
        int actualEmptyCell = board[(2 * ReversiEngine.BoardSize) + 3];
        int actualWhiteCell = board[(3 * ReversiEngine.BoardSize) + 3];

        Assert.AreEqual(expectedFlippedCount, actualFlippedCount);
        Assert.AreEqual(expectedEmptyCell, actualEmptyCell);
        Assert.AreEqual(expectedWhiteCell, actualWhiteCell);
    }

    [TestMethod]
    public void ApplyMove_ValidMove_PlacesDiscAndFlipsBracketedCells()
    {
        int[] board = ReversiEngine.CreateInitialBoard();
        const int expectedFlippedCount = 1;
        const int expectedFlippedIndex = (3 * ReversiEngine.BoardSize) + 3;
        int expectedBlack = ReversiEngine.Black;

        int[] flipped = ReversiEngine.ApplyMove(board, 2, 3, ReversiEngine.Black);
        int actualFlippedCount = flipped.Length;
        int actualFlippedIndex = flipped[0];
        int actualPlacedCell = board[(2 * ReversiEngine.BoardSize) + 3];
        int actualFlippedCell = board[(3 * ReversiEngine.BoardSize) + 3];

        Assert.AreEqual(expectedFlippedCount, actualFlippedCount);
        Assert.AreEqual(expectedFlippedIndex, actualFlippedIndex);
        Assert.AreEqual(expectedBlack, actualPlacedCell);
        Assert.AreEqual(expectedBlack, actualFlippedCell);
    }

    [TestMethod]
    public void ApplyMove_MultiDirectionalCaptures_FlipsAllLines()
    {
        int[] board = new int[ReversiEngine.CellCount];
        board[(3 * ReversiEngine.BoardSize) + 4] = ReversiEngine.White;
        board[(3 * ReversiEngine.BoardSize) + 5] = ReversiEngine.Black;
        board[(4 * ReversiEngine.BoardSize) + 3] = ReversiEngine.White;
        board[(5 * ReversiEngine.BoardSize) + 3] = ReversiEngine.Black;
        board[(4 * ReversiEngine.BoardSize) + 4] = ReversiEngine.White;
        board[(5 * ReversiEngine.BoardSize) + 5] = ReversiEngine.Black;
        const int expectedFlippedCount = 3;
        int expectedBlack = ReversiEngine.Black;

        int[] flipped = ReversiEngine.ApplyMove(board, 3, 3, ReversiEngine.Black);
        int actualFlippedCount = flipped.Length;
        int actualOriginCell = board[(3 * ReversiEngine.BoardSize) + 3];
        int actualHorizontalCell = board[(3 * ReversiEngine.BoardSize) + 4];
        int actualVerticalCell = board[(4 * ReversiEngine.BoardSize) + 3];
        int actualDiagonalCell = board[(4 * ReversiEngine.BoardSize) + 4];

        Assert.AreEqual(expectedFlippedCount, actualFlippedCount);
        Assert.AreEqual(expectedBlack, actualOriginCell);
        Assert.AreEqual(expectedBlack, actualHorizontalCell);
        Assert.AreEqual(expectedBlack, actualVerticalCell);
        Assert.AreEqual(expectedBlack, actualDiagonalCell);
    }
}
