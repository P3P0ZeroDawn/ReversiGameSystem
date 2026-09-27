using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

/// <summary>
/// Represents login request.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LoginRequest
{
    /// <summary>
    /// Gets or sets the login.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string Password { get; set; } = string.Empty;
}
