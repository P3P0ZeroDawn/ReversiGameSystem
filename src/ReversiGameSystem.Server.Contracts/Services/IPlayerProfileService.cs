using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Players;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines remote player profile operations.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IPlayerProfileService
{
    /// <summary>
    /// Retrieves a registered player's public profile and statistics.
    /// </summary>
    /// <param name="criteria">The authenticated lookup criteria.</param>
    /// <returns>The expected profile lookup outcome.</returns>
    [OperationContract]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public Task<PlayerProfileLookupOutcome> GetPlayerProfileAsync(PlayerProfileCriteria criteria);
}
