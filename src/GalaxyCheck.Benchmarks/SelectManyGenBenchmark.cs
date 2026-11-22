using BenchmarkDotNet.Attributes;
using GalaxyCheck.Gens.Parameters;
using System.Linq;

namespace GalaxyCheck.Benchmarks
{
    [SimpleJob]
    public class SelectManyGenBenchmark
    {

        private const int OperationsPerInvoke = 10000;

        private int _seed = 0;

        [IterationCleanup]
        public void IncrementRng()
        {
            _seed += 1;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void SelectManyGen()
        {
            var gen =
                from x in Gen.Int32()
                from y in Gen.Int32()
                select (x, y);

            gen.Sample(iterations: OperationsPerInvoke, seed: _seed, size: 100);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void SelectManyGenBaseline()
        {
            var gen = Gen.Int32();

            gen.Sample(iterations: OperationsPerInvoke * 2, seed: _seed, size: 100);
        }
    }
}
