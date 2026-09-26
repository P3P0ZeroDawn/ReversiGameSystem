using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

[DataContract(Namespace = ContractNamespaces.Data)]
public enum PlayerProfileResultCode
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
