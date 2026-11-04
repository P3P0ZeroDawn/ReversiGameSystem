using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Leaderboards;

/// <summary>
/// Retrieves and ranks the global leaderboard from persisted player statistics.
/// </summary>
public sealed class LeaderboardApplicationService
{
    /// <summary>Represents the maximum accepted number of leaderboard entries.</summary>
    public const int MaximumAllowedEntries = 100;

    private readonly ILeaderboardRepository _leaderboardRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardApplicationService"/> class.
    /// </summary>
    /// <param name="leaderboardRepository">The leaderboard data repository.</param>
    public LeaderboardApplicationService(ILeaderboardRepository leaderboardRepository)
    {
        _leaderboardRepository = leaderboardRepository
            ?? throw new ArgumentNullException(nameof(leaderboardRepository));
    }

    /// <summary>Retrieves, orders, and limits the global leaderboard.</summary>
    /// <param name="maximumEntries">The maximum number of entries to return.</param>
    /// <returns>The application result.</returns>
    public async Task<LeaderboardResult> GetAsync(int maximumEntries)
    {
        if (maximumEntries < 1 || maximumEntries > MaximumAllowedEntries)
        {
            return new LeaderboardResult(LeaderboardStatus.InvalidMaximumEntries);
        }

        IReadOnlyList<LeaderboardPlayer> players =
            await _leaderboardRepository.GetPlayersAsync();
        List<LeaderboardPlayer> rankedPlayers = players
            .OrderByDescending(player => player.Wins)
            .ThenByDescending(CalculateWinRate)
            .ThenBy(player => player.Username, StringComparer.OrdinalIgnoreCase)
            .Take(maximumEntries)
            .ToList();

        return new LeaderboardResult(
            LeaderboardStatus.Success,
            CreateEntries(rankedPlayers));
    }

    private static IReadOnlyList<LeaderboardEntry> CreateEntries(
        IReadOnlyList<LeaderboardPlayer> players)
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        for (int index = 0; index < players.Count; index++)
        {
            LeaderboardPlayer player = players[index];
            entries.Add(new LeaderboardEntry(
                index + 1,
                player.AccountId,
                player.Username,
                player.ProfileImageUri,
                player.MatchesPlayed,
                player.Wins,
                CalculateWinRate(player)));
        }

        return entries;
    }

    private static double CalculateWinRate(LeaderboardPlayer player)
    {
        double winRate = 0d;
        if (player.MatchesPlayed > 0)
        {
            winRate = (double)player.Wins / player.MatchesPlayed;
        }

        return winRate;
    }
}
