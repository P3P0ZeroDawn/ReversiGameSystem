using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Defines the expected results of authentication.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum AuthenticationStatus
{
    /// <summary>
    /// Indicates that authentication succeeded.
    /// </summary>
    [EnumMember]
    Success,

    /// <summary>
    /// Indicates that the supplied credentials are invalid.
    /// </summary>
    [EnumMember]
    InvalidCredentials,

    /// <summary>
    /// Indicates that the account is inactive.
    /// </summary>
    [EnumMember]
    InactiveAccount,

    /// <summary>
    /// Indicates that the persistence resource is unavailable.
    /// </summary>
    [EnumMember]
    PersistenceUnavailable
}
