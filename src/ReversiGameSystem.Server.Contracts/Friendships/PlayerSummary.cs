using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Friendships;

/// <summary>
/// Contains the public account identity and image availability of a found player.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerSummary
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
}
