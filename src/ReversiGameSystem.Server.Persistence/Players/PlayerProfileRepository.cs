using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Players;

/// <summary>Loads active public profiles and their finished-match totals.</summary>
public sealed class PlayerProfileRepository : IPlayerProfileRepository
{
    private const string ActiveAccountStatusCode = "ACTIVE";
    private const string FinishedMatchStatusCode = "FINISHED";
    private const string WinResultCode = "WIN";
    private const string LossResultCode = "LOSS";
    private const string DrawResultCode = "DRAW";

    /// <inheritdoc />
    public async Task<PlayerProfileLookupResult> GetAsync(long accountId)
    {
        if (accountId <= 0)
        {
            return PlayerProfileLookupResult.CreateNotFound();
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            return await LoadProfileAsync(context, accountId);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveAsync(long accountId)
    {
        if (accountId <= 0)
        {
            return false;
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            return await context.Account.AnyAsync(account => account.accountId == accountId
                && account.statusCode == ActiveAccountStatusCode);
        }
    }

    /// <inheritdoc />
    public async Task<ProfileRepositoryUpdateStatus> ChangeUsernameAsync(
        long accountId,
        string username)
    {
        ProfileRepositoryUpdateStatus updateStatus;
        try
        {
            using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
            using (DbContextTransaction transaction = context.Database.BeginTransaction(
                IsolationLevel.Serializable))
            {
                Account? account = await context.Account.SingleOrDefaultAsync(item =>
                    item.accountId == accountId
                    && item.statusCode == ActiveAccountStatusCode);
                if (account == null)
                {
                    updateStatus = ProfileRepositoryUpdateStatus.InactiveAccount;
                }
                else
                {
                    bool usernameUnavailable = await context.Account.AnyAsync(other =>
                        other.accountId != accountId
                        && other.statusCode == ActiveAccountStatusCode
                        && other.username == username);
                    if (usernameUnavailable)
                    {
                        updateStatus = ProfileRepositoryUpdateStatus.UsernameUnavailable;
                    }
                    else
                    {
                        account.username = username;
                        await context.SaveChangesAsync();
                        transaction.Commit();
                        updateStatus = ProfileRepositoryUpdateStatus.Success;
                    }

                }
            }
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            updateStatus = ProfileRepositoryUpdateStatus.UsernameUnavailable;
        }

        return updateStatus;
    }

    /// <inheritdoc />
    public async Task<ProfileRepositoryUpdateStatus> SetProfileImageAsync(
        long accountId,
        string imageReference)
    {
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        using (DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable))
        {
            Account? account = await context.Account.SingleOrDefaultAsync(item =>
                item.accountId == accountId
                && item.statusCode == ActiveAccountStatusCode);
            if (account == null)
            {
                return ProfileRepositoryUpdateStatus.InactiveAccount;
            }

            account.profileImageUri = imageReference;
            await context.SaveChangesAsync();
            transaction.Commit();
            return ProfileRepositoryUpdateStatus.Success;
        }
    }

    private static async Task<PlayerProfileLookupResult> LoadProfileAsync(
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
            return PlayerProfileLookupResult.CreateNotFound();
        }

        List<ResultCount> resultCounts = await LoadResultCountsAsync(context, accountId);
        PlayerProfile profile = new PlayerProfile(
            identity.AccountId,
            identity.Username,
            identity.ProfileImageUri,
            GetResultCount(resultCounts, WinResultCode),
            GetResultCount(resultCounts, LossResultCode),
            GetResultCount(resultCounts, DrawResultCode));
        return PlayerProfileLookupResult.CreateFound(profile);
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

    private static int GetResultCount(
        IEnumerable<ResultCount> counts,
        string resultCode)
    {
        ResultCount? result = counts.FirstOrDefault(
            item => string.Equals(
                item.ResultCode,
                resultCode,
                StringComparison.Ordinal));
        if (result == null)
        {
            return 0;
        }

        return result.Count;
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        Exception? current = exception;
        while (current != null)
        {
            if (current is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
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
