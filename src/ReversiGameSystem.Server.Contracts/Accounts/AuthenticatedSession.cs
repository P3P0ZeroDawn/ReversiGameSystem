using System;
using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Represents an authenticated session issued by the server.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AuthenticatedSession
{
    /// <summary>
    /// Gets or sets the opaque session token.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticated account.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public AccountReference Account { get; set; } = new AccountReference();

    /// <summary>
    /// Gets or sets the authenticated email address.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred locale code.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public string PreferredLocaleCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC expiration time.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public DateTime ExpiresAtUtc { get; set; }
}
