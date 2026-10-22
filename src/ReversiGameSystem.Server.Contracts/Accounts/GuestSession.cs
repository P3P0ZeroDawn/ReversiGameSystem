using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Represents an opaque guest session issued by the server.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class GuestSession
{
    /// <summary>
    /// Gets or sets the opaque session token issued by the server.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confirmed display name of the guest or participant.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC expiration of the session.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public DateTime ExpiresAtUtc { get; set; }
}
