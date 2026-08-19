using BenchmarkDotNet.Attributes;
using Solvers;
using Tests;

namespace Benchmarks;

[Config(typeof(Config))]
// ReSharper disable once ClassCanBeSealed.Global
public class Behchmarks
{
    private const int V1OperationPerInvoke = 50;
    private const int V2OperationPerInvoke = 500;

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

        for (var i = 0; i < V1OperationPerInvoke; i++)
        {
            foreach (var sudoku in sudokus)
                V1Solver.Solve(sudoku, result);
        }
    }

    private static void V2Solve(char[][][] sudokus)
    {
        var result = Result;

        for (var i = 0; i < V2OperationPerInvoke; i++)
        {
            foreach (var sudoku in sudokus)
                V2Solver.Solve(sudoku, result);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = V1OperationPerInvoke), BenchmarkCategory("V1")]
    public void V1Easy()
    {
        V1Solve(Easy);
    }

    [Benchmark(OperationsPerInvoke = V1OperationPerInvoke), BenchmarkCategory("V1")]
    public void V1Medium()
    {
        V1Solve(Medium);
    }

    [Benchmark(OperationsPerInvoke = V1OperationPerInvoke), BenchmarkCategory("V1")]
    public void V1Hard()
    {
        V1Solve(Hard);
    }

    [Benchmark(OperationsPerInvoke = V1OperationPerInvoke), BenchmarkCategory("V1")]
    public void V1Diabolical()
    {
        V1Solve(Diabolical);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = V2OperationPerInvoke), BenchmarkCategory("V2")]
    public void V2Easy()
    {
        V2Solve(Easy);
    }

    [Benchmark(OperationsPerInvoke = V2OperationPerInvoke), BenchmarkCategory("V2")]
    public void V2Medium()
    {
        V2Solve(Medium);
    }

    [Benchmark(OperationsPerInvoke = V2OperationPerInvoke), BenchmarkCategory("V2")]
    public void V2Hard()
    {
        V2Solve(Hard);
    }

    [Benchmark(OperationsPerInvoke = V2OperationPerInvoke), BenchmarkCategory("V2")]
    public void V2Diabolical()
    {
        V2Solve(Diabolical);
    }
}