using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Leaderboards;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines the global leaderboard lookup available without an application session token.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface ILeaderboardService
{
    /// <summary>
    /// Retrieves ranked active accounts using the requested entry limit.
    /// </summary>
    /// <param name="maximumEntries">
    /// The maximum entry count, from 1 through 100 inclusive.
    /// </param>
    /// <returns>
    /// The ordered leaderboard entries, or an empty array when no accounts qualify.
    /// </returns>
    /// <exception cref="FaultException{LeaderboardFault}">
    /// A declared leaderboard rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(LeaderboardFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public LeaderboardEntry[] GetLeaderboard(int maximumEntries);
}
