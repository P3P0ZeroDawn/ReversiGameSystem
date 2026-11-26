using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.LeaderboardApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class GetLeaderboardTests : LeaderboardApplicationServiceTestBase
{
    [TestMethod]
    public void GetLeaderboard_ValidPlayers_RanksByWinsThenWinRateThenUsername()
    {
        Repository.Players = new[]
        {
            new LeaderboardPlayer(1, "Zero", null, 0, 0),
            new LeaderboardPlayer(2, "beta", "image", 10, 7),
            new LeaderboardPlayer(3, "Charlie", null, 12, 8),
            new LeaderboardPlayer(4, "Alpha", null, 10, 7),
            new LeaderboardPlayer(5, "Delta", null, 8, 7)
        };
        string[] expectedOrder = { "Charlie", "Delta", "Alpha", "beta", "Zero" };

        LeaderboardResult result = Service.GetLeaderboard(10);
        string[] actualOrder = result.Entries
            .Select(entry => entry.Username)
            .ToArray();

        CollectionAssert.AreEqual(expectedOrder, actualOrder);
    }

    [TestMethod]
    public void GetLeaderboard_ValidPlayers_AssignsOneBasedPositions()
    {
        Repository.Players = new[]
        {
            new LeaderboardPlayer(1, "Second", null, 10, 5),
            new LeaderboardPlayer(2, "First", null, 10, 8)
        };
        int[] expectedPositions = { 1, 2 };

        LeaderboardResult result = Service.GetLeaderboard(10);
        int[] actualPositions = result.Entries
            .Select(entry => entry.Position)
            .ToArray();

        CollectionAssert.AreEqual(expectedPositions, actualPositions);
    }

    [TestMethod]
    public void GetLeaderboard_MorePlayersThanMaximum_LimitsReturnedEntries()
    {
        Repository.Players = new[]
        {
            new LeaderboardPlayer(1, "One", null, 10, 9),
            new LeaderboardPlayer(2, "Two", null, 10, 8),
            new LeaderboardPlayer(3, "Three", null, 10, 7)
        };
        int expectedCount = 2;

        LeaderboardResult result = Service.GetLeaderboard(expectedCount);
        int actualCount = result.Entries.Count;

        Assert.AreEqual(expectedCount, actualCount);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(LeaderboardApplicationService.MaximumAllowedEntries)]
    public void GetLeaderboard_BoundaryMaximums_ReturnsSuccessWithRequestedCount(
        int maximumEntries)
    {
        Repository.Players = Enumerable.Range(1, 101)
            .Select(index => new LeaderboardPlayer(
                index,
                $"Player{index:D3}",
                null,
                1,
                1))
            .ToArray();
        LeaderboardStatus expectedStatus = LeaderboardStatus.Success;
        int expectedCount = maximumEntries;

        LeaderboardResult result = Service.GetLeaderboard(maximumEntries);
        LeaderboardStatus actualStatus = result.Status;
        int actualCount = result.Entries.Count;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCount, actualCount);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(LeaderboardApplicationService.MaximumAllowedEntries + 1)]
    public void GetLeaderboard_InvalidMaximum_ReturnsInvalidMaximumEntries(
        int maximumEntries)
    {
        LeaderboardStatus expectedStatus = LeaderboardStatus.InvalidMaximumEntries;
        bool expectedWasCalled = false;

        LeaderboardResult result = Service.GetLeaderboard(maximumEntries);
        LeaderboardStatus actualStatus = result.Status;
        bool actualWasCalled = Repository.WasCalled;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedWasCalled, actualWasCalled);
    }

    [TestMethod]
    public void GetLeaderboard_EmptyRepository_ReturnsSuccessWithEmptyEntries()
    {
        LeaderboardStatus expectedStatus = LeaderboardStatus.Success;
        int expectedCount = 0;

        LeaderboardResult result = Service.GetLeaderboard(10);
        LeaderboardStatus actualStatus = result.Status;
        int actualCount = result.Entries.Count;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCount, actualCount);
    }

    [TestMethod]
    public void GetLeaderboard_ZeroMatchesPlayed_CalculatesZeroWinRate()
    {
        Repository.Players = new[]
        {
            new LeaderboardPlayer(1, "NewPlayer", null, 0, 0)
        };
        double expectedWinRate = 0d;

        LeaderboardResult result = Service.GetLeaderboard(10);
        double actualWinRate = result.Entries[0].WinRate;

        Assert.AreEqual(expectedWinRate, actualWinRate);
    }

    [TestMethod]
    public void GetLeaderboard_RepositoryThrows_PropagatesException()
    {
        Repository.ThrowOnGet = true;
        string expectedMessage = "Simulated leaderboard repository failure.";
        Action action = () => Service.GetLeaderboard(10);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(action);
        string actualMessage = exception.Message;

        Assert.AreEqual(expectedMessage, actualMessage);
    }
}
