using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

[DataContract(Namespace = ContractNamespaces.Data)]
public enum LoginResultCode
{
    [EnumMember]
    Success,

    [EnumMember]
    InvalidRequest,

    [EnumMember]
    InvalidCredentials,

    [EnumMember]
    AccountInactive,

    [EnumMember]
    ServiceUnavailable
}
