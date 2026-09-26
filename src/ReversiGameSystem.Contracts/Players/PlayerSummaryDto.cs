using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerSummaryDto
{
    [DataMember(Order = 1, IsRequired = true)]
    public long AccountId { get; set; }

    [DataMember(Order = 2, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 3, IsRequired = true)]
    public int MatchesPlayed { get; set; }

    [DataMember(Order = 4, IsRequired = true)]
    public int Wins { get; set; }

    [DataMember(Order = 5, IsRequired = true)]
    public int Losses { get; set; }

    [DataMember(Order = 6, IsRequired = true)]
    public int Draws { get; set; }

    [DataMember(Order = 7, IsRequired = true)]
    public double WinRate { get; set; }
}
