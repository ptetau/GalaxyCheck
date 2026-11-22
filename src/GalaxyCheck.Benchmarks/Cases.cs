using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using GalaxyCheck;

namespace GalaxyCheck.Benchmarks
{
    public record P(int A, string B, DateTime C, Guid D);

    /// <summary>
    /// The six benchmark cases. This file is compiled twice: into the benchmark program
    /// (current library) and into the baseline runner (library of commit f127a67).
    /// A value is one iteration of a check, one sampled value, or one shrink run.
    /// </summary>
    public static class Cases
    {
        public static readonly string[] Names =
        {
            "check int", "sample int", "sample string", "sample list of ints", "sample record", "shrink list of ints",
        };

        private const int Batch = 1000;

        // Runs one batch and returns the number of values it produced.
        private static Func<int> Batcher(string name)
        {
            switch (name)
            {
                case "check int":
                    return () =>
                    {
                        Property.ForAll(Gen.Int32(), x => true).Check(iterations: Batch);
                        return Batch;
                    };
                case "sample int":
                    return () => { Gen.Int32().Sample(iterations: Batch); return Batch; };
                case "sample string":
                    return () => { Gen.String().Sample(iterations: Batch); return Batch; };
                case "sample list of ints":
                    return () => { Gen.Int32().ListOf().Sample(iterations: Batch); return Batch; };
                case "sample record":
                    return () => { Gen.Create<P>().Sample(iterations: Batch); return Batch; };
                case "shrink list of ints":
                    return () =>
                    {
                        for (var i = 0; i < 20; i++)
                        {
                            Property.ForAll(Gen.Int32().ListOf(), xs => xs.Count < 5 || xs.Sum() < 100).Check(seed: i);
                        }
                        return 20;
                    };
                default:
                    throw new ArgumentException("unknown case " + name);
            }
        }

        public static (double ValuesPerSecond, double BytesPerValue) Measure(string name, double seconds)
        {
            var batch = Batcher(name);
            batch(); // warm up

            long values = 0;
            var bytesBefore = GC.GetTotalAllocatedBytes(true);
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < seconds)
            {
                values += batch();
            }
            sw.Stop();
            var bytes = GC.GetTotalAllocatedBytes(true) - bytesBefore;

            return (values / sw.Elapsed.TotalSeconds, (double)bytes / values);
        }

        // Writes one line for each case: name, values per second, bytes per value (tab separated).
        public static void MeasureAllTo(System.IO.TextWriter writer, double seconds)
        {
            foreach (var name in Names)
            {
                var (vps, bpv) = Measure(name, seconds);
                writer.WriteLine(string.Join("\t", name,
                    vps.ToString("R", CultureInfo.InvariantCulture),
                    bpv.ToString("R", CultureInfo.InvariantCulture)));
            }
        }
    }
}
