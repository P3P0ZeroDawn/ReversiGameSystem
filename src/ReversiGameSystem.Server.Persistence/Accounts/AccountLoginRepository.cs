using System.Data.Entity;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Accounts;

/// <summary>Loads the account data required to verify one login.</summary>
public sealed class AccountLoginRepository : IAccountLoginRepository
{
    /// <inheritdoc />
    public async Task<AccountLoginLookupResult> FindByLoginAsync(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return AccountLoginLookupResult.CreateNotFound();
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            Account? account = await context.Account
                .Include(candidate => candidate.AccountSettings)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    candidate => candidate.username == login
                        || candidate.emailAddress == login);
            if (account == null)
            {
                return AccountLoginLookupResult.CreateNotFound();
            }

            LoginAccount loginAccount = new LoginAccount(
                account.accountId,
                account.username,
                account.emailAddress,
                account.passwordHash,
                account.statusCode,
                account.AccountSettings?.preferredLocaleCode ?? string.Empty);
            return AccountLoginLookupResult.CreateFound(loginAccount);
        }
    }
}
