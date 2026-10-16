using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Represents the expected outcome of account authentication.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AuthenticationOutcome
{
    /// <summary>
    /// Gets or sets the authentication status.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public AuthenticationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the authenticated session when authentication succeeds.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public AuthenticatedSession? Session { get; set; }
}
