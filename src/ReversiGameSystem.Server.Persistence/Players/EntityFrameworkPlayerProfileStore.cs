using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Players;

/// <summary>
/// Represents entity framework player profile store.
/// </summary>
public sealed class EntityFrameworkPlayerProfileStore : IPlayerProfileStore
{
    private const string ActiveAccountStatusCode = "ACTIVE";
    private const string FinishedMatchStatusCode = "FINISHED";
    private const string WinResultCode = "WIN";
    private const string LossResultCode = "LOSS";
    private const string DrawResultCode = "DRAW";

    /// <summary>
    /// Performs the get async operation.
    /// </summary>
    /// <param name="accountId">The account id.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<PlayerProfileStoreResult> GetAsync(long accountId)
    {
        if (accountId <= 0)
        {
            return new PlayerProfileStoreResult(PlayerProfileStoreOutcome.NotFound);
        }

        try
        {
            using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
            {
                return await LoadProfileAsync(context, accountId);
            }
        }
        catch (EntityException)
        {
            return new PlayerProfileStoreResult(PlayerProfileStoreOutcome.Unavailable);
        }
        catch (SqlException)
        {
            return new PlayerProfileStoreResult(PlayerProfileStoreOutcome.Unavailable);
        }
        catch (InvalidOperationException)
        {
            return new PlayerProfileStoreResult(PlayerProfileStoreOutcome.Unavailable);
        }
        catch (NotSupportedException)
        {
            return new PlayerProfileStoreResult(PlayerProfileStoreOutcome.Unavailable);
        }
    }

    private static async Task<PlayerProfileStoreResult> LoadProfileAsync(
        ReversiGameSystemEntities context,
        long accountId)
    {
        PlayerIdentity? identity = await context.Account
            .AsNoTracking()
            .Where(account => account.accountId == accountId
                && account.statusCode == ActiveAccountStatusCode)
            .Select(account => new PlayerIdentity
            {
                AccountId = account.accountId,
                Username = account.username,
                ProfileImageUri = account.profileImageUri
            })
            .FirstOrDefaultAsync();
        if (identity == null)
        {
            return new PlayerProfileStoreResult(PlayerProfileStoreOutcome.NotFound);
        }

        List<ResultCount> resultCounts = await LoadResultCountsAsync(context, accountId);
        PlayerProfileData profile = new PlayerProfileData(
            identity.AccountId,
            identity.Username,
            identity.ProfileImageUri,
            Count(resultCounts, WinResultCode),
            Count(resultCounts, LossResultCode),
            Count(resultCounts, DrawResultCode));
        return new PlayerProfileStoreResult(PlayerProfileStoreOutcome.Found, profile);
    }

    private static Task<List<ResultCount>> LoadResultCountsAsync(
        ReversiGameSystemEntities context,
        long accountId)
    {
        return context.MatchParticipant
            .AsNoTracking()
            .Where(participant => participant.accountId == accountId
                && participant.GameMatch1.statusCode == FinishedMatchStatusCode
                && participant.GameMatch1.countsTowardStatistics
                && participant.resultCode != null)
            .GroupBy(participant => participant.resultCode)
            .Select(group => new ResultCount
            {
                ResultCode = group.Key,
                Count = group.Count()
            })
            .ToListAsync();
    }

    private static int Count(IEnumerable<ResultCount> counts, string resultCode)
    {
        ResultCount? result = counts.FirstOrDefault(
            item => string.Equals(
                item.ResultCode,
                resultCode,
                StringComparison.Ordinal));
        return result?.Count ?? 0;
    }

    private sealed class PlayerIdentity
    {
        public long AccountId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? ProfileImageUri { get; set; }
    }

    private sealed class ResultCount
    {
        public string ResultCode { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
