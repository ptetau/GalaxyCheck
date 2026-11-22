using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public record S10Run(IReadOnlyList<TrxResult> Results, CliResult Cli, string RowsFile, string CountFile);

public class S10_RunTests
{
    /// <summary>Runs a sample test project of this slice. The extra arguments and the environment are for this run only.</summary>
    private static S10Run Execute(string name, string extraArgs = "", string? generatedSource = null)
    {
        var folder = Path.Combine(Repo.Samples, "S10", name);
        foreach (var memory in Directory.GetDirectories(folder, ".galaxycheck", SearchOption.AllDirectories))
        {
            Directory.Delete(memory, recursive: true);
        }

        var work = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", "s10-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var rows = Path.Combine(work, "rows.txt");
        var count = Path.Combine(work, "count.txt");
        var args = extraArgs;
        if (generatedSource != null)
        {
            var source = Path.Combine(work, "Generated.cs");
            File.WriteAllText(source, generatedSource);
            args += $" -p:ExtraCompile=\"{source}\"";
        }

        Environment.SetEnvironmentVariable("S10_ROWS_FILE", rows);
        Environment.SetEnvironmentVariable("S10_COUNT_FILE", count);
        try
        {
            var resultsFolder = Path.Combine(work, "results");
            Directory.CreateDirectory(resultsFolder);
            var run = Cli.Dotnet($"test \"{folder}\" --logger \"trx;LogFileName=results.trx\" --results-directory \"{resultsFolder}\" {args}");
            var results = Directory.GetFiles(resultsFolder, "*.trx", SearchOption.AllDirectories).SelectMany(Trx.Read).ToList();
            return new S10Run(results, run, rows, count);
        }
        finally
        {
            Environment.SetEnvironmentVariable("S10_ROWS_FILE", null);
            Environment.SetEnvironmentVariable("S10_COUNT_FILE", null);
        }
    }

    public static readonly Lazy<S10Run> Main = new(() => Execute("Main"));
    private static readonly Lazy<S10Run> Reference = new(() => Execute("Reference"));

    /// <summary>The results of the test method with this name.</summary>
    public static List<TrxResult> Find(S10Run run, string method) =>
        run.Results.Where(r => Regex.IsMatch(r.TestName, @"(^|[.\s])" + Regex.Escape(method) + @"(\(|\s|$)")).ToList();

    private static TrxResult One(S10Run run, string method)
    {
        var found = Find(run, method);
        found.Should().ContainSingle($"the run must hold one result for {method}. Output: {run.Cli.Output}");
        return found[0];
    }

    private static List<string> Lines(string path) =>
        File.Exists(path) ? File.ReadAllLines(path).Where(l => l.Length > 0).ToList() : new List<string>();

    // R23

    [Fact]
    public void test_001_S8_R23_property_that_holds_is_one_passed_test()
    {
        var run = Main.Value;
        var result = One(run, "TheIntHolds");
        result.Outcome.Should().Be("Passed", result.Message + " Output: " + run.Cli.Output);
    }

    [Fact]
    public void test_001_S8_R23_falsified_property_is_one_failed_test_with_the_text_of_xunit_v2()
    {
        var v3 = Main.Value;
        var v2 = Reference.Value;
        var failed3 = One(v3, "TheIntIsBelow10");
        var failed2 = One(v2, "TheIntIsBelow10");

        failed2.Outcome.Should().Be("Failed", "the reference must fail. Output: " + v2.Cli.Output);
        failed2.Message.Should().Contain("10", "the reference message must hold the counterexample");
        failed3.Outcome.Should().Be("Failed", failed3.Message + " Output: " + v3.Cli.Output);
        failed3.Message.Should().Contain("10", "the counterexample is 10. Output: " + v3.Cli.Output);
        failed3.Message.Trim().Should().Be(failed2.Message.Trim(), "the message must be the text of GalaxyCheck.Xunit, with the replay string. Output: " + v3.Cli.Output);
    }

    // R24

    [Fact]
    public void test_001_S8_R24_each_form_that_holds_passes_and_each_falsified_form_fails()
    {
        var run = Main.Value;
        var problems = new List<string>();
        foreach (var form in new[] { "FormVoid", "FormBool", "FormProperty", "FormGen", "FormTask", "FormTaskBool", "FormAsyncProperty", "FormAsyncGen" })
        {
            var holds = Find(run, form + "Holds");
            var falsified = Find(run, form + "Falsified");
            if (holds.Count != 1 || holds[0].Outcome != "Passed") problems.Add($"{form}Holds must be one passed test, found: " + string.Join(",", holds.Select(h => h.Outcome)));
            if (falsified.Count != 1 || falsified[0].Outcome != "Failed") problems.Add($"{form}Falsified must be one failed test, found: " + string.Join(",", falsified.Select(h => h.Outcome)));
        }

        problems.Should().BeEmpty("Output: " + run.Cli.Output);
    }

    // R25

    private static readonly Lazy<S10Run> ReplayRun = new(() =>
    {
        // The replay string of the failing property, from the core library. The test function runs once and fails.
        var replay = Property.ForAll(Gen.Create<int>(), x => x < 10).Check(seed: 42).Counterexample!.Replay;
        var source = $$"""
            using GalaxyCheck;
            namespace S10Dynamic;
            public class ReplayTests
            {
                [Property]
                [Replay("{{replay}}")]
                public bool TheIntIsBelow10Replayed(int x) { Counter.Hit(); return x < 10; }
            }
            """;
        return Execute("Dynamic", generatedSource: source);
    });

    [Fact]
    public void test_001_S8_R25_replay_runs_the_function_one_time_and_fails_with_the_same_counterexample()
    {
        var run = ReplayRun.Value;
        var result = One(run, "TheIntIsBelow10Replayed");
        result.Outcome.Should().Be("Failed", result.Message + " Output: " + run.Cli.Output);
        result.Message.Should().Contain("10", "the counterexample is 10. Output: " + run.Cli.Output);
        Lines(run.CountFile).Should().HaveCount(1, "the test function must run one time. Output: " + run.Cli.Output);
    }

    [Fact]
    public void test_001_S8_R25_invalid_replay_fails_with_the_error_text_of_xunit_v2()
    {
        var v3 = Main.Value;
        var v2 = Reference.Value;
        var failed2 = One(v2, "ReplayZero");
        var failed3 = One(v3, "ReplayZero");
        failed2.Outcome.Should().Be("Failed", "the reference must fail with the replay error. Output: " + v2.Cli.Output);
        failed3.Outcome.Should().Be("Failed", failed3.Message + " Output: " + v3.Cli.Output);
        failed3.Message.Trim().Should().Be(failed2.Message.Trim(), "the error text must be the text of GalaxyCheck.Xunit. Output: " + v3.Cli.Output);
    }

    [Fact]
    public void test_001_S8_R25_member_gen_gives_5_in_each_test_case()
    {
        var run = Main.Value;
        var result = One(run, "MemberGenFive");
        result.Outcome.Should().Be("Passed", result.Message + " Output: " + run.Cli.Output);
        var values = Lines(run.RowsFile).Where(l => l.StartsWith("five:", StringComparison.Ordinal)).ToList();
        values.Should().NotBeEmpty("the test function must run. Output: " + run.Cli.Output);
        values.Should().OnlyContain(l => l == "five:5", "the parameter is 5 in each test case. Output: " + run.Cli.Output);
    }

    // R26

    [Fact]
    public void test_001_S8_R26_sample_test_fails_and_lists_the_sampled_values()
    {
        var run = Main.Value;
        var result = One(run, "SampleFive");
        result.Outcome.Should().Be("Failed", "a sample test always fails. Output: " + run.Cli.Output);
        result.Message.Should().Contain("5", "the message lists the sampled values. Output: " + run.Cli.Output);
        result.Message.Should().NotContain("not implemented", "the message must list values, not the stub error");
    }

    // R27

    private static string SnapshotSource(string className, string generator) => $$"""
        using GalaxyCheck;
        using GalaxyCheck.Gens;
        namespace S10Dynamic;
        public class {{className}}
        {
            [GenSnapshot]
            public IGen<int> Values() => {{generator}};
        }
        """;

    private static readonly Lazy<(S10Run Stored, S10Run Same, S10Run Changed)> Snapshots = new(() =>
    {
        // A new class name gives a new snapshot, so that no snapshot of an earlier run is read.
        var className = "Snap" + Guid.NewGuid().ToString("N");
        var first = Execute("Dynamic", generatedSource: SnapshotSource(className, "Gen.Int32().Between(0, 100)"));
        var same = Execute("Dynamic", generatedSource: SnapshotSource(className, "Gen.Int32().Between(0, 100)"));
        var changed = Execute("Dynamic", generatedSource: SnapshotSource(className, "Gen.Int32().Between(1000, 2000)"));
        return (first, same, changed);
    });

    [Fact]
    public void test_001_S8_R27_snapshot_test_passes_with_no_change_and_fails_when_the_values_change()
    {
        var (_, same, changed) = Snapshots.Value;
        var passed = Find(same, "Values");
        passed.Should().ContainSingle("Output: " + same.Cli.Output);
        passed[0].Outcome.Should().Be("Passed", "with a stored snapshot and no change the test passes. " + passed[0].Message + " Output: " + same.Cli.Output);

        var failed = Find(changed, "Values");
        failed.Should().ContainSingle("Output: " + changed.Cli.Output);
        failed[0].Outcome.Should().Be("Failed", "when the generator changes its values the test fails. Output: " + changed.Cli.Output);
    }

    // R28

    [Fact]
    public void test_001_S8_R28_inline_data_rows_give_2_tests_with_the_first_parameter_from_the_row_and_the_second_generated()
    {
        var run = Main.Value;
        var results = Find(run, "RowsData");
        results.Should().HaveCount(2, "one test for each row. Output: " + run.Cli.Output);
        results.Should().OnlyContain(r => r.Outcome == "Passed", "Output: " + run.Cli.Output);

        var rows = Lines(run.RowsFile)
            .Where(l => l.StartsWith("rows:", StringComparison.Ordinal))
            .Select(l => l.Substring(5).Split(','))
            .ToList();
        rows.Should().NotBeEmpty("the test function must run. Output: " + run.Cli.Output);
        rows.Select(r => r[0]).Distinct().Should().BeEquivalentTo(new[] { "1", "2" }, "the first parameter is 1 or 2. Output: " + run.Cli.Output);
        rows.GroupBy(r => r[0]).Should().OnlyContain(g => g.Select(r => r[1]).Distinct().Count() > 1, "the second parameter is generated. Output: " + run.Cli.Output);
    }

    // R29

    private static readonly Lazy<S10Run> ConfigRun = new(() =>
    {
        const string source = """
            using GalaxyCheck;
            using GalaxyCheck.Configuration;
            namespace S10Dynamic;
            public class SevenIterations : IConfigureGlobal
            {
                public void Configure(IGlobalConfiguration instance) => instance.Properties.DefaultIterations = 7;
            }
            public class CountTests
            {
                [Property]
                public void Counted(int x) => Counter.Hit();
            }
            """;
        return Execute("Dynamic", generatedSource: source);
    });

    [Fact]
    public void test_001_S8_R29_global_default_iterations_of_7_run_the_function_7_times()
    {
        var run = ConfigRun.Value;
        var result = One(run, "Counted");
        result.Outcome.Should().Be("Passed", result.Message + " Output: " + run.Cli.Output);
        Lines(run.CountFile).Should().HaveCount(7, "the function must run 7 times. Output: " + run.Cli.Output);
    }
}
