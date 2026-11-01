using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Leaderboards;

/// <summary>
/// Defines access to persisted player statistics used by the leaderboard.
/// </summary>
public interface ILeaderboardRepository
{
    /// <summary>Retrieves active players and their counted match statistics.</summary>
    /// <returns>The persisted player statistics.</returns>
    public Task<IReadOnlyList<LeaderboardPlayer>> GetPlayersAsync();
}
