using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;
using ApplicationService =
    ReversiGameSystem.Server.Application.Leaderboards.LeaderboardApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.LeaderboardApplicationService;

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

        Assert.AreEqual(expectedCount, result.Entries.Count);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(ApplicationService.MaximumAllowedEntries)]
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

        LeaderboardResult result = Service.GetLeaderboard(maximumEntries);

        Assert.AreEqual(LeaderboardStatus.Success, result.Status);
        Assert.AreEqual(maximumEntries, result.Entries.Count);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(ApplicationService.MaximumAllowedEntries + 1)]
    public void GetLeaderboard_InvalidMaximum_ReturnsInvalidMaximumEntries(
        int maximumEntries)
    {
        LeaderboardResult result = Service.GetLeaderboard(maximumEntries);

        Assert.AreEqual(LeaderboardStatus.InvalidMaximumEntries, result.Status);
        Assert.IsFalse(Repository.WasCalled);
    }

    [TestMethod]
    public void GetLeaderboard_EmptyRepository_ReturnsSuccessWithEmptyEntries()
    {
        LeaderboardResult result = Service.GetLeaderboard(10);

        Assert.AreEqual(LeaderboardStatus.Success, result.Status);
        Assert.AreEqual(0, result.Entries.Count);
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

        Assert.AreEqual(expectedWinRate, result.Entries[0].WinRate);
    }

    [TestMethod]
    public void GetLeaderboard_RepositoryThrows_PropagatesException()
    {
        Repository.ThrowOnGet = true;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => Service.GetLeaderboard(10));

        Assert.AreEqual(
            "Simulated leaderboard repository failure.",
            exception.Message);
    }
}
