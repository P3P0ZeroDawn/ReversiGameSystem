using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Contracts.Accounts;

namespace ReversiGameSystem.Contracts.Services;

/// <summary>
/// Defines the account service contract.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IAccountService
{
    /// <summary>
    /// Performs the create account async operation.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result of the operation.</returns>
    [OperationContract]
    public Task<CreateAccountResponse> CreateAccountAsync(CreateAccountRequest request);

    /// <summary>
    /// Performs the login async operation.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result of the operation.</returns>
    [OperationContract]
    public Task<LoginResponse> LoginAsync(LoginRequest request);
}
