using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileDto
{
    [DataMember(Order = 1, IsRequired = true)]
    public long AccountId { get; set; }

    [DataMember(Order = 2, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 3, EmitDefaultValue = false)]
    public string? ProfileImageUri { get; set; }

    [DataMember(Order = 4, IsRequired = true)]
    public PlayerStatisticsDto Statistics { get; set; } = new PlayerStatisticsDto();
}
