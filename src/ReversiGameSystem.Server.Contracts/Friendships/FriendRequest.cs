using System;
using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Friendships;

/// <summary>
/// Represents a pending friend request between two registered accounts.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class FriendRequest
{
    /// <summary>
    /// Gets or sets the stable opaque identifier of the pending friend request.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public identity of the account that sent the request.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public AccountReference Sender { get; set; } = new AccountReference();

    /// <summary>
    /// Gets or sets the public identity of the account that receives the request.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public AccountReference Recipient { get; set; } = new AccountReference();

    /// <summary>
    /// Gets or sets the UTC creation time assigned by the server.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public DateTime CreatedAtUtc { get; set; }
}
