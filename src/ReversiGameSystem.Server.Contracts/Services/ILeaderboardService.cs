using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Leaderboards;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines remote leaderboard lookup operations.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface ILeaderboardService
{
    /// <summary>
    /// Retrieves the current ranked leaderboard standings.
    /// </summary>
    /// <param name="criteria">The leaderboard lookup criteria.</param>
    /// <returns>The generated leaderboard standings.</returns>
    [OperationContract]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    Task<LeaderboardStandings> GetLeaderboardAsync(LeaderboardCriteria criteria);
}
