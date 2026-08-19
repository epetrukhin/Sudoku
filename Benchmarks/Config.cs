using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace Benchmarks;

public sealed class Config : ManualConfig
{
    public Config()
    {
        // AddHardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(StatisticColumn.P95, StatisticColumn.OperationsPerSecond, RankColumn.Arabic, CategoriesColumn.Default);
        AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);

        AddJob(
            Job.Default.WithRuntime(CoreRuntime.Core10_0)
                .WithLaunchCount(1)
                .WithWarmupCount(5)
                .WithIterationCount(10));
    }
}