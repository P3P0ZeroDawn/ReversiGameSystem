using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain.ReversiEngineTests;

[TestClass]
[TestCategory("Unit")]
public sealed class IsLegalMoveTests
{
    [TestMethod]
    public void IsLegalMove_NullBoard_ThrowsArgumentNullException()
    {
        const string expectedParamName = "board";
        Action action = () => ReversiEngine.IsLegalMove(null!, 2, 3, ReversiEngine.Black);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        string actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    public void IsLegalMove_InvalidBoardLength_ThrowsArgumentException()
    {
        int[] board = new int[10];
        const string expectedParamName = "board";
        Action action = () => ReversiEngine.IsLegalMove(board, 2, 3, ReversiEngine.Black);

        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        string actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    public void IsLegalMove_LegalOpeningMove_ReturnsTrueWithoutMutatingBoard()
    {
        int[] board = ReversiEngine.CreateInitialBoard();
        bool expectedIsLegal = true;
        int expectedCell = ReversiEngine.Empty;

        bool actualIsLegal = ReversiEngine.IsLegalMove(board, 2, 3, ReversiEngine.Black);
        int actualCell = board[(2 * ReversiEngine.BoardSize) + 3];

        Assert.AreEqual(expectedIsLegal, actualIsLegal);
        Assert.AreEqual(expectedCell, actualCell);
    }

    [TestMethod]
    public void IsLegalMove_IllegalCornerMove_ReturnsFalseWithoutMutatingBoard()
    {
        int[] board = ReversiEngine.CreateInitialBoard();
        bool expectedIsLegal = false;
        int expectedCell = ReversiEngine.Empty;

        bool actualIsLegal = ReversiEngine.IsLegalMove(board, 0, 0, ReversiEngine.Black);
        int actualCell = board[0];

        Assert.AreEqual(expectedIsLegal, actualIsLegal);
        Assert.AreEqual(expectedCell, actualCell);
    }
}
