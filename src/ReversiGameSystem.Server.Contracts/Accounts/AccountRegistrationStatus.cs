using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Defines the expected results of account registration.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum AccountRegistrationStatus
{
    /// <summary>
    /// Indicates that the account was registered.
    /// </summary>
    [EnumMember]
    Success,

    /// <summary>
    /// Indicates that the username is unavailable.
    /// </summary>
    [EnumMember]
    UsernameUnavailable,

    /// <summary>
    /// Indicates that the email address is unavailable.
    /// </summary>
    [EnumMember]
    EmailUnavailable,

    /// <summary>
    /// Indicates that the username is invalid.
    /// </summary>
    [EnumMember]
    InvalidUsername,

    /// <summary>
    /// Indicates that the email address is invalid.
    /// </summary>
    [EnumMember]
    InvalidEmail,

    /// <summary>
    /// Indicates that the password is invalid.
    /// </summary>
    [EnumMember]
    InvalidPassword,

    /// <summary>
    /// Indicates that the locale is invalid or unsupported.
    /// </summary>
    [EnumMember]
    InvalidLocale,

    /// <summary>
    /// Indicates that the profile image is invalid.
    /// </summary>
    [EnumMember]
    InvalidProfileImage,

    /// <summary>
    /// Indicates that the persistence resource is unavailable.
    /// </summary>
    [EnumMember]
    PersistenceUnavailable
}
