using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines the registration operation retained during the account migration.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IAccountAccessService
{
    /// <summary>
    /// Registers a player account.
    /// </summary>
    /// <param name="credentials">The account information to validate and register.</param>
    /// <returns>The expected registration outcome.</returns>
    [OperationContract]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public Task<AccountRegistrationOutcome> RegisterAccountAsync(
        AccountRegistrationCredentials credentials);

}
