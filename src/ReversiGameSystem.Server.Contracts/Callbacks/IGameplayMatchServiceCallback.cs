using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.Contracts.Callbacks;

/// <summary>
/// Receives complete dynamic match state and participant connection changes.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IGameplayMatchServiceCallback
{
    /// <summary>
    /// Delivers a complete confirmed dynamic state after a match transition.
    /// </summary>
    /// <param name="state">
    /// The complete confirmed dynamic match state for this recipient.
    /// </param>
    [OperationContract(IsOneWay = true)]
    public void OnMatchStateChanged(MatchStateSnapshot state);

    /// <summary>
    /// Notifies an authorized recipient of a human participant connection state.
    /// </summary>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <param name="seatNumber">
    /// The human participant seat, one for black or two for white.
    /// </param>
    /// <param name="isConnected">
    /// Whether the server currently considers the human participant connected.
    /// </param>
    [OperationContract(IsOneWay = true)]
    public void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected);
}
