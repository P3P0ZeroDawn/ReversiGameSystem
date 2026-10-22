using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Identifies a rejected leaderboard entry limit.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum LeaderboardFailure
{
    /// <summary>
    /// The requested entry limit is outside the inclusive range from 1 through 100.
    /// </summary>
    [EnumMember]
    InvalidMaximumEntries,
}
