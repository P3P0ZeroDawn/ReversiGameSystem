using ReversiGameSystem.Game;
using ReversiGameSystem.Matches;

namespace ReversiGameSystem.Features.Matches;

internal static class MatchBoardMapper
{
    internal static ReversiBoard MapBoard(ConfirmedBoardCell[] board)
    {
        string[] rows = new string[ReversiBoard.Size];
        for (int row = 0; row < rows.Length; row++)
        {
            char[] cells = new char[ReversiBoard.Size];
            for (int column = 0; column < cells.Length; column++)
            {
                cells[column] = board[row * ReversiBoard.Size + column] switch
                {
                    ConfirmedBoardCell.Black => 'B',
                    ConfirmedBoardCell.White => 'W',
                    _ => '.'
                };
            }
            rows[row] = new string(cells);
        }
        return ReversiBoard.FromRows(rows);
    }
}
