using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Players;

/// <summary>
/// Represents the public profile state of a registered player.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileSnapshot
{
    /// <summary>
    /// Gets or sets the account represented by the profile.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public AccountReference Account { get; set; } = new AccountReference();

    /// <summary>
    /// Gets or sets a value indicating whether the account has a profile image.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public bool HasProfileImage { get; set; }

    /// <summary>
    /// Gets or sets the completed-match statistics.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public PlayerStatistics Statistics { get; set; } = new PlayerStatistics();
}
