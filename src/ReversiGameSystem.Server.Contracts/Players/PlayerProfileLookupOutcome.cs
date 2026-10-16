using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Players;

/// <summary>
/// Represents the expected outcome of a player profile lookup.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileLookupOutcome
{
    /// <summary>
    /// Gets or sets the lookup status.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public PlayerProfileStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the profile when the lookup succeeds.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public PlayerProfileSnapshot? Profile { get; set; }
}
