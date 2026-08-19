using System;
using Solvers;
using Tests;
using static Solvers.Constants;

namespace Runner;

internal static class Program
{
    private static void Main()
    {
        CompareSolverResults();
        return;

        var sudoku = FormatConverter.FromSudokuWikiToLeetCode(TestCases.Diabolical12);
        var result = FormatConverter.FromSudokuWikiToLeetCode(TestCases.Diabolical12);

        var board = V1Solver.ParseBoard(result);
        board = V1Solver.Board.Solve(board);
        board.DumpTo(result);

        Dump(sudoku, result);
    }

    private static void CompareSolverResults()
    {
        foreach (var sudoku in TestCases.All)
            CompareSolverResults(sudoku);
    }

    private static void CompareSolverResults(string sudoku)
    {
        var v1Result = FormatConverter.FromSudokuWikiToLeetCode(sudoku);
        var v1Board = V1Solver.ParseBoard(v1Result);
        v1Board = V1Solver.Board.Solve(v1Board);
        v1Board.DumpTo(v1Result);

        var v2Result = FormatConverter.FromSudokuWikiToLeetCode(sudoku);
        var v2Board = V2Solver.ParseBoard(v2Result);
        v2Board = V2Solver.Board.Solve(v2Board);
        v2Board.DumpTo(v2Result);

        if (FormatConverter.FromLeetCodeToSudokuWiki(v1Result) != FormatConverter.FromLeetCodeToSudokuWiki(v2Result))
            Dump(v1Result, v2Result);
    }

    private static void Dump(char[][] left, char[][] right)
    {
        for (var ri = 0; ri < BoardSize; ri++)
        {
            var sourceRow = left[ri];
            var solvedRow = right[ri];

            Console.Write(new string(sourceRow));
            Console.Write('\t');
            Console.WriteLine(new string(solvedRow));
        }
    }
}