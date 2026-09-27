using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

/// <summary>
/// Represents player statistics dto.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerStatisticsDto
{
    /// <summary>
    /// Gets or sets the matches played.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int MatchesPlayed { get; set; }

    /// <summary>
    /// Gets or sets the wins.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public int Wins { get; set; }

    /// <summary>
    /// Gets or sets the losses.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public int Losses { get; set; }

    /// <summary>
    /// Gets or sets the draws.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public int Draws { get; set; }

    /// <summary>
    /// Gets or sets the win ratio from 0.0 to 1.0.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public double WinRate { get; set; }
}
