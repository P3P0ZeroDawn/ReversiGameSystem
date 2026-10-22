using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Identifies a registered, guest or system participant and its match seat.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MatchParticipantSummary
{
    /// <summary>
    /// Gets or sets the participant seat: one for black or two for white.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int SeatNumber { get; set; }

    /// <summary>
    /// Gets or sets whether the participant is registered, a guest or the system.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public ParticipantKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the registered account identifier, or null for a guest or system participant.
    /// </summary>
    [DataMember(Order = 3, EmitDefaultValue = false)]
    public long? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the confirmed display name of the guest or participant.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public string DisplayName { get; set; } = string.Empty;
}
