using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Running;

namespace GalaxyCheck.Benchmarks
{
    class Program
    {
        private const string BaselineCommit = "f127a67";
        private const double SecondsPerCase = 4;

        static int Main(string[] args)
        {
            var reportIndex = Array.IndexOf(args, "--report");
            if (reportIndex >= 0)
            {
                if (reportIndex + 1 >= args.Length)
                {
                    Console.Error.WriteLine("usage: --report <path>");
                    return 2;
                }
                WriteReport(args[reportIndex + 1]);
                return 0;
            }

            var gateIndex = Array.IndexOf(args, "--gate");
            if (gateIndex >= 0)
            {
                if (gateIndex + 1 >= args.Length)
                {
                    Console.Error.WriteLine("usage: --gate <path>");
                    return 2;
                }

                return PerformanceGate.Run(args[gateIndex + 1], Console.Out);
            }

            BenchmarkRunner.Run<RngBenchmark>();
            BenchmarkRunner.Run<ReflectedGenBenchmark>();
            BenchmarkRunner.Run<ListGenBenchmark>();
            BenchmarkRunner.Run<SelectManyGenBenchmark>();
            BenchmarkRunner.Run<SelectGenBenchmark>();
            return 0;
        }

        private static void WriteReport(string path)
        {
            var root = FindRepositoryRoot();

            // The baseline runs in a child process, because it has the same assembly name as the current library.
            var baseline = RunBaseline(root);

            var current = new Dictionary<string, (double Vps, double Bpv)>();
            foreach (var name in Cases.Names)
            {
                var (vps, bpv) = Cases.Measure(name, SecondsPerCase);
                current[name] = (vps, bpv);
            }

            using var stream = File.Create(path);
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteString("baseline", BaselineCommit);
                writer.WriteStartArray("cases");
                foreach (var name in Cases.Names)
                {
                    var b = baseline[name];
                    var c = current[name];
                    writer.WriteStartObject();
                    writer.WriteString("name", name);
                    WriteMeasure(writer, "baseline", b.Vps, b.Bpv);
                    WriteMeasure(writer, "current", c.Vps, c.Bpv);
                    writer.WriteNumber("throughputRatio", c.Vps / b.Vps);
                    writer.WriteNumber("allocationRatio", c.Bpv / b.Bpv);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }

        private static void WriteMeasure(Utf8JsonWriter writer, string property, double vps, double bpv)
        {
            writer.WriteStartObject(property);
            writer.WriteNumber("valuesPerSecond", vps);
            writer.WriteNumber("bytesPerValue", bpv);
            writer.WriteEndObject();
        }

        private static Dictionary<string, (double Vps, double Bpv)> RunBaseline(string root)
        {
            var work = Path.Combine(Path.GetTempPath(), "galaxycheck-baseline-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            try
            {
                var source = Path.Combine(work, "source");
                Directory.CreateDirectory(source);
                var archive = Path.Combine(work, "baseline.tar");
                Run("git", new[] { "-C", root, "archive", "--format=tar", "-o", archive, BaselineCommit, "src/GalaxyCheck" }, work);
                Run("tar", new[] { "-xf", archive, "-C", source }, work);

                var template = Path.Combine(root, "src", "GalaxyCheck.Benchmarks");
                var runner = Path.Combine(work, "runner");
                Directory.CreateDirectory(runner);
                File.WriteAllText(Path.Combine(runner, "Runner.csproj"),
                    File.ReadAllText(Path.Combine(template, "baseline-runner", "Runner.csproj.txt"))
                        .Replace("CASES_PATH", Path.Combine(template, "Cases.cs"))
                        .Replace("BASELINE_PATH", Path.Combine(source, "src", "GalaxyCheck", "GalaxyCheck.csproj")));
                File.Copy(Path.Combine(template, "baseline-runner", "Program.cs.txt"), Path.Combine(runner, "Program.cs"));
                File.Copy(Path.Combine(root, "nuget.config"), Path.Combine(runner, "nuget.config"), true);

                var output = Run("dotnet", new[] { "run", "-c", "Release", "--project", runner, "--", SecondsPerCase.ToString(CultureInfo.InvariantCulture) }, work);

                var result = new Dictionary<string, (double, double)>();
                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.TrimEnd('\r').Split('\t');
                    if (parts.Length == 3 && Array.IndexOf(Cases.Names, parts[0]) >= 0)
                    {
                        result[parts[0]] = (
                            double.Parse(parts[1], CultureInfo.InvariantCulture),
                            double.Parse(parts[2], CultureInfo.InvariantCulture));
                    }
                }
                foreach (var name in Cases.Names)
                {
                    if (!result.ContainsKey(name))
                    {
                        throw new InvalidOperationException("The baseline runner gave no result for '" + name + "'.");
                    }
                }
                return result;
            }
            finally
            {
                try { Directory.Delete(work, true); } catch (IOException) { }
            }
        }

        private static string Run(string fileName, string[] arguments, string workingDirectory)
        {
            var info = new ProcessStartInfo(fileName)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var argument in arguments) info.ArgumentList.Add(argument);

            using var process = Process.Start(info)!;
            var stderr = new StringBuilder();
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
            process.BeginErrorReadLine();
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"'{fileName}' failed with exit code {process.ExitCode}.\n{stdout}\n{stderr}");
            }
            return stdout;
        }

        private static string FindRepositoryRoot()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "GalaxyCheck.sln"))
                        && Directory.Exists(Path.Combine(dir.FullName, "src", "GalaxyCheck.Benchmarks", "baseline-runner")))
                    {
                        return dir.FullName;
                    }
                    dir = dir.Parent;
                }
            }
            throw new InvalidOperationException("Cannot find the repository root.");
        }
    }
}
