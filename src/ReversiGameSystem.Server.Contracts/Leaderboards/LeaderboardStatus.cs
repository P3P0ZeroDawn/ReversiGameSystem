using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Leaderboards;

/// <summary>
/// Defines the expected results of a leaderboard lookup.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum LeaderboardStatus
{
    /// <summary>
    /// Indicates that the standings were retrieved.
    /// </summary>
    [EnumMember]
    Success,

    /// <summary>
    /// Indicates that the criteria are invalid.
    /// </summary>
    [EnumMember]
    InvalidCriteria,

    /// <summary>
    /// Indicates that the persistence resource is unavailable.
    /// </summary>
    [EnumMember]
    PersistenceUnavailable
}
