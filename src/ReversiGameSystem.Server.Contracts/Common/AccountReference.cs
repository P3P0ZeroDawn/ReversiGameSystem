using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Common;

/// <summary>
/// Represents the public identity of a registered player account.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AccountReference
{
    /// <summary>
    /// Gets or sets the stable account identifier.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public long AccountId { get; set; }

    /// <summary>
    /// Gets or sets the public username.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string Username { get; set; } = string.Empty;
}
