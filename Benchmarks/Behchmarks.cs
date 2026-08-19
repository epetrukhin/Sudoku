using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[Config(typeof(Config))]
// ReSharper disable once ClassCanBeSealed.Global
public class Behchmarks
{
    [GlobalSetup]
    public void Setup()
    {}

    [IterationSetup]
    public void IterationSetup()
    {}

    [Benchmark]
    public object Size()
    {
        return new ConcurrentDictionary<long, object?>(Enumerable.Range(1, 300_000).Select(x => new KeyValuePair<long, object?>(x, null)));
    }
}