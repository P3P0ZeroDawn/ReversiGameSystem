using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Friendships;

namespace ReversiGameSystem.Server.Contracts.Callbacks;

/// <summary>
/// Receives authorized friend requests, decisions and friendship changes.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IFriendshipServiceCallback
{
    /// <summary>
    /// Notifies the recipient of a confirmed pending friend request.
    /// </summary>
    /// <param name="request">
    /// The confirmed pending friend request addressed to the recipient.
    /// </param>
    [OperationContract(IsOneWay = true)]
    public void OnFriendRequestReceived(FriendRequest request);

    /// <summary>
    /// Notifies the sender that a pending friend request was accepted or rejected.
    /// </summary>
    /// <param name="requestId">
    /// The stable identifier of the pending friend request.
    /// </param>
    /// <param name="accepted">
    /// True when the request was accepted, or false when it was rejected.
    /// </param>
    [OperationContract(IsOneWay = true)]
    public void OnFriendRequestResponded(string requestId, bool accepted);

    /// <summary>
    /// Notifies an affected account of a friend presence change or removal.
    /// </summary>
    /// <param name="accountId">
    /// The stable identifier of the friend whose presence or relationship changed.
    /// </param>
    /// <param name="change">
    /// The confirmed connection, disconnection or friendship removal change.
    /// </param>
    [OperationContract(IsOneWay = true)]
    public void OnFriendChanged(long accountId, FriendChange change);
}
