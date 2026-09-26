using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

[DataContract(Namespace = ContractNamespaces.Data)]
public enum PlayerSummaryResultCode
{
    [EnumMember]
    Success,

    [EnumMember]
    InvalidRequest,

    [EnumMember]
    InvalidSession,

    [EnumMember]
    AccountNotFound,

    [EnumMember]
    ServiceUnavailable
}
