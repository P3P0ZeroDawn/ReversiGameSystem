using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>Loads the account data required to verify one login.</summary>
public interface IAccountLoginRepository
{
    /// <summary>Finds an account by username or email address.</summary>
    /// <param name="login">The normalized username or email address.</param>
    /// <returns>The account lookup result.</returns>
    public Task<AccountLoginLookupResult> FindByLoginAsync(string login);
}
