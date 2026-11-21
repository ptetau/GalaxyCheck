using BenchmarkDotNet.Attributes;
using GalaxyCheck.Gens.Parameters;
using System.Linq;

namespace GalaxyCheck.Benchmarks
{
    [SimpleJob]
    public class SelectGenBenchmark
    {

        private const int OperationsPerInvoke = 50000;

        private int _seed = 0;

        [IterationCleanup]
        public void IncrementRng()
        {
            _seed += 1;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void SelectGen()
        {
            var gen = Gen.Int32().Select(x => x);

            gen.Sample(iterations: OperationsPerInvoke, seed: _seed, size: 100);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void SelectGenBaseline()
        {
            var gen = Gen.Int32();

            gen.Sample(iterations: OperationsPerInvoke, seed: _seed, size: 100);
        }
    }
}
