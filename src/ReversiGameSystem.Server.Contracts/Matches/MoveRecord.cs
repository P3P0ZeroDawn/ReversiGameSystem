using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Records a confirmed piece placement and its origin within a match.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MoveRecord
{
    /// <summary>
    /// Gets or sets the consecutive one-based number of the confirmed placement.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int MoveNumber { get; set; }

    /// <summary>
    /// Gets or sets the participant seat: one for black or two for white.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public int SeatNumber { get; set; }

    /// <summary>
    /// Gets or sets the zero-based board row from zero through seven.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public int Row { get; set; }

    /// <summary>
    /// Gets or sets the zero-based board column from zero through seven.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the UTC placement time confirmed by the server.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public DateTime PlayedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the number of flipped opposing pieces, excluding the piece placed.
    /// </summary>
    [DataMember(Order = 6, IsRequired = true)]
    public int FlippedPieceCount { get; set; }

    /// <summary>
    /// Gets or sets whether the placement came from a player, a timeout or the system.
    /// </summary>
    [DataMember(Order = 7, IsRequired = true)]
    public MoveOrigin Origin { get; set; }
}
