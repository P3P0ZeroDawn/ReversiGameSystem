using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Leaderboards;

namespace ReversiGameSystem.Tests;

internal sealed class LeaderboardHandlerTests
{
    internal void ExecuteAsync_ValidRequest_ReturnsRankedEntries()
    {
        LeaderboardEntry expectedEntry = new LeaderboardEntry(
            1,
            42,
            "PlayerOne",
            "profile-images/player.png",
            12,
            7,
            7d / 12d);
        LeaderboardStoreStub store = new LeaderboardStoreStub
        {
            Result = new LeaderboardStoreResult(
                LeaderboardStoreOutcome.Success,
                new[] { expectedEntry })
        };
        GetLeaderboardHandler handler = new GetLeaderboardHandler(store);

        LeaderboardResult result = handler.ExecuteAsync(new LeaderboardQuery(10))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LeaderboardOutcome.Success, result.Outcome);
        TestAssert.AreEqual(10, store.MaximumEntries);
        TestAssert.AreEqual(1, result.Entries.Count);
        TestAssert.AreEqual("PlayerOne", result.Entries[0].Username);
    }

    internal void ExecuteAsync_InvalidMaximum_ReturnsInvalidRequest()
    {
        LeaderboardStoreStub store = new LeaderboardStoreStub();
        GetLeaderboardHandler handler = new GetLeaderboardHandler(store);

        LeaderboardResult belowMinimum = handler.ExecuteAsync(new LeaderboardQuery(0))
            .GetAwaiter()
            .GetResult();
        LeaderboardResult aboveMaximum = handler.ExecuteAsync(
            new LeaderboardQuery(GetLeaderboardHandler.MaximumAllowedEntries + 1))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LeaderboardOutcome.InvalidRequest, belowMinimum.Outcome);
        TestAssert.AreEqual(LeaderboardOutcome.InvalidRequest, aboveMaximum.Outcome);
        TestAssert.AreEqual<int?>(null, store.MaximumEntries);
    }

    internal void ExecuteAsync_UnavailableStore_ReturnsServiceUnavailable()
    {
        GetLeaderboardHandler handler = new GetLeaderboardHandler(
            new LeaderboardStoreStub
            {
                Result = new LeaderboardStoreResult(LeaderboardStoreOutcome.Unavailable)
            });

        LeaderboardResult result = handler.ExecuteAsync(new LeaderboardQuery(10))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LeaderboardOutcome.ServiceUnavailable, result.Outcome);
    }

    internal void ExecuteAsync_StoreFailure_ReturnsServiceUnavailable()
    {
        GetLeaderboardHandler handler = new GetLeaderboardHandler(
            new LeaderboardStoreStub { ThrowOnGet = true });

        LeaderboardResult result = handler.ExecuteAsync(new LeaderboardQuery(10))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(LeaderboardOutcome.ServiceUnavailable, result.Outcome);
    }
}

internal sealed class LeaderboardStoreStub : ILeaderboardStore
{
    internal int? MaximumEntries { get; private set; }

    internal bool ThrowOnGet { get; set; }

    internal LeaderboardStoreResult Result { get; set; } =
        new LeaderboardStoreResult(
            LeaderboardStoreOutcome.Success,
            Array.Empty<LeaderboardEntry>());

    public Task<LeaderboardStoreResult> GetAsync(int maximumEntries)
    {
        MaximumEntries = maximumEntries;
        if (ThrowOnGet)
        {
            throw new InvalidOperationException("Simulated leaderboard store failure.");
        }

        return Task.FromResult(Result);
    }
}
