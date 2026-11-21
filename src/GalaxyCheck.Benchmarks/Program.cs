using BenchmarkDotNet.Running;

namespace GalaxyCheck.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (System.Array.IndexOf(args, "--report") >= 0 || System.Array.IndexOf(args, "--gate") >= 0)
            {
                throw new System.NotImplementedException("not implemented");
            }

            BenchmarkRunner.Run<RngBenchmark>();
            BenchmarkRunner.Run<ReflectedGenBenchmark>();
            BenchmarkRunner.Run<ListGenBenchmark>();
            BenchmarkRunner.Run<SelectManyGenBenchmark>();
            BenchmarkRunner.Run<SelectGenBenchmark>();
        }
    }
}
