using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

/// <summary>
/// Represents player profile dto.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileDto
{
    /// <summary>
    /// Gets or sets the account id.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public long AccountId { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile image uri.
    /// </summary>
    [DataMember(Order = 3, EmitDefaultValue = false)]
    public string? ProfileImageUri { get; set; }

    /// <summary>
    /// Gets or sets the statistics.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public PlayerStatisticsDto Statistics { get; set; } = new PlayerStatisticsDto();
}
