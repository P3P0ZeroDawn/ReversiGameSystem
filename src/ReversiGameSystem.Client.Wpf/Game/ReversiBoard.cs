using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Game;

internal enum DiscColor
{
    Empty,
    Black,
    White
}

/// <summary>
/// Represents the square state of a Reversi board.
/// </summary>
internal sealed class ReversiBoard
{
    internal const int Size = 8;

    private static readonly (int Row, int Column)[] Directions =
    {
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1), (0, 1),
        (1, -1), (1, 0), (1, 1)
    };

    private readonly DiscColor[,] _cells = new DiscColor[Size, Size];

    internal DiscColor this[int row, int column]
    {
        get
        {
            if (row < 0 || row >= Size || column < 0 || column >= Size)
            {
                throw new ArgumentOutOfRangeException();
            }

            return _cells[row, column];
        }
    }

    internal static ReversiBoard CreateInitial()
    {
        ReversiBoard board = new ReversiBoard();
        board._cells[3, 3] = DiscColor.White;
        board._cells[3, 4] = DiscColor.Black;
        board._cells[4, 3] = DiscColor.Black;
        board._cells[4, 4] = DiscColor.White;
        return board;
    }

    internal static ReversiBoard FromRows(params string[] rows)
    {
        if (rows == null || rows.Length != Size)
        {
            throw new ArgumentException("A board requires exactly eight rows.", nameof(rows));
        }

        ReversiBoard board = new ReversiBoard();
        for (int row = 0; row < Size; row++)
        {
            if (rows[row] == null || rows[row].Length != Size)
            {
                throw new ArgumentException(
                    "Every board row requires exactly eight cells.",
                    nameof(rows));
            }

            for (int column = 0; column < Size; column++)
            {
                board._cells[row, column] = rows[row][column] switch
                {
                    '.' => DiscColor.Empty,
                    'B' => DiscColor.Black,
                    'W' => DiscColor.White,
                    _ => throw new ArgumentException(
                        "Board cells must use '.', 'B', or 'W'.",
                        nameof(rows))
                };
            }
        }

        return board;
    }

    internal int Count(DiscColor color)
    {
        int count = 0;
        for (int row = 0; row < Size; row++)
        {
            for (int column = 0; column < Size; column++)
            {
                if (_cells[row, column] == color)
                {
                    count++;
                }
            }
        }

        return count;
    }

    internal IReadOnlyList<BoardPosition> GetValidMoves(DiscColor color)
    {
        List<BoardPosition> positions = new List<BoardPosition>();
        if (color == DiscColor.Empty)
        {
            return positions;
        }

        for (int row = 0; row < Size; row++)
        {
            for (int column = 0; column < Size; column++)
            {
                BoardPosition position = new BoardPosition(row, column);
                if (_cells[row, column] == DiscColor.Empty
                    && GetFlippedPositions(color, position).Count > 0)
                {
                    positions.Add(position);
                }
            }
        }

        return positions;
    }

    internal bool TryApplyMove(
        DiscColor color,
        BoardPosition position,
        out ReversiBoard? updatedBoard)
    {
        updatedBoard = null;
        if (color == DiscColor.Empty
            || !IsInside(position.Row, position.Column)
            || _cells[position.Row, position.Column] != DiscColor.Empty)
        {
            return false;
        }

        IReadOnlyList<BoardPosition> flippedPositions =
            GetFlippedPositions(color, position);
        if (flippedPositions.Count == 0)
        {
            return false;
        }

        ReversiBoard result = Clone();
        result._cells[position.Row, position.Column] = color;
        foreach (BoardPosition flippedPosition in flippedPositions)
        {
            result._cells[flippedPosition.Row, flippedPosition.Column] = color;
        }

        updatedBoard = result;
        return true;
    }

    internal static DiscColor GetOpponent(DiscColor color)
    {
        return color switch
        {
            DiscColor.Black => DiscColor.White,
            DiscColor.White => DiscColor.Black,
            _ => throw new ArgumentOutOfRangeException(nameof(color))
        };
    }

    private ReversiBoard Clone()
    {
        ReversiBoard clone = new ReversiBoard();
        Array.Copy(_cells, clone._cells, _cells.Length);
        return clone;
    }

    private IReadOnlyList<BoardPosition> GetFlippedPositions(
        DiscColor color,
        BoardPosition position)
    {
        List<BoardPosition> flippedPositions = new List<BoardPosition>();
        DiscColor opponent = GetOpponent(color);
        foreach ((int rowDirection, int columnDirection) in Directions)
        {
            List<BoardPosition> line = new List<BoardPosition>();
            int row = position.Row + rowDirection;
            int column = position.Column + columnDirection;
            while (IsInside(row, column) && _cells[row, column] == opponent)
            {
                line.Add(new BoardPosition(row, column));
                row += rowDirection;
                column += columnDirection;
            }

            if (line.Count > 0
                && IsInside(row, column)
                && _cells[row, column] == color)
            {
                flippedPositions.AddRange(line);
            }
        }

        return flippedPositions;
    }

    private static bool IsInside(int row, int column)
    {
        return row >= 0 && row < Size && column >= 0 && column < Size;
    }
}
