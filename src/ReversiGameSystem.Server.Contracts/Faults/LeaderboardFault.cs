using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Describes a rejected leaderboard entry limit.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardFault
{
    /// <summary>
    /// Gets or sets the recognized rejection reason.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public LeaderboardFailure Reason { get; set; }
}
