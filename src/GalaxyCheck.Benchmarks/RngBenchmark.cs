using BenchmarkDotNet.Attributes;
using GalaxyCheck.Gens.Parameters;

namespace GalaxyCheck.Benchmarks
{
    [SimpleJob]
    public class RngBenchmark
    {
        private const int OperationsPerInvoke = 100_000;
        private int _seed = 0;

        [IterationCleanup]
        public void IncrementRng()
        {
            _seed += OperationsPerInvoke;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [Arguments(0, 10)]
        [Arguments(0, 100)]
        [Arguments(0, 1000)]
        [Arguments(long.MinValue, long.MaxValue)]
        public void RandomLong(long minValue, long maxValue)
        {
            for (var seed = _seed; seed < (_seed + OperationsPerInvoke); seed++)
            {
                var source = GenSource.Create(seed, 0);
                source.NextBounded(unchecked((ulong)maxValue - (ulong)minValue));
            }
        }
    }
}
