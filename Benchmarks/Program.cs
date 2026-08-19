using BenchmarkDotNet.Running;

namespace Benchmarks;

internal abstract class Program
{
    private static void Main()
    {
        var result = BenchmarkRunner.Run<Behchmarks>();

        Console.WriteLine(result);
        Console.ReadLine();
    }
}