using System;
using System.Collections.Generic;
using ReversiGameSystem.Server.Application.Leaderboards;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

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
