using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Leaderboards;

/// <summary>
/// Represents leaderboard request.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardRequest
{
    /// <summary>
    /// Gets or sets the maximum entries.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int MaximumEntries { get; set; }
}
