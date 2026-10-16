using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Leaderboards;

/// <summary>
/// Represents a generated snapshot of leaderboard standings.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardStandings
{
    /// <summary>
    /// Gets or sets the lookup status.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public LeaderboardStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the standings were generated.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ranked positions.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public LeaderboardPosition[] Positions { get; set; } = Array.Empty<LeaderboardPosition>();
}
