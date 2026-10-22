using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Contains the confirmed result and terminal state version of a match.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MatchConclusion
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
    /// Gets or sets the confirmed match end reason.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public MatchEndReason Reason { get; set; }

    /// <summary>
    /// Gets or sets the winning seat, or null for a draw.
    /// </summary>
    [DataMember(Order = 4, EmitDefaultValue = false)]
    public int? WinnerSeatNumber { get; set; }

    /// <summary>
    /// Gets or sets the seat that abandoned the match, when applicable.
    /// </summary>
    [DataMember(Order = 5, EmitDefaultValue = false)]
    public int? AbandoningSeatNumber { get; set; }

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
    /// Gets or sets the UTC completion time confirmed by the server.
    /// </summary>
    [DataMember(Order = 8, IsRequired = true)]
    public DateTime EndedAtUtc { get; set; }
}
