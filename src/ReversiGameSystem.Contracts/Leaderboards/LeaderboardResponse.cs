using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Leaderboards;

/// <summary>
/// Represents leaderboard response.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardResponse
{
    /// <summary>
    /// Gets or sets the result code.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public LeaderboardResultCode ResultCode { get; set; }

    /// <summary>
    /// Gets or sets the entries.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public LeaderboardEntryDto[] Entries { get; set; } = Array.Empty<LeaderboardEntryDto>();
}
