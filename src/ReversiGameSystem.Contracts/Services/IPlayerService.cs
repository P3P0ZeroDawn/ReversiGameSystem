using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Contracts.Leaderboards;
using ReversiGameSystem.Contracts.Players;

namespace ReversiGameSystem.Contracts.Services;

/// <summary>
/// Defines the player service contract.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IPlayerService
{
    /// <summary>
    /// Performs the get player profile async operation.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result of the operation.</returns>
    [OperationContract]
    Task<PlayerProfileResponse> GetPlayerProfileAsync(PlayerProfileRequest request);

    /// <summary>
    /// Performs the get leaderboard async operation.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result of the operation.</returns>
    [OperationContract]
    Task<LeaderboardResponse> GetLeaderboardAsync(LeaderboardRequest request);
}
