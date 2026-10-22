using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines authoritative match actions, observation and authorized match history.
/// </summary>
[ServiceContract(
    Namespace = ContractNamespaces.Services,
    CallbackContract = typeof(IGameplayMatchServiceCallback))]
public interface IGameplayMatchService
{
    /// <summary>
    /// Registers the participant callback channel without changing the match or returning its
    /// state.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void OpenMatchChannel(string sessionToken, long matchId);

    /// <summary>
    /// Retrieves base match information personalized for the authorized participant or observer.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <returns>
    /// The participants, assigned recipient seat and fixed turn duration of the match.
    /// </returns>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MatchInformation GetMatchInformation(string sessionToken, long matchId);

    /// <summary>
    /// Retrieves a complete confirmed dynamic match state without registering a channel.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <returns>
    /// The complete dynamic state of one confirmed match state version.
    /// </returns>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MatchStateSnapshot GetMatchState(string sessionToken, long matchId);

    /// <summary>
    /// Validates and commits a placement against the expected match state version.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="placement">
    /// The match, coordinates and expected match state version for the placement.
    /// </param>
    /// <returns>
    /// The confirmed complete dynamic state after the placement and automatic transitions.
    /// </returns>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement);

    /// <summary>
    /// Concludes the participant match by abandonment or returns its existing conclusion.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <returns>
    /// The confirmed conclusion with its terminal match state version.
    /// </returns>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MatchConclusion ForfeitMatch(string sessionToken, long matchId);

    /// <summary>
    /// Registers authorized read-only observation and retrieves the current dynamic state.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <returns>
    /// The complete dynamic state for the observer, without granting a participant seat.
    /// </returns>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MatchStateSnapshot ObserveMatch(string sessionToken, long matchId);

    /// <summary>
    /// Retrieves matches belonging to the authenticated account history.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <returns>
    /// The authorized account matches in descending registration time order.
    /// </returns>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MatchSummary[] GetMatchHistory(string sessionToken);

    /// <summary>
    /// Retrieves coherent match details and confirmed board frames for authorized replay.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <returns>
    /// The authorized match details, moves and complete board frames.
    /// </returns>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MatchDetails GetMatchDetails(string sessionToken, long matchId);

    /// <summary>
    /// Retrieves confirmed placements for an authorized match.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <returns>
    /// The placements in ascending move number order, or an empty array before any placement.
    /// </returns>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MoveRecord[] GetMoveHistory(string sessionToken, long matchId);
}
