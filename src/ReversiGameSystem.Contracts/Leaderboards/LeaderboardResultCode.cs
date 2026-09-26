using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Leaderboards;

[DataContract(Namespace = ContractNamespaces.Data)]
public enum LeaderboardResultCode
{
    [EnumMember]
    Success,

    [EnumMember]
    InvalidRequest,

    [EnumMember]
    ServiceUnavailable
}
