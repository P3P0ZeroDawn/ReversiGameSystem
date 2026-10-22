using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Identifies a recognized player profile rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum ProfileFailure
{
    /// <summary>
    /// The target account identifier is not positive.
    /// </summary>
    [EnumMember]
    InvalidAccountId,

    /// <summary>
    /// The requested account does not exist.
    /// </summary>
    [EnumMember]
    AccountNotFound,

    /// <summary>
    /// The proposed username does not satisfy the username policy.
    /// </summary>
    [EnumMember]
    InvalidUsername,

    /// <summary>
    /// Another account already uses the username.
    /// </summary>
    [EnumMember]
    UsernameUnavailable,

    /// <summary>
    /// The image is absent or has invalid content, format, size or dimensions.
    /// </summary>
    [EnumMember]
    InvalidImage,

    /// <summary>
    /// The requesting or affected account is no longer active.
    /// </summary>
    [EnumMember]
    InactiveAccount,
}
