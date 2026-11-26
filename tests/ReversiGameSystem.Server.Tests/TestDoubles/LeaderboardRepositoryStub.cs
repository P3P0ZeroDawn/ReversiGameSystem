using System;
using System.Collections.Generic;
using ReversiGameSystem.Server.Application.Leaderboards;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

public sealed class LeaderboardRepositoryStub : ILeaderboardRepository
{
    public bool WasCalled { get; private set; }

    public bool ThrowOnGet { get; set; }

    public IReadOnlyList<LeaderboardPlayer> Players { get; set; } =
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
