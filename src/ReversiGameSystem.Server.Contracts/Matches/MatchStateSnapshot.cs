using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Contains the complete dynamic state of a confirmed match version.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MatchStateSnapshot
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
    /// Gets or sets the confirmed match lifecycle state.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public MatchState State { get; set; }

    /// <summary>
    /// Gets or sets the 64 board cells in row-major order.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public BoardCellState[] Board { get; set; } = Array.Empty<BoardCellState>();

    /// <summary>
    /// Gets or sets the confirmed number of black pieces.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public int BlackPieceCount { get; set; }

    /// <summary>
    /// Gets or sets the confirmed number of white pieces.
    /// </summary>
    [DataMember(Order = 6, IsRequired = true)]
    public int WhitePieceCount { get; set; }

    /// <summary>
    /// Gets or sets the seat holding the active turn, or null after the match ends.
    /// </summary>
    [DataMember(Order = 7, EmitDefaultValue = false)]
    public int? CurrentTurnSeatNumber { get; set; }

    /// <summary>
    /// Gets or sets the UTC start of the active turn, or null after the match ends.
    /// </summary>
    [DataMember(Order = 8, EmitDefaultValue = false)]
    public DateTime? TurnStartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the placement in this state transition, or null for a transition without
    /// placement.
    /// </summary>
    [DataMember(Order = 9, EmitDefaultValue = false)]
    public MoveRecord? LastMove { get; set; }

    /// <summary>
    /// Gets or sets the seat whose turn was passed in this transition, if applicable.
    /// </summary>
    [DataMember(Order = 10, EmitDefaultValue = false)]
    public int? SkippedSeatNumber { get; set; }

    /// <summary>
    /// Gets or sets the confirmed match conclusion, or null while the match is unfinished.
    /// </summary>
    [DataMember(Order = 11, EmitDefaultValue = false)]
    public MatchConclusion? Conclusion { get; set; }
}
