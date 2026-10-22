using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Identifies an empty, black or white board cell.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum BoardCellState
{
    /// <summary>
    /// The board cell has no piece.
    /// </summary>
    [EnumMember]
    Empty,

    /// <summary>
    /// The board cell has a black piece.
    /// </summary>
    [EnumMember]
    Black,

    /// <summary>
    /// The board cell has a white piece.
    /// </summary>
    [EnumMember]
    White,
}
