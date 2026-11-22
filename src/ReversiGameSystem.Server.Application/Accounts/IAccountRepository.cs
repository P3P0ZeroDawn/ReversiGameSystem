namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Defines synchronous persistence operations for registered accounts.
/// </summary>
public interface IAccountRepository
{
    /// <summary>Finds an account by username or email address.</summary>
    /// <param name="loginIdentifier">The normalized username or email address.</param>
    /// <returns>The matching account or AccountNotFound.</returns>
    AccountResult<LoginAccount> FindByLogin(string loginIdentifier);

    /// <summary>Finds an account by email address.</summary>
    /// <param name="emailAddress">The normalized email address.</param>
    /// <returns>The matching account or AccountNotFound.</returns>
    AccountResult<LoginAccount> FindByEmail(string emailAddress);

    /// <summary>Finds an account by its persistent identifier.</summary>
    /// <param name="accountId">The persistent account identifier.</param>
    /// <returns>The matching account or AccountNotFound.</returns>
    AccountResult<LoginAccount> FindById(long accountId);

    /// <summary>Checks whether the username and email address are available.</summary>
    /// <param name="username">The proposed username.</param>
    /// <param name="emailAddress">The proposed email address.</param>
    /// <returns>Success when available, or the conflicting status.</returns>
    AccountStatus CheckRegistrationAvailability(string username, string emailAddress);

    /// <summary>Creates a new active account and its default settings.</summary>
    /// <param name="registration">The validated registration data.</param>
    /// <returns>The created account identifier or a conflict/validation status.</returns>
    AccountResult<long> CreateAccount(AccountRegistrationData registration);

    /// <summary>Updates the stored password hash of an active account.</summary>
    /// <param name="accountId">The persistent account identifier.</param>
    /// <param name="newPasswordHash">The encoded new password hash.</param>
    /// <returns>Success when updated, or InactiveAccount when unavailable.</returns>
    AccountStatus UpdatePasswordHash(long accountId, string newPasswordHash);

    /// <summary>Deactivates an active account when it has no match in progress.</summary>
    /// <param name="accountId">The persistent account identifier.</param>
    /// <returns>Success, InactiveAccount, or ActiveMatchInProgress.</returns>
    AccountStatus DeactivateAccount(long accountId);
}
