using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Common;

/// <summary>
/// Identifies an authenticated server session.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class SessionIdentity
{
    /// <summary>
    /// Gets or sets the opaque session token.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string SessionToken { get; set; } = string.Empty;
}
