using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
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
    public PlayerProfileLookupResult GetProfile(long accountId)
    {
        if (accountId <= 0)
        {
            return PlayerProfileLookupResult.CreateNotFound();
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            return LoadProfile(context, accountId);
        }
    }

    /// <inheritdoc />
    public bool IsActive(long accountId)
    {
        if (accountId <= 0)
        {
            return false;
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            return context.Account.Any(account => account.accountId == accountId
                && account.statusCode == ActiveAccountStatusCode);
        }
    }

    /// <inheritdoc />
    public ProfileRepositoryUpdateStatus ChangeUsername(
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
                Account? account = context.Account.SingleOrDefault(item =>
                    item.accountId == accountId
                    && item.statusCode == ActiveAccountStatusCode);
                if (account == null)
                {
                    updateStatus = ProfileRepositoryUpdateStatus.InactiveAccount;
                }
                else
                {
                    bool usernameUnavailable = context.Account.Any(other =>
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
                        context.SaveChanges();
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
    public ProfileRepositoryUpdateStatus SetProfileImage(
        long accountId,
        string imageReference)
    {
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        using (DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable))
        {
            Account? account = context.Account.SingleOrDefault(item =>
                item.accountId == accountId
                && item.statusCode == ActiveAccountStatusCode);
            if (account == null)
            {
                return ProfileRepositoryUpdateStatus.InactiveAccount;
            }

            account.profileImageUri = imageReference;
            context.SaveChanges();
            transaction.Commit();
            return ProfileRepositoryUpdateStatus.Success;
        }
    }

    private static PlayerProfileLookupResult LoadProfile(
        ReversiGameSystemEntities context,
        long accountId)
    {
        PlayerIdentity? identity = context.Account
            .AsNoTracking()
            .Where(account => account.accountId == accountId
                && account.statusCode == ActiveAccountStatusCode)
            .Select(account => new PlayerIdentity
            {
                AccountId = account.accountId,
                Username = account.username,
                ProfileImageUri = account.profileImageUri
            })
            .FirstOrDefault();
        if (identity == null)
        {
            return PlayerProfileLookupResult.CreateNotFound();
        }

        List<ResultCount> resultCounts = LoadResultCounts(context, accountId);
        PlayerProfile profile = new PlayerProfile(
            identity.AccountId,
            identity.Username,
            identity.ProfileImageUri,
            GetResultCount(resultCounts, WinResultCode),
            GetResultCount(resultCounts, LossResultCode),
            GetResultCount(resultCounts, DrawResultCode));
        return PlayerProfileLookupResult.CreateFound(profile);
    }

    private static List<ResultCount> LoadResultCounts(
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
            .ToList();
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
