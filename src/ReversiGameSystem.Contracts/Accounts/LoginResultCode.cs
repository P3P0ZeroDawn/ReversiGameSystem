using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

/// <summary>
/// Defines the supported login result code values.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum LoginResultCode
{
    /// <summary>
    /// Represents success.
    /// </summary>
    [EnumMember]
    Success,

    /// <summary>
    /// Represents invalid request.
    /// </summary>
    [EnumMember]
    InvalidRequest,

    /// <summary>
    /// Represents invalid credentials.
    /// </summary>
    [EnumMember]
    InvalidCredentials,

    /// <summary>
    /// Represents account inactive.
    /// </summary>
    [EnumMember]
    AccountInactive,

    /// <summary>
    /// Represents service unavailable.
    /// </summary>
    [EnumMember]
    ServiceUnavailable
}
