namespace ReversiGameSystem.Server.Application.Accounts;

public interface IAccountRepository
{
    AccountResult<LoginAccount> FindByLogin(string loginIdentifier);

    AccountResult<LoginAccount> FindByEmail(string emailAddress);

    AccountResult<LoginAccount> FindById(long accountId);

    AccountStatus CheckRegistrationAvailability(string username, string emailAddress);

    AccountResult<long> CreateAccount(AccountRegistrationData registration);

    AccountStatus UpdatePasswordHash(long accountId, string newPasswordHash);

    AccountStatus DeactivateAccount(long accountId);
}
