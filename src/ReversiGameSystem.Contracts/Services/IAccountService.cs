using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Contracts.Accounts;

namespace ReversiGameSystem.Contracts.Services;

[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IAccountService
{
    [OperationContract]
    Task<CreateAccountResponse> CreateAccountAsync(CreateAccountRequest request);

    [OperationContract]
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
