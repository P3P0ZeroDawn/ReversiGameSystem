using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Players;

/// <summary>Commits public profile changes using the existing account model.</summary>
public sealed class ProfileRepository : IPlayerProfileEditorStore
{
    private const string ActiveStatus = "ACTIVE";

    /// <inheritdoc />
    public bool IsActive(long accountId)
    {
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            return context.Account.Any(account => account.accountId == accountId
                && account.statusCode == ActiveStatus);
        }
    }

    /// <inheritdoc />
    public void ChangeUsername(long accountId, string username)
    {
        try
        {
            using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
            using (DbContextTransaction transaction = context.Database.BeginTransaction(
                IsolationLevel.Serializable))
            {
                Account account = LoadActive(context, accountId);
                if (context.Account.Any(other => other.accountId != accountId
                    && other.statusCode == ActiveStatus && other.username == username))
                {
                    throw new ProfileRejectedException(ProfileRejection.UsernameUnavailable);
                }

                account.username = username;
                context.SaveChanges();
                transaction.Commit();
            }
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new ProfileRejectedException(ProfileRejection.UsernameUnavailable);
        }
    }

    /// <inheritdoc />
    public void SetProfileImage(long accountId, string imageReference)
    {
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        using (DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable))
        {
            Account account = LoadActive(context, accountId);
            account.profileImageUri = imageReference;
            context.SaveChanges();
            transaction.Commit();
        }
    }

    private static Account LoadActive(ReversiGameSystemEntities context, long accountId)
    {
        Account? account = context.Account.SingleOrDefault(item => item.accountId == accountId
            && item.statusCode == ActiveStatus);
        return account ?? throw new ProfileRejectedException(ProfileRejection.InactiveAccount);
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        Exception? current = exception;
        while (current != null)
        {
            if (current is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
