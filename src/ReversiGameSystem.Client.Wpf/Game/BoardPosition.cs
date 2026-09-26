using System;

namespace ReversiGameSystem.Game;

/// <summary>
/// Identifies one zero-based square on a Reversi board.
/// </summary>
internal readonly struct BoardPosition : IEquatable<BoardPosition>
{
    internal BoardPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }

    internal int Row { get; }

    internal int Column { get; }

    internal string ToNotation()
    {
        return $"{(char)('A' + Column)}{Row + 1}";
    }

    public bool Equals(BoardPosition other)
    {
        return Row == other.Row && Column == other.Column;
    }

    public override bool Equals(object? obj)
    {
        return obj is BoardPosition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (Row * ReversiBoard.Size) + Column;
    }
}
