using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Describes a recognized friend request or friendship rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class FriendshipFault
{
    /// <summary>
    /// Gets or sets the recognized rejection reason.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public FriendshipFailure Reason { get; set; }
}
