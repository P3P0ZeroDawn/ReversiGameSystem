using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Players;

/// <summary>
/// Represents statistics calculated from completed matches.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerStatistics
{
    /// <summary>
    /// Gets or sets the number of completed matches.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int MatchesPlayed { get; set; }

    /// <summary>
    /// Gets or sets the number of wins.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public int Wins { get; set; }

    /// <summary>
    /// Gets or sets the number of losses.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public int Losses { get; set; }

    /// <summary>
    /// Gets or sets the number of draws.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public int Draws { get; set; }

    /// <summary>
    /// Gets or sets the ratio of wins to completed matches.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public double WinRate { get; set; }
}
