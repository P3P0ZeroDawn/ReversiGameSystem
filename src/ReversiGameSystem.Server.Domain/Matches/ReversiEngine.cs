using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Domain.Matches;

/// <summary>
/// Provides stateless Reversi board initialization, move validation, and capture evaluation rules.
/// </summary>
public static class ReversiEngine
{
    public const int Empty = 0;
    public const int Black = 1;
    public const int White = 2;
    public const int BoardSize = 8;
    public const int CellCount = 64;

    private static readonly int[] RowOffsets = new int[] { -1, -1, -1, 0, 0, 1, 1, 1 };
    private static readonly int[] ColumnOffsets = new int[] { -1, 0, 1, -1, 1, -1, 0, 1 };

    public static int[] CreateInitialBoard()
    {
        int[] board = new int[CellCount];
        board[(3 * BoardSize) + 3] = White;
        board[(3 * BoardSize) + 4] = Black;
        board[(4 * BoardSize) + 3] = Black;
        board[(4 * BoardSize) + 4] = White;
        return board;
    }

    /// <summary>
    /// Flips captured opponent discs on <paramref name="board"/> in place when the move is legal,
    /// returning the flipped flat cell indices (0..63) or an empty array if invalid.
    /// </summary>
    public static int[] ApplyMove(int[] board, int row, int column, int disc)
    {
        ValidateBoard(board);
        List<int> capturedIndices = FindCapturedIndices(board, row, column, disc);
        if (capturedIndices.Count == 0)
        {
            return Array.Empty<int>();
        }

        board[(row * BoardSize) + column] = disc;
        for (int i = 0; i < capturedIndices.Count; i++)
        {
            board[capturedIndices[i]] = disc;
        }

        return capturedIndices.ToArray();
    }

    public static bool HasValidMoves(int[] board, int disc)
    {
        ValidateBoard(board);
        for (int row = 0; row < BoardSize; row++)
        {
            for (int column = 0; column < BoardSize; column++)
            {
                if (FindCapturedIndices(board, row, column, disc).Count > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsLegalMove(int[] board, int row, int column, int disc)
    {
        ValidateBoard(board);
        return FindCapturedIndices(board, row, column, disc).Count > 0;
    }

    private static void ValidateBoard(int[] board)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        if (board.Length != CellCount)
        {
            throw new ArgumentException("The board must contain exactly 64 cells.", nameof(board));
        }
    }

    private static List<int> FindCapturedIndices(int[] board, int row, int column, int disc)
    {
        List<int> capturedIndices = new List<int>();
        if ((disc != Black && disc != White)
            || row < 0
            || row >= BoardSize
            || column < 0
            || column >= BoardSize
            || board[(row * BoardSize) + column] != Empty)
        {
            return capturedIndices;
        }

        int opponent = disc == Black ? White : Black;
        List<int> lineBuffer = new List<int>(BoardSize);

        for (int direction = 0; direction < 8; direction++)
        {
            lineBuffer.Clear();
            int currentRow = row + RowOffsets[direction];
            int currentColumn = column + ColumnOffsets[direction];

            while (currentRow >= 0
                && currentRow < BoardSize
                && currentColumn >= 0
                && currentColumn < BoardSize)
            {
                int index = (currentRow * BoardSize) + currentColumn;
                int cellValue = board[index];
                if (cellValue == opponent)
                {
                    lineBuffer.Add(index);
                }
                else if (cellValue == disc)
                {
                    if (lineBuffer.Count > 0)
                    {
                        capturedIndices.AddRange(lineBuffer);
                    }

                    break;
                }
                else
                {
                    break;
                }

                currentRow += RowOffsets[direction];
                currentColumn += ColumnOffsets[direction];
            }
        }

        return capturedIndices;
    }
}
