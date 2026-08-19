using static Solvers.Constants;

namespace Solvers;

public static class FormatConverter
{
    public static char[][] FromSudokuWikiToLeetCode(string board)
    {
        ArgumentNullException.ThrowIfNull(board);

        if (board.Length != CellsCount)
            throw new ArgumentOutOfRangeException(nameof(board), board, $"Invalid length of input string: {board.Length}");
        if (board.Any(x => x is < '0' or > '9'))
            throw new ArgumentOutOfRangeException(nameof(board), board, "Invalid chars in input string");

        return board.Replace('0', '.').Chunk(BoardSize).ToArray();
    }

    public static string FromLeetCodeToSudokuWiki(char[][] board)
    {
        return new string(board.SelectMany(x => x).ToArray()).Replace('.', '0');
    }
}