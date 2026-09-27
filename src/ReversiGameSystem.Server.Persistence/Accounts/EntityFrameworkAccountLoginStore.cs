using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Accounts;

public sealed class EntityFrameworkAccountLoginStore : IAccountLoginStore
{
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
        catch (Exception exception) when (
            exception is EntityException
            || exception is SqlException
            || exception is InvalidOperationException)
        {
            return new AccountLoginLookupResult(AccountLoginLookupOutcome.Unavailable);
        }
    }
}
