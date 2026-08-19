using BenchmarkDotNet.Attributes;
using Solvers;
using Tests;

namespace Benchmarks;

[Config(typeof(Config))]
// ReSharper disable once ClassCanBeSealed.Global
public class Behchmarks
{
    private static readonly char[][] Result = new char[Constants.BoardSize][];

    private static char[][][] Easy = null!;
    private static char[][][] Medium = null!;
    private static char[][][] Hard = null!;
    private static char[][][] Diabolical = null!;

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < Constants.BoardSize; i++)
            Result[i] = new char[Constants.BoardSize];

        Easy = TestCases.AllEasy.Select(FormatConverter.FromSudokuWikiToLeetCode).ToArray();
        Medium = TestCases.AllMedium.Select(FormatConverter.FromSudokuWikiToLeetCode).ToArray();
        Hard = TestCases.AllHard.Select(FormatConverter.FromSudokuWikiToLeetCode).ToArray();
        Diabolical = TestCases.AllDiabolical.Select(FormatConverter.FromSudokuWikiToLeetCode).ToArray();
    }

    [IterationSetup]
    public void IterationSetup()
    {}

    private static void V1Solve(char[][][] sudokus)
    {
        var result = Result;

        foreach (var sudoku in sudokus)
            V1Solver.Solve(sudoku, result);
    }

    private static void V2Solve(char[][][] sudokus)
    {
        var result = Result;

        foreach (var sudoku in sudokus)
            V2Solver.Solve(sudoku, result);
    }

    [Benchmark(Baseline = true)]
    public void V1Easy()
    {
        V1Solve(Easy);
    }

    [Benchmark]
    public void V1Medium()
    {
        V1Solve(Medium);
    }

    [Benchmark]
    public void V1Hard()
    {
        V1Solve(Hard);
    }

    [Benchmark]
    public void V1Diabolical()
    {
        V1Solve(Diabolical);
    }

    [Benchmark]
    public void V2Easy()
    {
        V2Solve(Easy);
    }

    [Benchmark]
    public void V2Medium()
    {
        V2Solve(Medium);
    }

    [Benchmark]
    public void V2Hard()
    {
        V2Solve(Hard);
    }

    [Benchmark]
    public void V2Diabolical()
    {
        V2Solve(Diabolical);
    }
}