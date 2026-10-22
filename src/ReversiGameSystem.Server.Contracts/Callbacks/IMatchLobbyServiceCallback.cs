using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.Contracts.Callbacks;

/// <summary>
/// Receives confirmed match starts and lobby cancellations.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IMatchLobbyServiceCallback
{
    /// <summary>
    /// Delivers the started match dynamic state personalized for the authorized recipient.
    /// </summary>
    /// <param name="state">
    /// The complete confirmed dynamic match state for this recipient.
    /// </param>
    [OperationContract(IsOneWay = true)]
    public void OnMatchStarted(MatchStateSnapshot state);

    /// <summary>
    /// Notifies an authorized session that a waiting lobby was cancelled.
    /// </summary>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    [OperationContract(IsOneWay = true)]
    public void OnLobbyClosed(long matchId);
}
