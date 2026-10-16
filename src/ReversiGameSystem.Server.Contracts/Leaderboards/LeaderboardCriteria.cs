using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Leaderboards;

/// <summary>
/// Represents the criteria used to retrieve leaderboard standings.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardCriteria
{
    /// <summary>
    /// Gets or sets the maximum number of positions to retrieve.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int MaximumEntries { get; set; }
}
