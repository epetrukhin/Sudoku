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

        // var sudoku = FormatConverter.FromSudokuWikiToLeetCode(TestCases.Diabolical12);
        // var result = FormatConverter.FromSudokuWikiToLeetCode(TestCases.Diabolical12);
        //
        // V1Solver.Solve(result, result);
        //
        // Dump(sudoku, result);
    }

    private static void CompareSolverResults()
    {
        foreach (var sudoku in TestCases.All)
            CompareSolverResults(sudoku);
    }

    private static void CompareSolverResults(string sudoku)
    {
        var v1 = FormatConverter.FromSudokuWikiToLeetCode(sudoku);
        V1Solver.Solve(v1, v1);

        var v2 = FormatConverter.FromSudokuWikiToLeetCode(sudoku);
        V2Solver.Solve(v2, v2);

        if (FormatConverter.FromLeetCodeToSudokuWiki(v1) != FormatConverter.FromLeetCodeToSudokuWiki(v2))
            Dump(v1, v2);
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