using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Leaderboards;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardResponse
{
    [DataMember(Order = 1, IsRequired = true)]
    public LeaderboardResultCode ResultCode { get; set; }

    [DataMember(Order = 2, IsRequired = true)]
    public LeaderboardEntryDto[] Entries { get; set; } = Array.Empty<LeaderboardEntryDto>();
}
