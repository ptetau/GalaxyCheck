using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace GalaxyCheck.Benchmarks
{
    /// <summary>
    /// One measured case of the report: the ratio of the current library to the baseline, for throughput and for
    /// allocation.
    /// </summary>
    public record BenchmarkCase(string Name, double ThroughputRatio, double AllocationRatio);

    /// <summary>
    /// The rule that compares each case of a report with the performance targets.
    /// </summary>
    public static class PerformanceGate
    {
        public const double MinThroughputRatio = 100;
        public const double MaxAllocationRatio = 0.01;

        /// <summary>
        /// Reads the cases of a report. The report is read as plain JSON data.
        /// </summary>
        public static IReadOnlyList<BenchmarkCase> ReadReport(string path)
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);

            var cases = new List<BenchmarkCase>();
            foreach (var element in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                cases.Add(new BenchmarkCase(
                    element.GetProperty("name").GetString() ?? "",
                    element.GetProperty("throughputRatio").GetDouble(),
                    element.GetProperty("allocationRatio").GetDouble()));
            }

            return cases;
        }

        /// <summary>
        /// Returns one line for each case that misses a target. No line means that the gate passes.
        /// </summary>
        public static IReadOnlyList<string> Failures(IEnumerable<BenchmarkCase> cases)
        {
            var failures = new List<string>();
            foreach (var benchmarkCase in cases)
            {
                if (!(benchmarkCase.ThroughputRatio >= MinThroughputRatio))
                {
                    failures.Add($"Case \"{benchmarkCase.Name}\": throughput ratio {Format(benchmarkCase.ThroughputRatio)} is below the target of {Format(MinThroughputRatio)}.");
                }

                if (!(benchmarkCase.AllocationRatio <= MaxAllocationRatio))
                {
                    failures.Add($"Case \"{benchmarkCase.Name}\": allocation ratio {Format(benchmarkCase.AllocationRatio)} is above the target of {Format(MaxAllocationRatio)}.");
                }
            }

            return failures;
        }

        /// <summary>
        /// Runs the gate on a report file and returns the exit code: 0 when each case meets the targets, 1 when a
        /// case misses a target, and 2 when the report cannot be read.
        /// </summary>
        public static int Run(string path, TextWriter output)
        {
            IReadOnlyList<BenchmarkCase> cases;
            try
            {
                cases = ReadReport(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException ||
                                       ex is KeyNotFoundException || ex is InvalidOperationException)
            {
                output.WriteLine($"The report '{path}' cannot be read: {ex.Message}");
                return 2;
            }

            var failures = Failures(cases);
            foreach (var failure in failures)
            {
                output.WriteLine(failure);
            }

            if (failures.Count == 0)
            {
                output.WriteLine($"Each of the {cases.Count} cases meets the targets.");
                return 0;
            }

            return 1;
        }

        private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
