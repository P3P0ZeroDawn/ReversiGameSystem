using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain.ReversiEngineTests;

[TestClass]
[TestCategory("Unit")]
public sealed class HasValidMovesTests
{
    [TestMethod]
    public void HasValidMoves_NullBoard_ThrowsArgumentNullException()
    {
        const string expectedParamName = "board";
        Action action = () => ReversiEngine.HasValidMoves(null!, ReversiEngine.Black);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        string actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    public void HasValidMoves_InvalidBoardLength_ThrowsArgumentException()
    {
        int[] board = new int[10];
        const string expectedParamName = "board";
        Action action = () => ReversiEngine.HasValidMoves(board, ReversiEngine.Black);

        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        string actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    [DataRow(ReversiEngine.Black)]
    [DataRow(ReversiEngine.White)]
    public void HasValidMoves_InitialBoard_ReturnsTrue(int disc)
    {
        int[] board = ReversiEngine.CreateInitialBoard();
        bool expectedHasMoves = true;

        bool actualHasMoves = ReversiEngine.HasValidMoves(board, disc);

        Assert.AreEqual(expectedHasMoves, actualHasMoves);
    }

    [TestMethod]
    public void HasValidMoves_EmptyBoard_ReturnsFalse()
    {
        int[] board = new int[ReversiEngine.CellCount];
        bool expectedHasMoves = false;

        bool actualHasMoves = ReversiEngine.HasValidMoves(board, ReversiEngine.Black);

        Assert.AreEqual(expectedHasMoves, actualHasMoves);
    }
}
