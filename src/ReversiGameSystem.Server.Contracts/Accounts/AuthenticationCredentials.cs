using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Represents the credentials used to authenticate an account.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AuthenticationCredentials
{
    /// <summary>
    /// Gets or sets the username or email address supplied by the player.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string LoginIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plaintext password provided in transit.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string Password { get; set; } = string.Empty;
}
