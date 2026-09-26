using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerStatisticsDto
{
    [DataMember(Order = 1, IsRequired = true)]
    public int MatchesPlayed { get; set; }

    [DataMember(Order = 2, IsRequired = true)]
    public int Wins { get; set; }

    [DataMember(Order = 3, IsRequired = true)]
    public int Losses { get; set; }

    [DataMember(Order = 4, IsRequired = true)]
    public int Draws { get; set; }

    /// <summary>
    /// Gets or sets the win ratio from 0.0 to 1.0.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public double WinRate { get; set; }
}
