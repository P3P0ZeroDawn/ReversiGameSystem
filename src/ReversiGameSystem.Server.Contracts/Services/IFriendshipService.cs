using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines player lookup, friend requests, friendships and authorized presence notifications.
/// </summary>
[ServiceContract(
    Namespace = ContractNamespaces.Services,
    CallbackContract = typeof(IFriendshipServiceCallback))]
public interface IFriendshipService
{
    /// <summary>
    /// Registers the current callback channel for the authenticated account.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <exception cref="FaultException{FriendshipFault}">
    /// A declared friendship rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(FriendshipFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void OpenFriendshipChannel(string sessionToken);

    /// <summary>
    /// Finds registered player accounts matching the supplied username or email text.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="searchText">
    /// The username or email search text; empty text requests the permitted player listing.
    /// </param>
    /// <returns>
    /// The public summaries of matching available player accounts.
    /// </returns>
    /// <exception cref="FaultException{FriendshipFault}">
    /// A declared friendship rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(FriendshipFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public PlayerSummary[] FindPlayers(string sessionToken, string searchText);

    /// <summary>
    /// Creates a pending friend request from the authenticated account to another account.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="recipientAccountId">
    /// The positive identifier of the account receiving the friend request.
    /// </param>
    /// <returns>
    /// The confirmed pending friend request with its stable identifier.
    /// </returns>
    /// <exception cref="FaultException{FriendshipFault}">
    /// A declared friendship rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(FriendshipFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId);

    /// <summary>
    /// Retrieves pending friend requests addressed to the authenticated account.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <returns>
    /// The received pending requests, or an empty array when there are none.
    /// </returns>
    /// <exception cref="FaultException{FriendshipFault}">
    /// A declared friendship rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(FriendshipFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public FriendRequest[] GetPendingFriendRequests(string sessionToken);

    /// <summary>
    /// Accepts or rejects a pending request addressed to the authenticated account.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="requestId">
    /// The stable identifier of the pending friend request.
    /// </param>
    /// <param name="accept">
    /// True to accept the request, or false to reject it.
    /// </param>
    /// <exception cref="FaultException{FriendshipFault}">
    /// A declared friendship rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(FriendshipFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void RespondToFriendRequest(string sessionToken, string requestId, bool accept);

    /// <summary>
    /// Retrieves the authenticated account's current friends and their known presence.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <returns>
    /// The current friends, or an empty array when there are none.
    /// </returns>
    /// <exception cref="FaultException{FriendshipFault}">
    /// A declared friendship rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(FriendshipFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public FriendSummary[] GetFriends(string sessionToken);

    /// <summary>
    /// Deactivates an existing friendship while preserving its recorded history.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="friendAccountId">
    /// The positive identifier of the friend whose relationship is removed.
    /// </param>
    /// <exception cref="FaultException{FriendshipFault}">
    /// A declared friendship rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(FriendshipFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void RemoveFriend(string sessionToken, long friendAccountId);
}
