using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Leaderboards;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public int MaximumEntries { get; set; }
}
