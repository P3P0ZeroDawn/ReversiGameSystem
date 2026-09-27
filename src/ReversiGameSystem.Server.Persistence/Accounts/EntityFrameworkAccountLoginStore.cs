using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Accounts;

/// <summary>
/// Represents entity framework account login store.
/// </summary>
public sealed class EntityFrameworkAccountLoginStore : IAccountLoginStore
{
    /// <summary>
    /// Performs the find by login async operation.
    /// </summary>
    /// <param name="login">The login.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<AccountLoginLookupResult> FindByLoginAsync(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return new AccountLoginLookupResult(AccountLoginLookupOutcome.NotFound);
        }

        try
        {
            using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
            {
                Account? account = await context.Account.AsNoTracking().FirstOrDefaultAsync(
                    candidate => candidate.username == login
                        || candidate.emailAddress == login);
                if (account == null)
                {
                    return new AccountLoginLookupResult(AccountLoginLookupOutcome.NotFound);
                }

                return new AccountLoginLookupResult(
                    AccountLoginLookupOutcome.Found,
                    new LoginAccount(
                        account.accountId,
                        account.username,
                        account.emailAddress,
                        account.passwordHash,
                        account.statusCode));
            }
        }
        catch (EntityException)
        {
            return new AccountLoginLookupResult(AccountLoginLookupOutcome.Unavailable);
        }
        catch (SqlException)
        {
            return new AccountLoginLookupResult(AccountLoginLookupOutcome.Unavailable);
        }
        catch (InvalidOperationException)
        {
            return new AccountLoginLookupResult(AccountLoginLookupOutcome.Unavailable);
        }
    }
}
