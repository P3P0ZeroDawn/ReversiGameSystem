using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain.ReversiEngineTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CreateInitialBoardTests
{
    [TestMethod]
    public void CreateInitialBoard_StandardSetup_PlacesFourCenteredDiscs()
    {
        int expectedCellCount = ReversiEngine.CellCount;
        int expectedWhite = ReversiEngine.White;
        int expectedBlack = ReversiEngine.Black;

        int[] board = ReversiEngine.CreateInitialBoard();
        int actualCellCount = board.Length;
        int actualTopLeft = board[(3 * ReversiEngine.BoardSize) + 3];
        int actualTopRight = board[(3 * ReversiEngine.BoardSize) + 4];
        int actualBottomLeft = board[(4 * ReversiEngine.BoardSize) + 3];
        int actualBottomRight = board[(4 * ReversiEngine.BoardSize) + 4];

        Assert.AreEqual(expectedCellCount, actualCellCount);
        Assert.AreEqual(expectedWhite, actualTopLeft);
        Assert.AreEqual(expectedBlack, actualTopRight);
        Assert.AreEqual(expectedBlack, actualBottomLeft);
        Assert.AreEqual(expectedWhite, actualBottomRight);
    }
}
