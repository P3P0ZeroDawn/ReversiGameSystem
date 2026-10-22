using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Contains coherent match information, moves and board frames for replay.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MatchDetails
{
    /// <summary>
    /// Gets or sets the stable match identifier.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public long MatchId { get; set; }

    /// <summary>
    /// Gets or sets the confirmed nonnegative version of the match state.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public long MatchStateVersion { get; set; }

    /// <summary>
    /// Gets or sets the confirmed participants and their seats within the match.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public MatchParticipantSummary[] Participants { get; set; } =
        Array.Empty<MatchParticipantSummary>();

    /// <summary>
    /// Gets or sets the configured turn duration in seconds, from 10 through 120.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public int TurnDurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the confirmed match lifecycle state.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public MatchState State { get; set; }

    /// <summary>
    /// Gets or sets the UTC match registration time used to order match history.
    /// </summary>
    [DataMember(Order = 6, IsRequired = true)]
    public DateTime RecordedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the confirmed match conclusion, or null while the match is unfinished.
    /// </summary>
    [DataMember(Order = 7, EmitDefaultValue = false)]
    public MatchConclusion? Conclusion { get; set; }

    /// <summary>
    /// Gets or sets the confirmed placements in ascending move number order.
    /// </summary>
    [DataMember(Order = 8, IsRequired = true)]
    public MoveRecord[] Moves { get; set; } = Array.Empty<MoveRecord>();

    /// <summary>
    /// Gets or sets the initial board followed by one 64-cell board per confirmed placement.
    /// </summary>
    [DataMember(Order = 9, IsRequired = true)]
    public BoardCellState[][] BoardFrames { get; set; } = Array.Empty<BoardCellState[]>();
}
