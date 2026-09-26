using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Contracts.Leaderboards;
using ReversiGameSystem.Contracts.Players;

namespace ReversiGameSystem.Contracts.Services;

[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IPlayerService
{
    [OperationContract]
    Task<PlayerProfileResponse> GetPlayerProfileAsync(PlayerProfileRequest request);

    [OperationContract]
    Task<LeaderboardResponse> GetLeaderboardAsync(LeaderboardRequest request);
}
