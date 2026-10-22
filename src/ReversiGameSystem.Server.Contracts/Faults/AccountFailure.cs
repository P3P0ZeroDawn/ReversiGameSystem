using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Identifies a recognized account operation rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum AccountFailure
{
    /// <summary>
    /// The registration object is absent.
    /// </summary>
    [EnumMember]
    MissingRegistrationData,

    /// <summary>
    /// The proposed username does not satisfy the username policy.
    /// </summary>
    [EnumMember]
    InvalidUsername,

    /// <summary>
    /// The supplied email address does not satisfy the email policy.
    /// </summary>
    [EnumMember]
    InvalidEmailAddress,

    /// <summary>
    /// The proposed password does not satisfy the password policy.
    /// </summary>
    [EnumMember]
    InvalidPassword,

    /// <summary>
    /// The locale is absent or unsupported.
    /// </summary>
    [EnumMember]
    InvalidLocale,

    /// <summary>
    /// The image is absent or has invalid content, format, size or dimensions.
    /// </summary>
    [EnumMember]
    InvalidImage,

    /// <summary>
    /// Another account already uses the username.
    /// </summary>
    [EnumMember]
    UsernameUnavailable,

    /// <summary>
    /// An account already uses the email address.
    /// </summary>
    [EnumMember]
    EmailUnavailable,

    /// <summary>
    /// The code is incorrect, consumed or unrelated to an authorized process.
    /// </summary>
    [EnumMember]
    InvalidVerificationCode,

    /// <summary>
    /// The code associated with the authorized process has expired.
    /// </summary>
    [EnumMember]
    ExpiredVerificationCode,

    /// <summary>
    /// The supplied current password does not match the stored password.
    /// </summary>
    [EnumMember]
    IncorrectCurrentPassword,

    /// <summary>
    /// The requesting or affected account is no longer active.
    /// </summary>
    [EnumMember]
    InactiveAccount,

    /// <summary>
    /// The account is participating in an active match.
    /// </summary>
    [EnumMember]
    ActiveMatchInProgress,
}
