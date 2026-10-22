using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Identifies a recognized friendship operation rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum FriendshipFailure
{
    /// <summary>
    /// The target account identifier is not positive.
    /// </summary>
    [EnumMember]
    InvalidAccountId,

    /// <summary>
    /// The sender attempted to send a friend request to the same account.
    /// </summary>
    [EnumMember]
    CannotSendRequestToSelf,

    /// <summary>
    /// The other account is absent or inactive for this operation.
    /// </summary>
    [EnumMember]
    AccountUnavailable,

    /// <summary>
    /// An accepted friendship already exists between the accounts.
    /// </summary>
    [EnumMember]
    FriendshipAlreadyExists,

    /// <summary>
    /// A friend request is already pending in either direction.
    /// </summary>
    [EnumMember]
    RequestAlreadyPending,

    /// <summary>
    /// The friend request identifier is absent or empty.
    /// </summary>
    [EnumMember]
    MissingRequestId,

    /// <summary>
    /// The friend request is absent or no longer pending.
    /// </summary>
    [EnumMember]
    RequestUnavailable,

    /// <summary>
    /// The requesting account is not the recipient of the pending friend request.
    /// </summary>
    [EnumMember]
    UnauthorizedRequest,

    /// <summary>
    /// No current friendship exists between the accounts.
    /// </summary>
    [EnumMember]
    FriendshipNotFound,

    /// <summary>
    /// The requesting or affected account is no longer active.
    /// </summary>
    [EnumMember]
    InactiveAccount,
}
