using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

/// <summary>
/// Defines the supported player profile result code values.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum PlayerProfileResultCode
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
    /// Represents invalid session.
    /// </summary>
    [EnumMember]
    InvalidSession,

    /// <summary>
    /// Represents account not found.
    /// </summary>
    [EnumMember]
    AccountNotFound,

    /// <summary>
    /// Represents service unavailable.
    /// </summary>
    [EnumMember]
    ServiceUnavailable
}
