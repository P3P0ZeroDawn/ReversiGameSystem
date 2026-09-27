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

public sealed class EntityFrameworkLeaderboardStore : ILeaderboardStore
{
    private const string ActiveAccountStatusCode = "ACTIVE";
    private const string FinishedMatchStatusCode = "FINISHED";
    private const string WinResultCode = "WIN";

    public async Task<LeaderboardStoreResult> GetAsync(int maximumEntries)
    {
        try
        {
            using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
            {
                List<LeaderboardPlayer> players = await context.Account
                    .AsNoTracking()
                    .Where(account => account.statusCode == ActiveAccountStatusCode)
                    .Select(account => new LeaderboardPlayer
                    {
                        AccountId = account.accountId,
                        Username = account.username,
                        ProfileImageUri = account.profileImageUri
                    })
                    .ToListAsync();

                List<PlayerResultCount> counts = await context.MatchParticipant
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

                var rankedPlayers = players
                    .Select(player =>
                    {
                        int matchesPlayed = counts
                            .Where(item => item.AccountId == player.AccountId)
                            .Sum(item => item.Count);
                        int wins = counts
                            .Where(item => item.AccountId == player.AccountId
                                && item.ResultCode == WinResultCode)
                            .Sum(item => item.Count);
                        return new
                        {
                            Player = player,
                            MatchesPlayed = matchesPlayed,
                            Wins = wins,
                            WinRate = matchesPlayed == 0 ? 0d : (double)wins / matchesPlayed
                        };
                    })
                    .OrderByDescending(item => item.Wins)
                    .ThenByDescending(item => item.WinRate)
                    .ThenBy(item => item.Player.Username, StringComparer.OrdinalIgnoreCase)
                    .Take(maximumEntries)
                    .ToList();

                List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
                for (int index = 0; index < rankedPlayers.Count; index++)
                {
                    var item = rankedPlayers[index];
                    entries.Add(new LeaderboardEntry(
                        index + 1,
                        item.Player.AccountId,
                        item.Player.Username,
                        item.Player.ProfileImageUri,
                        item.MatchesPlayed,
                        item.Wins,
                        item.WinRate));
                }

                return new LeaderboardStoreResult(LeaderboardStoreOutcome.Success, entries);
            }
        }
        catch (Exception exception) when (
            exception is EntityException
            || exception is SqlException
            || exception is InvalidOperationException
            || exception is NotSupportedException)
        {
            return new LeaderboardStoreResult(LeaderboardStoreOutcome.Unavailable);
        }
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
}
