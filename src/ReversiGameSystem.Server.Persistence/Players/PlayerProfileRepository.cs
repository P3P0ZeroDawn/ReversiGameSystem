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
    public ProfileResult<PlayerProfile> GetProfile(long accountId)
    {
        if (accountId <= 0)
        {
            return new ProfileResult<PlayerProfile>(ProfileStatus.AccountNotFound);
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            return LoadProfile(context, accountId);
        }
    }

    /// <inheritdoc />
    public ProfileResult<string> GetProfileImageReference(long accountId)
    {
        if (accountId <= 0)
        {
            return new ProfileResult<string>(ProfileStatus.AccountNotFound);
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            ImageReferenceRow? lookup = context.Account
                .AsNoTracking()
                .Where(account => account.accountId == accountId
                    && account.statusCode == ActiveAccountStatusCode)
                .Select(account => new ImageReferenceRow
                {
                    ProfileImageUri = account.profileImageUri
                })
                .FirstOrDefault();
            if (lookup == null)
            {
                return new ProfileResult<string>(ProfileStatus.AccountNotFound);
            }

            return new ProfileResult<string>(lookup.ProfileImageUri ?? string.Empty);
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
    public ProfileStatus ChangeUsername(
        long accountId,
        string username)
    {
        ProfileStatus updateStatus;
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
                    updateStatus = ProfileStatus.InactiveAccount;
                }
                else
                {
                    bool usernameUnavailable = context.Account.Any(other =>
                        other.accountId != accountId
                        && other.statusCode == ActiveAccountStatusCode
                        && other.username == username);
                    if (usernameUnavailable)
                    {
                        updateStatus = ProfileStatus.UsernameUnavailable;
                    }
                    else
                    {
                        account.username = username;
                        context.SaveChanges();
                        transaction.Commit();
                        updateStatus = ProfileStatus.Success;
                    }
                }
            }
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            updateStatus = ProfileStatus.UsernameUnavailable;
        }

        return updateStatus;
    }

    /// <inheritdoc />
    public ProfileStatus SetProfileImage(
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
                return ProfileStatus.InactiveAccount;
            }

            account.profileImageUri = imageReference;
            context.SaveChanges();
            transaction.Commit();
            return ProfileStatus.Success;
        }
    }

    private static ProfileResult<PlayerProfile> LoadProfile(
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
            return new ProfileResult<PlayerProfile>(ProfileStatus.AccountNotFound);
        }

        List<ResultCount> resultCounts = LoadResultCounts(context, accountId);
        PlayerProfile profile = new PlayerProfile(
            identity.AccountId,
            identity.Username,
            identity.ProfileImageUri,
            GetResultCount(resultCounts, WinResultCode),
            GetResultCount(resultCounts, LossResultCode),
            GetResultCount(resultCounts, DrawResultCode));
        return new ProfileResult<PlayerProfile>(profile);
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

    private sealed class ImageReferenceRow
    {
        public string? ProfileImageUri { get; set; }
    }
}
