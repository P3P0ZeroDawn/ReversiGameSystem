using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Players;

/// <summary>
/// Defines the expected results of a player profile lookup.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum PlayerProfileStatus
{
    /// <summary>
    /// Indicates that the profile was retrieved.
    /// </summary>
    [EnumMember]
    Success,

    /// <summary>
    /// Indicates that the lookup criteria are invalid.
    /// </summary>
    [EnumMember]
    InvalidCriteria,

    /// <summary>
    /// Indicates that the session is invalid or expired.
    /// </summary>
    [EnumMember]
    InvalidSession,

    /// <summary>
    /// Indicates that the account was not found.
    /// </summary>
    [EnumMember]
    AccountNotFound,

    /// <summary>
    /// Indicates that the persistence resource is unavailable.
    /// </summary>
    [EnumMember]
    PersistenceUnavailable
}
