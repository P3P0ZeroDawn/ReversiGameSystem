using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Domain.Matches;

/// <summary>
/// Provides stateless Reversi board initialization, move validation, and capture evaluation rules.
/// </summary>
public static class ReversiEngine
{
    /// <summary>Value representing an empty board cell.</summary>
    public const int Empty = 0;

    /// <summary>Value representing a Black disc (Seat 1).</summary>
    public const int Black = 1;

    /// <summary>Value representing a White disc (Seat 2).</summary>
    public const int White = 2;

    /// <summary>Width and height of a standard Reversi board.</summary>
    public const int BoardSize = 8;

    /// <summary>Total number of cells on a standard 8x8 Reversi board.</summary>
    public const int CellCount = 64;

    private static readonly int[] RowOffsets = new int[] { -1, -1, -1, 0, 0, 1, 1, 1 };
    private static readonly int[] ColumnOffsets = new int[] { -1, 0, 1, -1, 1, -1, 0, 1 };

    /// <summary>
    /// Creates a new 64-cell board array initialized with the standard 4 centered discs.
    /// </summary>
    /// <returns>A 64-element integer array in row-major order.</returns>
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
    /// Validates a move on the given board and, if legal, places the disc and flips all bracketed opponent discs in place.
    /// </summary>
    /// <param name="board">The 64-cell board state in row-major order.</param>
    /// <param name="row">Target zero-based row index (0..7).</param>
    /// <param name="column">Target zero-based column index (0..7).</param>
    /// <param name="disc">The player's disc value (<see cref="Black"/> or <see cref="White"/>).</param>
    /// <returns>
    /// An array containing the flat cell indices (0..63) of all flipped opponent discs when the move is valid;
    /// otherwise, an empty array when the move is out of bounds, targets an occupied cell, or captures no opponent discs.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="board"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="board"/> does not contain 64 cells.</exception>
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

    /// <summary>
    /// Determines whether the specified disc has at least one legal move on the given board.
    /// </summary>
    /// <param name="board">The 64-cell board state in row-major order.</param>
    /// <param name="disc">The player's disc value (<see cref="Black"/> or <see cref="White"/>).</param>
    /// <returns><see langword="true"/> if at least one legal placement exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="board"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="board"/> does not contain 64 cells.</exception>
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
