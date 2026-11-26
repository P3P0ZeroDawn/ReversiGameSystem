using System.Collections.Generic;

namespace ReversiGameSystem.Server.Application.Leaderboards;

public interface ILeaderboardRepository
{
    public IReadOnlyList<LeaderboardPlayer> GetLeaderboardPlayers();
}
