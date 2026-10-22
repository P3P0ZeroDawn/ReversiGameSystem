using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Identifies a recognized session or access rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum SessionFailure
{
    /// <summary>
    /// The account or password is incorrect without distinguishing the cause.
    /// </summary>
    [EnumMember]
    InvalidCredentials,

    /// <summary>
    /// The requesting or affected account is no longer active.
    /// </summary>
    [EnumMember]
    InactiveAccount,

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
    /// The proposed guest name is invalid.
    /// </summary>
    [EnumMember]
    InvalidGuestName,

    /// <summary>
    /// The proposed guest name is already reserved.
    /// </summary>
    [EnumMember]
    GuestNameUnavailable,

    /// <summary>
    /// The session token is absent or empty.
    /// </summary>
    [EnumMember]
    MissingSessionToken,

    /// <summary>
    /// The session is invalid or is a guest session where a registered account is required.
    /// </summary>
    [EnumMember]
    InvalidSession,
}
