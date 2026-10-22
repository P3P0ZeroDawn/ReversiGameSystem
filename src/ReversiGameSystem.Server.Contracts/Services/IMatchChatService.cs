using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines stored match chat and account mute notifications for registered participants.
/// </summary>
[ServiceContract(
    Namespace = ContractNamespaces.Services,
    CallbackContract = typeof(IMatchChatServiceCallback))]
public interface IMatchChatService
{
    /// <summary>
    /// Registers the current callback channel for an authorized registered chat participant.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void OpenChatChannel(string sessionToken, long matchId);

    /// <summary>
    /// Validates moderation and stores a message from an authorized unmuted participant.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <param name="content">
    /// The message text to trim, validate and moderate.
    /// </param>
    /// <returns>
    /// The stored message identified by its match and message number.
    /// </returns>
    /// <exception cref="FaultException{ChatFault}">
    /// A declared chat rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(ChatFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MatchChatEntry SendChatMessage(string sessionToken, long matchId, string content);

    /// <summary>
    /// Retrieves stored messages for an authorized registered match participant.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="matchId">
    /// The stable positive match identifier.
    /// </param>
    /// <returns>
    /// The stored messages in message number order, or an empty array when none exist.
    /// </returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{MatchFault}">
    /// A declared match rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(MatchFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public MatchChatEntry[] GetChatHistory(string sessionToken, long matchId);
}
