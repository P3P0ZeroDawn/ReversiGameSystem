using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Summarizes a match in the authenticated account history.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MatchSummary
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
    /// Gets or sets the seat assigned to the account that owns this match history.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public int AssignedSeatNumber { get; set; }

    /// <summary>
    /// Gets or sets the confirmed match lifecycle state.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public MatchState State { get; set; }

    /// <summary>
    /// Gets or sets the UTC match registration time used to order match history.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public DateTime RecordedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the confirmed number of black pieces.
    /// </summary>
    [DataMember(Order = 6, IsRequired = true)]
    public int BlackPieceCount { get; set; }

    /// <summary>
    /// Gets or sets the confirmed number of white pieces.
    /// </summary>
    [DataMember(Order = 7, IsRequired = true)]
    public int WhitePieceCount { get; set; }

    /// <summary>
    /// Gets or sets the confirmed match conclusion, or null while the match is unfinished.
    /// </summary>
    [DataMember(Order = 8, EmitDefaultValue = false)]
    public MatchConclusion? Conclusion { get; set; }
}
