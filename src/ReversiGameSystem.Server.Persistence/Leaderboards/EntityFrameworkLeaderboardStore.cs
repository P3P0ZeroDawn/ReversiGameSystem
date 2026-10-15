using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Leaderboards;

/// <summary>
/// Represents entity framework leaderboard store.
/// </summary>
public sealed class EntityFrameworkLeaderboardStore : ILeaderboardStore
{
    private const string ActiveAccountStatusCode = "ACTIVE";
    private const string FinishedMatchStatusCode = "FINISHED";
    private const string WinResultCode = "WIN";

    /// <summary>
    /// Performs the get async operation.
    /// </summary>
    /// <param name="maximumEntries">The maximum entries.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<LeaderboardStoreResult> GetAsync(int maximumEntries)
    {
        LeaderboardStoreResult result;
        try
        {
            using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
            {
                List<LeaderboardPlayer> players = await LoadPlayersAsync(context);
                List<PlayerResultCount> counts = await LoadResultCountsAsync(context);
                List<RankedLeaderboardPlayer> rankedPlayers = RankPlayers(
                    players,
                    counts,
                    maximumEntries);
                List<LeaderboardEntry> entries = CreateEntries(rankedPlayers);
                result = new LeaderboardStoreResult(LeaderboardStoreOutcome.Success, entries);
            }
        }
        catch (Exception exception) when (exception is EntityException
                                            or SqlException
                                            or InvalidOperationException
                                            or NotSupportedException)
        {
            result = new LeaderboardStoreResult(LeaderboardStoreOutcome.Unavailable);
        }

        return result;
    }

    private static Task<List<LeaderboardPlayer>> LoadPlayersAsync(
        ReversiGameSystemEntities context)
    {
        return context.Account
            .AsNoTracking()
            .Where(account => account.statusCode == ActiveAccountStatusCode)
            .Select(account => new LeaderboardPlayer
            {
                AccountId = account.accountId,
                Username = account.username,
                ProfileImageUri = account.profileImageUri
            })
            .ToListAsync();
    }

    private static Task<List<PlayerResultCount>> LoadResultCountsAsync(
        ReversiGameSystemEntities context)
    {
        return context.MatchParticipant
            .AsNoTracking()
            .Where(participant => participant.accountId.HasValue
                && participant.GameMatch1.statusCode == FinishedMatchStatusCode
                && participant.GameMatch1.countsTowardStatistics
                && participant.resultCode != null)
            .GroupBy(participant => new
            {
                AccountId = participant.accountId ?? 0L,
                participant.resultCode
            })
            .Select(group => new PlayerResultCount
            {
                AccountId = group.Key.AccountId,
                ResultCode = group.Key.resultCode,
                Count = group.Count()
            })
            .ToListAsync();
    }

    private static List<RankedLeaderboardPlayer> RankPlayers(
        IEnumerable<LeaderboardPlayer> players,
        IReadOnlyCollection<PlayerResultCount> counts,
        int maximumEntries)
    {
        return players
            .Select(player => CreateRankedPlayer(player, counts))
            .OrderByDescending(item => item.Wins)
            .ThenByDescending(item => item.WinRate)
            .ThenBy(item => item.Username, StringComparer.OrdinalIgnoreCase)
            .Take(maximumEntries)
            .ToList();
    }

    private static RankedLeaderboardPlayer CreateRankedPlayer(
        LeaderboardPlayer player,
        IEnumerable<PlayerResultCount> counts)
    {
        List<PlayerResultCount> playerCounts = counts
            .Where(item => item.AccountId == player.AccountId)
            .ToList();
        int matchesPlayed = playerCounts.Sum(item => item.Count);
        int wins = playerCounts
            .Where(item => item.ResultCode == WinResultCode)
            .Sum(item => item.Count);

        return new RankedLeaderboardPlayer
        {
            AccountId = player.AccountId,
            Username = player.Username,
            ProfileImageUri = player.ProfileImageUri,
            MatchesPlayed = matchesPlayed,
            Wins = wins,
            WinRate = matchesPlayed == 0 ? 0d : (double)wins / matchesPlayed
        };
    }

    private static List<LeaderboardEntry> CreateEntries(
        IReadOnlyList<RankedLeaderboardPlayer> rankedPlayers)
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        for (int index = 0; index < rankedPlayers.Count; index++)
        {
            RankedLeaderboardPlayer player = rankedPlayers[index];
            entries.Add(new LeaderboardEntry(
                index + 1,
                player.AccountId,
                player.Username,
                player.ProfileImageUri,
                player.MatchesPlayed,
                player.Wins,
                player.WinRate));
        }

        return entries;
    }

    private sealed class LeaderboardPlayer
    {
        public long AccountId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? ProfileImageUri { get; set; }
    }

    private sealed class PlayerResultCount
    {
        public long AccountId { get; set; }

        public string ResultCode { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    private sealed class RankedLeaderboardPlayer
    {
        public long AccountId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? ProfileImageUri { get; set; }

        public int MatchesPlayed { get; set; }

        public int Wins { get; set; }

        public double WinRate { get; set; }
    }
}
