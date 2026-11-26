using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class LeaderboardApplicationServiceTests
{
    [TestMethod]
    public void GetLeaderboard_ValidRequest_RanksAndLimitsEntries()
    {
        // Arrange
        LeaderboardRepositoryStub repository = new LeaderboardRepositoryStub
        {
            Players = new[]
            {
                new LeaderboardPlayer(1, "Zero", null, 0, 0),
                new LeaderboardPlayer(2, "beta", "image", 10, 7),
                new LeaderboardPlayer(3, "Charlie", null, 12, 8),
                new LeaderboardPlayer(4, "Alpha", null, 10, 7)
            }
        };
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(repository);

        // Act
        LeaderboardResult result = applicationService.GetLeaderboard(3);

        // Assert
        Assert.AreEqual(LeaderboardStatus.Success, result.Status);
        Assert.AreEqual(true, repository.WasCalled);
        Assert.AreEqual(3, result.Entries.Count);
        Assert.AreEqual("Charlie", result.Entries[0].Username);
        Assert.AreEqual(1, result.Entries[0].Position);
        Assert.AreEqual("Alpha", result.Entries[1].Username);
        Assert.AreEqual(2, result.Entries[1].Position);
        Assert.AreEqual("beta", result.Entries[2].Username);
        Assert.AreEqual(0.7d, result.Entries[2].WinRate);
    }

    [TestMethod]
    public void GetLeaderboard_InvalidMaximum_RejectsBeforeRepositoryAccess()
    {
        // Arrange
        LeaderboardRepositoryStub repository = new LeaderboardRepositoryStub();
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(repository);

        // Act
        LeaderboardResult belowMinimum = applicationService.GetLeaderboard(0);
        LeaderboardResult aboveMaximum = applicationService
            .GetLeaderboard(LeaderboardApplicationService.MaximumAllowedEntries + 1);

        // Assert
        Assert.AreEqual(
            LeaderboardStatus.InvalidMaximumEntries,
            belowMinimum.Status);
        Assert.AreEqual(
            LeaderboardStatus.InvalidMaximumEntries,
            aboveMaximum.Status);
        Assert.AreEqual(false, repository.WasCalled);
    }

    [TestMethod]
    public void GetLeaderboard_EmptyRepository_ReturnsSuccessfulEmptyList()
    {
        // Arrange
        LeaderboardRepositoryStub repository = new LeaderboardRepositoryStub();
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(repository);

        // Act
        LeaderboardResult result = applicationService.GetLeaderboard(10);

        // Assert
        Assert.AreEqual(LeaderboardStatus.Success, result.Status);
        Assert.AreEqual(0, result.Entries.Count);
        Assert.AreEqual(true, repository.WasCalled);
    }

    [TestMethod]
    public void GetLeaderboard_BoundaryMaximums_AcceptsOneAndOneHundred()
    {
        // Arrange
        IReadOnlyList<LeaderboardPlayer> players = Enumerable.Range(1, 101)
            .Select(index => new LeaderboardPlayer(
                index,
                $"Player{index:D3}",
                null,
                1,
                1))
            .ToArray();
        LeaderboardRepositoryStub minimumRepository = new LeaderboardRepositoryStub
        {
            Players = players
        };
        LeaderboardRepositoryStub maximumRepository = new LeaderboardRepositoryStub
        {
            Players = players
        };

        // Act
        LeaderboardResult minimumResult =
            new LeaderboardApplicationService(minimumRepository)
            .GetLeaderboard(1);
        LeaderboardResult maximumResult =
            new LeaderboardApplicationService(maximumRepository)
            .GetLeaderboard(LeaderboardApplicationService.MaximumAllowedEntries);

        // Assert
        Assert.AreEqual(LeaderboardStatus.Success, minimumResult.Status);
        Assert.AreEqual(1, minimumResult.Entries.Count);
        Assert.AreEqual(LeaderboardStatus.Success, maximumResult.Status);
        Assert.AreEqual(100, maximumResult.Entries.Count);
    }

    [TestMethod]
    public void GetLeaderboard_RepositoryFailure_PropagatesException()
    {
        // Arrange
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(
            new LeaderboardRepositoryStub { ThrowOnGet = true });

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => applicationService.GetLeaderboard(10));

        // Assert
        Assert.AreEqual("Simulated leaderboard repository failure.", exception.Message);
    }
}

internal sealed class LeaderboardRepositoryStub : ILeaderboardRepository
{
    internal bool WasCalled { get; private set; }

    internal bool ThrowOnGet { get; set; }

    internal IReadOnlyList<LeaderboardPlayer> Players { get; set; } =
        Array.Empty<LeaderboardPlayer>();

    public IReadOnlyList<LeaderboardPlayer> GetLeaderboardPlayers()
    {
        WasCalled = true;
        if (ThrowOnGet)
        {
            throw new InvalidOperationException(
                "Simulated leaderboard repository failure.");
        }

        return Players;
    }
}
