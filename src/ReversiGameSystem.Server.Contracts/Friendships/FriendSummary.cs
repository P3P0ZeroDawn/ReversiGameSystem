using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Friendships;

/// <summary>
/// Contains a current friend identity, image availability and online presence.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class FriendSummary
{
    /// <summary>
    /// Gets or sets the public account identifier and username.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public AccountReference Account { get; set; } = new AccountReference();

    /// <summary>
    /// Gets or sets whether the account has a custom profile image.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public bool HasProfileImage { get; set; }

    /// <summary>
    /// Gets or sets whether the server currently considers the friend online.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public bool IsOnline { get; set; }
}
