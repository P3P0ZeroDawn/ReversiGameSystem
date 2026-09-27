using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Accounts;

public sealed class EntityFrameworkAccountRegistrationStore : IAccountRegistrationStore
{
    private const string ActiveStatusCode = "ACTIVE";
    private const string StandardPieceStyleCode = "STANDARD";

    public async Task<AccountRegistrationStoreResult> RegisterAsync(
        AccountRegistrationData registration)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        try
        {
            using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
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
        }
        catch (DbUpdateException exception)
        {
            return MapUpdateException(exception);
        }
        catch (Exception exception) when (
            exception is EntityException
            || exception is SqlException
            || exception is InvalidOperationException)
        {
            return new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.Unavailable);
        }
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
        SqlException? sqlException = FindSqlException(exception);
        if (sqlException == null || (sqlException.Number != 2601 && sqlException.Number != 2627))
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

    private static SqlException? FindSqlException(Exception exception)
    {
        Exception? current = exception;
        while (current != null)
        {
            if (current is SqlException sqlException)
            {
                return sqlException;
            }

            current = current.InnerException;
        }

        return null;
    }
}
