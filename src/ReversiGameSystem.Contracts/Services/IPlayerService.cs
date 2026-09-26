using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Contracts.Players;

namespace ReversiGameSystem.Contracts.Services;

[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IPlayerService
{
    [OperationContract]
    Task<PlayerSummaryResponse> GetCurrentPlayerSummaryAsync(
        PlayerSummaryRequest request);
}
