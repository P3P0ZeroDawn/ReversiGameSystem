using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Leaderboards;

namespace ReversiGameSystem.Server.Tests;

internal sealed class LeaderboardApplicationServiceTests
{
    internal void GetAsync_ValidRequest_RanksAndLimitsEntries()
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
        LeaderboardResult result = applicationService.GetAsync(3).GetAwaiter().GetResult();

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

    internal void GetAsync_InvalidMaximum_RejectsBeforeRepositoryAccess()
    {
        // Arrange
        LeaderboardRepositoryStub repository = new LeaderboardRepositoryStub();
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(repository);

        // Act
        LeaderboardResult belowMinimum = applicationService.GetAsync(0)
            .GetAwaiter().GetResult();
        LeaderboardResult aboveMaximum = applicationService
            .GetAsync(LeaderboardApplicationService.MaximumAllowedEntries + 1)
            .GetAwaiter()
            .GetResult();

        // Assert
        TestAssert.AreEqual(
            LeaderboardStatus.InvalidMaximumEntries,
            belowMinimum.Status);
        TestAssert.AreEqual(
            LeaderboardStatus.InvalidMaximumEntries,
            aboveMaximum.Status);
        TestAssert.AreEqual(false, repository.WasCalled);
    }

    internal void GetAsync_EmptyRepository_ReturnsSuccessfulEmptyList()
    {
        // Arrange
        LeaderboardRepositoryStub repository = new LeaderboardRepositoryStub();
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(repository);

        // Act
        LeaderboardResult result = applicationService.GetAsync(10)
            .GetAwaiter().GetResult();

        // Assert
        TestAssert.AreEqual(LeaderboardStatus.Success, result.Status);
        TestAssert.AreEqual(0, result.Entries.Count);
        TestAssert.AreEqual(true, repository.WasCalled);
    }

    internal void GetAsync_BoundaryMaximums_AcceptsOneAndOneHundred()
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
            .GetAsync(1)
            .GetAwaiter()
            .GetResult();
        LeaderboardResult maximumResult =
            new LeaderboardApplicationService(maximumRepository)
            .GetAsync(LeaderboardApplicationService.MaximumAllowedEntries)
            .GetAwaiter()
            .GetResult();

        // Assert
        TestAssert.AreEqual(LeaderboardStatus.Success, minimumResult.Status);
        TestAssert.AreEqual(1, minimumResult.Entries.Count);
        TestAssert.AreEqual(LeaderboardStatus.Success, maximumResult.Status);
        TestAssert.AreEqual(100, maximumResult.Entries.Count);
    }

    internal void GetAsync_RepositoryFailure_PropagatesException()
    {
        // Arrange
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(
            new LeaderboardRepositoryStub { ThrowOnGet = true });

        // Act
        InvalidOperationException exception = TestAssert.Throws<InvalidOperationException>(
            () => applicationService.GetAsync(10).GetAwaiter().GetResult());

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

    public Task<IReadOnlyList<LeaderboardPlayer>> GetPlayersAsync()
    {
        WasCalled = true;
        if (ThrowOnGet)
        {
            throw new InvalidOperationException(
                "Simulated leaderboard repository failure.");
        }

        return Task.FromResult(Players);
    }
}
