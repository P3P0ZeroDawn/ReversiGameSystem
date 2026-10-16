using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Players;

/// <summary>
/// Represents the criteria used to retrieve a player profile.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileCriteria
{
    /// <summary>
    /// Gets or sets the authenticated session.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public SessionIdentity Session { get; set; } = new SessionIdentity();

    /// <summary>
    /// Gets or sets the target account, or null for the authenticated account.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public AccountReference? TargetAccount { get; set; }
}
