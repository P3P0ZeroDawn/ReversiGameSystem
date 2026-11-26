using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class AccountRepositoryStub : IAccountRepository
{
    internal AccountResult<LoginAccount> FindByLoginResult { get; set; } =
        new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);

    internal AccountResult<LoginAccount> FindByEmailResult { get; set; } =
        new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);

    internal AccountResult<LoginAccount> FindByIdResult { get; set; } =
        new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);

    internal AccountStatus CheckRegistrationAvailabilityResult { get; set; } =
        AccountStatus.Success;

    internal AccountResult<long> CreateAccountResult { get; set; } = new AccountResult<long>(1);

    internal AccountStatus UpdatePasswordHashResult { get; set; } = AccountStatus.Success;

    internal AccountStatus DeactivateAccountResult { get; set; } = AccountStatus.Success;

    internal Exception? Failure { get; set; }

    internal string? LastLoginIdentifier { get; private set; }

    internal string? LastEmailAddress { get; private set; }

    internal long? LastAccountId { get; private set; }

    internal string? LastCheckedUsername { get; private set; }

    internal string? LastCheckedEmailAddress { get; private set; }

    internal AccountRegistrationData? LastRegistration { get; private set; }

    internal long? LastUpdatedAccountId { get; private set; }

    internal string? LastUpdatedPasswordHash { get; private set; }

    internal long? LastDeactivatedAccountId { get; private set; }

    internal int CallCount { get; private set; }

    public AccountResult<LoginAccount> FindByLogin(string loginIdentifier)
    {
        CallCount++;
        LastLoginIdentifier = loginIdentifier;
        ThrowIfConfigured();
        return FindByLoginResult;
    }

    public AccountResult<LoginAccount> FindByEmail(string emailAddress)
    {
        CallCount++;
        LastEmailAddress = emailAddress;
        ThrowIfConfigured();
        return FindByEmailResult;
    }

    public AccountResult<LoginAccount> FindById(long accountId)
    {
        CallCount++;
        LastAccountId = accountId;
        ThrowIfConfigured();
        return FindByIdResult;
    }

    public AccountStatus CheckRegistrationAvailability(string username, string emailAddress)
    {
        CallCount++;
        LastCheckedUsername = username;
        LastCheckedEmailAddress = emailAddress;
        ThrowIfConfigured();
        return CheckRegistrationAvailabilityResult;
    }

    public AccountResult<long> CreateAccount(AccountRegistrationData registration)
    {
        CallCount++;
        LastRegistration = registration;
        ThrowIfConfigured();
        return CreateAccountResult;
    }

    public AccountStatus UpdatePasswordHash(long accountId, string newPasswordHash)
    {
        CallCount++;
        LastUpdatedAccountId = accountId;
        LastUpdatedPasswordHash = newPasswordHash;
        ThrowIfConfigured();
        return UpdatePasswordHashResult;
    }

    public AccountStatus DeactivateAccount(long accountId)
    {
        CallCount++;
        LastDeactivatedAccountId = accountId;
        ThrowIfConfigured();
        return DeactivateAccountResult;
    }

    private void ThrowIfConfigured()
    {
        if (Failure != null)
        {
            throw Failure;
        }
    }
}
