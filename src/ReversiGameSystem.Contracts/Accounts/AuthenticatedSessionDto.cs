using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

/// <summary>
/// Represents authenticated session dto.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AuthenticatedSessionDto
{
    /// <summary>
    /// Gets or sets the session token.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account id.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public long AccountId { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public string EmailAddress { get; set; } = string.Empty;
}
