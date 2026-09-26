using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

[DataContract(Namespace = ContractNamespaces.Data)]
public enum CreateAccountResultCode
{
    [EnumMember]
    Success,

    [EnumMember]
    InvalidRequest,

    [EnumMember]
    InvalidUsername,

    [EnumMember]
    InvalidEmailAddress,

    [EnumMember]
    InvalidPassword,

    [EnumMember]
    UsernameAlreadyExists,

    [EnumMember]
    EmailAddressAlreadyExists,

    [EnumMember]
    ServiceUnavailable
}
