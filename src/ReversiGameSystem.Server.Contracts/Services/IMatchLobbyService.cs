using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines waiting match creation, joining, configuration, start and cancellation.
/// </summary>
[ServiceContract(
    Namespace = ContractNamespaces.Services,
    CallbackContract = typeof(IMatchLobbyServiceCallback))]
public interface IMatchLobbyService
{
    /// <summary>
    /// Registers the current callback channel for authorized lobby notifications.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
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
    public void OpenLobbyChannel(string sessionToken);

    /// <summary>
    /// Creates a waiting match and assigns the creator to its first seat.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="configuration">
    /// The proposed match configuration to validate.
    /// </param>
    /// <returns>
    /// The confirmed waiting lobby and its temporary join code.
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
    public LobbySnapshot CreateMatch(string sessionToken, MatchConfiguration configuration);

    /// <summary>
    /// Retrieves waiting human-opponent lobbies that the session may join.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <returns>
    /// The available lobbies, or an empty array when there are none.
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
    public AvailableMatch[] GetAvailableMatches(string sessionToken);

    /// <summary>
    /// Joins a waiting lobby by code and starts the match with its second participant.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchCode">
    /// The temporary code identifying a currently joinable lobby.
    /// </param>
    /// <returns>
    /// The lobby snapshot containing the started match identifier.
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
    public LobbySnapshot JoinMatch(string sessionToken, string matchCode);

    /// <summary>
    /// Changes the turn duration of a waiting lobby owned by the caller.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <param name="turnDurationSeconds">
    /// The proposed turn duration in seconds, from 10 through 120.
    /// </param>
    /// <returns>
    /// The confirmed lobby after its turn duration is saved.
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
    public LobbySnapshot ChangeTurnDuration(
        string sessionToken,
        long matchId,
        int turnDurationSeconds);

    /// <summary>
    /// Starts the caller's waiting lobby against a system opponent.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <returns>
    /// The confirmed initial dynamic state of the started match.
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
    public MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId);

    /// <summary>
    /// Cancels a waiting lobby owned by the caller, including an already confirmed cancellation.
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
    public void CancelMatch(string sessionToken, long matchId);
}
