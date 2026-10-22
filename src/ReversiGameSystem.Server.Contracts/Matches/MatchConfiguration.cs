using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Contains the proposed or confirmed configuration of a match.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MatchConfiguration
{
    /// <summary>
    /// Gets or sets the configured turn duration in seconds, from 10 through 120.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int TurnDurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the proposed or confirmed opponent kind.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public OpponentKind OpponentKind { get; set; }

    /// <summary>
    /// Gets or sets whether the match is eligible to contribute to account statistics.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public bool CountsTowardStatistics { get; set; }
}
