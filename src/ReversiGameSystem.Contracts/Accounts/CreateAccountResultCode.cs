using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

/// <summary>
/// Defines the supported create account result code values.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum CreateAccountResultCode
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
    /// Represents invalid username.
    /// </summary>
    [EnumMember]
    InvalidUsername,

    /// <summary>
    /// Represents invalid email address.
    /// </summary>
    [EnumMember]
    InvalidEmailAddress,

    /// <summary>
    /// Represents invalid password.
    /// </summary>
    [EnumMember]
    InvalidPassword,

    /// <summary>
    /// Represents username already exists.
    /// </summary>
    [EnumMember]
    UsernameAlreadyExists,

    /// <summary>
    /// Represents email address already exists.
    /// </summary>
    [EnumMember]
    EmailAddressAlreadyExists,

    /// <summary>
    /// Represents service unavailable.
    /// </summary>
    [EnumMember]
    ServiceUnavailable
}
