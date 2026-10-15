using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Accounts;

/// <summary>
/// Represents entity framework account registration store.
/// </summary>
public sealed class EntityFrameworkAccountRegistrationStore : IAccountRegistrationStore
{
    private const string ActiveStatusCode = "ACTIVE";
    private const string StandardPieceStyleCode = "STANDARD";

    /// <summary>
    /// Performs the register async operation.
    /// </summary>
    /// <param name="registration">The registration.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<AccountRegistrationStoreResult> RegisterAsync(
        AccountRegistrationData registration)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        AccountRegistrationStoreResult result;
        try
        {
            using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
            {
                result = await SaveRegistrationAsync(context, registration);
            }
        }
        catch (DbUpdateException exception)
        {
            result = MapUpdateException(exception);
        }
        catch (Exception exception) when (exception is EntityException
                                            or SqlException
                                            or InvalidOperationException)
        {
            result = new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.Unavailable);
        }

        return result;
    }

    private static async Task<AccountRegistrationStoreResult> SaveRegistrationAsync(
        ReversiGameSystemEntities context,
        AccountRegistrationData registration)
    {
        if (await context.Account.AsNoTracking().AnyAsync(
            account => account.username == registration.Username))
        {
            return new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.UsernameAlreadyExists);
        }

        if (await context.Account.AsNoTracking().AnyAsync(
            account => account.emailAddress == registration.EmailAddress))
        {
            return new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.EmailAddressAlreadyExists);
        }

        bool localeIsSupported = await context.SupportedLocale.AsNoTracking().AnyAsync(
            locale => locale.localeCode == registration.PreferredLocaleCode
                && locale.isEnabled);
        if (!localeIsSupported)
        {
            return new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.UnsupportedLocale);
        }

        Account account = CreateAccount(registration);
        context.Account.Add(account);
        await context.SaveChangesAsync();

        return new AccountRegistrationStoreResult(
            AccountRegistrationStoreOutcome.Success,
            account.accountId);
    }

    private static Account CreateAccount(AccountRegistrationData registration)
    {
        Account account = new Account
        {
            username = registration.Username,
            emailAddress = registration.EmailAddress,
            passwordHash = registration.PasswordHash,
            profileImageUri = registration.ProfileImageUri,
            statusCode = ActiveStatusCode,
            registeredAtUtc = registration.RegisteredAtUtc
        };

        AccountSettings settings = new AccountSettings
        {
            preferredLocaleCode = registration.PreferredLocaleCode,
            pieceStyleCode = StandardPieceStyleCode,
            Account = account
        };
        account.AccountSettings = settings;
        return account;
    }

    private static AccountRegistrationStoreResult MapUpdateException(
        DbUpdateException exception)
    {
        if (!TryFindSqlException(exception, out SqlException? sqlException)
            || sqlException == null
            || (sqlException.Number != 2601 && sqlException.Number != 2627))
        {
            return new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.Unavailable);
        }

        if (sqlException.Message.IndexOf(
            "UQ_Account_Username",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.UsernameAlreadyExists);
        }

        if (sqlException.Message.IndexOf(
            "UQ_Account_EmailAddress",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.EmailAddressAlreadyExists);
        }

        return new AccountRegistrationStoreResult(AccountRegistrationStoreOutcome.Unavailable);
    }

    private static bool TryFindSqlException(
        Exception exception,
        out SqlException? sqlException)
    {
        Exception? current = exception;
        while (current != null)
        {
            if (current is SqlException currentSqlException)
            {
                sqlException = currentSqlException;
                return true;
            }

            current = current.InnerException;
        }

        sqlException = null;
        return false;
    }
}
