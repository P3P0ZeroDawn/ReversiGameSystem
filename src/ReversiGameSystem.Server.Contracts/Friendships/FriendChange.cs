using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Friendships;

/// <summary>
/// Identifies a presence or removal change concerning one friend.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum FriendChange
{
    /// <summary>
    /// The friend is online.
    /// </summary>
    [EnumMember]
    Connected,

    /// <summary>
    /// The friend is offline.
    /// </summary>
    [EnumMember]
    Disconnected,

    /// <summary>
    /// The friendship was removed.
    /// </summary>
    [EnumMember]
    Removed,
}
