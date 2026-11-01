using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Leaderboards;

/// <summary>
/// Retrieves the persisted player statistics required by the global leaderboard.
/// </summary>
public sealed class LeaderboardRepository : ILeaderboardRepository
{
    private const string ActiveAccountStatusCode = "ACTIVE";
    private const string FinishedMatchStatusCode = "FINISHED";
    private const string WinResultCode = "WIN";

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaderboardPlayer>> GetPlayersAsync()
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        List<LeaderboardAccount> accounts = await LoadAccountsAsync(context);
        List<PlayerResultCount> resultCounts = await LoadResultCountsAsync(context);
        return CreatePlayers(accounts, resultCounts);
    }

    private static Task<List<LeaderboardAccount>> LoadAccountsAsync(
        ReversiGameSystemEntities context)
    {
        return context.Account
            .AsNoTracking()
            .Where(account => account.statusCode == ActiveAccountStatusCode)
            .Select(account => new LeaderboardAccount
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

    private static IReadOnlyList<LeaderboardPlayer> CreatePlayers(
        IEnumerable<LeaderboardAccount> accounts,
        IReadOnlyCollection<PlayerResultCount> resultCounts)
    {
        return accounts
            .Select(account => CreatePlayer(account, resultCounts))
            .ToList();
    }

    private static LeaderboardPlayer CreatePlayer(
        LeaderboardAccount account,
        IEnumerable<PlayerResultCount> resultCounts)
    {
        List<PlayerResultCount> accountCounts = resultCounts
            .Where(item => item.AccountId == account.AccountId)
            .ToList();
        int matchesPlayed = accountCounts.Sum(item => item.Count);
        int wins = accountCounts
            .Where(item => item.ResultCode == WinResultCode)
            .Sum(item => item.Count);

        return new LeaderboardPlayer(
            account.AccountId,
            account.Username,
            account.ProfileImageUri,
            matchesPlayed,
            wins);
    }

    private sealed class LeaderboardAccount
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
