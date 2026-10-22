using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Players;

/// <summary>
/// Contains the public account identity, image availability and match statistics.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfile
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
    /// Gets or sets the server-calculated statistics of completed matches.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public PlayerStatistics Statistics { get; set; } = new PlayerStatistics();
}
