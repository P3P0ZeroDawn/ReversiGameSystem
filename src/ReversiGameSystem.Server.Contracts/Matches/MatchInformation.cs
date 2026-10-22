using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Contains base match information personalized for the authorized recipient.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MatchInformation
{
    /// <summary>
    /// Gets or sets the stable match identifier.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public long MatchId { get; set; }

    /// <summary>
    /// Gets or sets the confirmed participants and their seats within the match.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public MatchParticipantSummary[] Participants { get; set; } =
        Array.Empty<MatchParticipantSummary>();

    /// <summary>
    /// Gets or sets the recipient seat, or null when the recipient is an observer.
    /// </summary>
    [DataMember(Order = 3, EmitDefaultValue = false)]
    public int? AssignedSeatNumber { get; set; }

    /// <summary>
    /// Gets or sets the configured turn duration in seconds, from 10 through 120.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public int TurnDurationSeconds { get; set; }
}
