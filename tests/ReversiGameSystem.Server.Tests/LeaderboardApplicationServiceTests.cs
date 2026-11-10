using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Application.Leaderboards;

namespace ReversiGameSystem.Server.Tests;

internal sealed class LeaderboardApplicationServiceTests
{
    internal void Get_ValidRequest_RanksAndLimitsEntries()
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
        LeaderboardResult result = applicationService.Get(3);

        // Assert
        TestAssert.AreEqual(LeaderboardStatus.Success, result.Status);
        TestAssert.AreEqual(true, repository.WasCalled);
        TestAssert.AreEqual(3, result.Entries.Count);
        TestAssert.AreEqual("Charlie", result.Entries[0].Username);
        TestAssert.AreEqual(1, result.Entries[0].Position);
        TestAssert.AreEqual("Alpha", result.Entries[1].Username);
        TestAssert.AreEqual(2, result.Entries[1].Position);
        TestAssert.AreEqual("beta", result.Entries[2].Username);
        TestAssert.AreEqual(0.7d, result.Entries[2].WinRate);
    }

    internal void Get_InvalidMaximum_RejectsBeforeRepositoryAccess()
    {
        // Arrange
        LeaderboardRepositoryStub repository = new LeaderboardRepositoryStub();
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(repository);

        // Act
        LeaderboardResult belowMinimum = applicationService.Get(0);
        LeaderboardResult aboveMaximum = applicationService
            .Get(LeaderboardApplicationService.MaximumAllowedEntries + 1);

        // Assert
        TestAssert.AreEqual(
            LeaderboardStatus.InvalidMaximumEntries,
            belowMinimum.Status);
        TestAssert.AreEqual(
            LeaderboardStatus.InvalidMaximumEntries,
            aboveMaximum.Status);
        TestAssert.AreEqual(false, repository.WasCalled);
    }

    internal void Get_EmptyRepository_ReturnsSuccessfulEmptyList()
    {
        // Arrange
        LeaderboardRepositoryStub repository = new LeaderboardRepositoryStub();
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(repository);

        // Act
        LeaderboardResult result = applicationService.Get(10);

        // Assert
        TestAssert.AreEqual(LeaderboardStatus.Success, result.Status);
        TestAssert.AreEqual(0, result.Entries.Count);
        TestAssert.AreEqual(true, repository.WasCalled);
    }

    internal void Get_BoundaryMaximums_AcceptsOneAndOneHundred()
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
            .Get(1);
        LeaderboardResult maximumResult =
            new LeaderboardApplicationService(maximumRepository)
            .Get(LeaderboardApplicationService.MaximumAllowedEntries);

        // Assert
        TestAssert.AreEqual(LeaderboardStatus.Success, minimumResult.Status);
        TestAssert.AreEqual(1, minimumResult.Entries.Count);
        TestAssert.AreEqual(LeaderboardStatus.Success, maximumResult.Status);
        TestAssert.AreEqual(100, maximumResult.Entries.Count);
    }

    internal void Get_RepositoryFailure_PropagatesException()
    {
        // Arrange
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(
            new LeaderboardRepositoryStub { ThrowOnGet = true });

        // Act
        InvalidOperationException exception = TestAssert.Throws<InvalidOperationException>(
            () => applicationService.Get(10));

        // Assert
        TestAssert.AreEqual("Simulated leaderboard repository failure.", exception.Message);
    }
}

internal sealed class LeaderboardRepositoryStub : ILeaderboardRepository
{
    internal bool WasCalled { get; private set; }

    internal bool ThrowOnGet { get; set; }

    internal IReadOnlyList<LeaderboardPlayer> Players { get; set; } =
        Array.Empty<LeaderboardPlayer>();

    public IReadOnlyList<LeaderboardPlayer> GetPlayers()
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
