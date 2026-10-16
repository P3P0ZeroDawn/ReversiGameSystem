using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines remote account registration and authentication operations.
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

    /// <summary>
    /// Authenticates an account and opens a server session.
    /// </summary>
    /// <param name="credentials">The credentials to authenticate.</param>
    /// <returns>The expected authentication outcome.</returns>
    [OperationContract]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public Task<AuthenticationOutcome> LoginAsync(AuthenticationCredentials credentials);
}
