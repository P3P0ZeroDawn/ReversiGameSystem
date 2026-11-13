using System;
using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Exposes account access use cases through WCF.
/// </summary>
[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple,
    UseSynchronizationContext = false)]
public sealed class AccountAccessService : IAccountAccessService
{
    private readonly ICreateAccountHandler _createAccountHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountAccessService"/> class.
    /// </summary>
    /// <param name="createAccountHandler">The account registration use case.</param>
    public AccountAccessService(ICreateAccountHandler createAccountHandler)
    {
        _createAccountHandler = createAccountHandler
            ?? throw new ArgumentNullException(nameof(createAccountHandler));
    }

    /// <inheritdoc />
    public async Task<AccountRegistrationOutcome> RegisterAccountAsync(
        AccountRegistrationCredentials credentials)
    {
        CreateAccountCommand command = AccountContractMapper.Map(credentials);
        CreateAccountResult result = await _createAccountHandler.ExecuteAsync(command);
        return AccountContractMapper.Map(result, credentials);
    }
}
