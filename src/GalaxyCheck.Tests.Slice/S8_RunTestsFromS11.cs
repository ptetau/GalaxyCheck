using System.Text.RegularExpressions;
using FluentAssertions;
using GalaxyCheck.Runners;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>The results of one run of a TUnit sample project, and the lines that its test functions logged.</summary>
internal record S11Run(IReadOnlyList<TrxResult> Results, CliResult Run, IReadOnlyDictionary<string, string[]> Logs)
{
    public IReadOnlyList<TrxResult> Of(string method) =>
        Results.Where(r => Regex.IsMatch(r.TestName, @"(^|[.\s])" + Regex.Escape(method) + @"(\(|$)")).ToList();

    public string[] Log(string test) => Logs.TryGetValue(test, out var lines) ? lines : Array.Empty<string>();
}

internal static class S11Samples
{
    public static string Folder(string name) => Path.Combine(Repo.Samples, "S11", name);

    /// <summary>Copies a sample project next to the original, so the relative ProjectReference still works.</summary>
    public static string Copy(string name, params (string Token, string Value)[] replacements)
    {
        var source = Folder(name);
        var target = Path.Combine(Repo.Samples, "S11", "_run-" + name + "-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(source, target);
        foreach (var file in Directory.GetFiles(target, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (var (token, value) in replacements)
            {
                text = text.Replace(token, value);
            }

            File.WriteAllText(file, text);
        }

        return target;
    }

    public static void Delete(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    public static S11Run Run(string projectFolder, params (string Name, string Value)[] environment)
    {
        var logFolder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", "S11-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(logFolder);
        var names = environment.Select(e => e.Name).Append("GC_LOG_DIR").ToArray();
        var previous = names.ToDictionary(n => n, Environment.GetEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable("GC_LOG_DIR", logFolder);
            foreach (var (name, value) in environment)
            {
                Environment.SetEnvironmentVariable(name, value);
            }

            var (results, run) = Trx.RunProject(projectFolder, "tunit");
            var logs = Directory.GetFiles(logFolder, "*.log")
                .ToDictionary(
                    f => Path.GetFileNameWithoutExtension(f),
                    f => File.ReadAllLines(f).Where(l => l.Length > 0).ToArray());
            return new S11Run(results, run, logs);
        }
        finally
        {
            foreach (var (name, value) in previous)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(directory);
            if (name is "bin" or "obj")
            {
                continue;
            }

            CopyDirectory(directory, Path.Combine(target, name));
        }
    }
}

public class S11_TUnitRunTests
{
    private static readonly Lazy<S11Run> Forms = new(() => S11Samples.Run(S11Samples.Folder("Forms")));

    private static readonly Lazy<S11Run> Config = new(() => S11Samples.Run(S11Samples.Folder("Config")));

    /// <summary>The replay string and the counterexample of the property "the int is below 10", from the core library.</summary>
    private static (string Replay, string Counterexample, string Message) Reference()
    {
        var property = Property.ForAll(Gen.Int32(), x => x < 10);
        var result = property.Check(iterations: 100, seed: 1);
        result.Falsified.Should().BeTrue();
        var message = "";
        try
        {
            property.Assert(iterations: 100, seed: 1);
        }
        catch (PropertyFailedException exception)
        {
            message = exception.Message;
        }

        return (result.Counterexample!.Replay, result.Counterexample!.Value.ToString()!, message);
    }

    private static readonly Lazy<(S11Run Run, string Replay, string Counterexample)> Replayed = new(() =>
    {
        var reference = Reference();
        var folder = S11Samples.Copy("Replay", ("__REPLAY__", reference.Replay));
        try
        {
            return (S11Samples.Run(folder), reference.Replay, reference.Counterexample);
        }
        finally
        {
            S11Samples.Delete(folder);
        }
    });

    /// <summary>Three runs of a copy of the snapshot sample: the first stores the snapshot, the second changes nothing, the third changes the values.</summary>
    private static readonly Lazy<(S11Run Unchanged, S11Run Changed)> Snapshots = new(() =>
    {
        var folder = S11Samples.Copy("Snapshot");
        try
        {
            S11Samples.Run(folder, ("GC_VARIANT", "A"));
            var unchanged = S11Samples.Run(folder, ("GC_VARIANT", "A"));
            var changed = S11Samples.Run(folder, ("GC_VARIANT", "B"));
            return (unchanged, changed);
        }
        finally
        {
            S11Samples.Delete(folder);
        }
    });

    private static TrxResult Single(S11Run run, string method)
    {
        run.Results.Should().NotBeEmpty(because: run.Run.Output);
        var found = run.Of(method);
        found.Should().HaveCount(1, because: $"{method} is one test. Output: {run.Run.Output}");
        return found[0];
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");

    [Fact]
    public void test_001_S8_R23_tunit_holding_property_is_one_passed_test()
    {
        var result = Single(Forms.Value, "HoldingBool");
        result.Outcome.Should().BeEquivalentTo("Passed", because: result.Message + Forms.Value.Run.Output);
    }

    [Fact]
    public void test_001_S8_R23_tunit_falsified_property_is_one_failed_test_with_counterexample_and_replay()
    {
        var result = Single(Forms.Value, "IntBelow10");
        result.Outcome.Should().BeEquivalentTo("Failed", because: Forms.Value.Run.Output);

        var message = Normalize(result.Message);

        // The text of GalaxyCheck.Xunit: a "Counterexample:" part with the value, and the line
        // "Reproduction: [Replay("<replay>")]".
        message.Should().MatchRegex(@"Counterexample:[\s\S]*\b10\b", because: "the counterexample is 10. Message: " + message);
        var match = Regex.Match(message, @"\[Replay\(""([^""]+)""\)\]");
        match.Success.Should().BeTrue(because: "the message holds [Replay(\"...\")] with a replay string. Message: " + message);
        var replay = match.Groups[1].Value;
        replay.Should().NotBeNullOrWhiteSpace();

        // The replay string decodes: the same property with it fails in one test case with the counterexample 10.
        var replayed = Property.ForAll(Gen.Int32(), x => x < 10).Check(replay: replay);
        replayed.Falsified.Should().BeTrue();
        replayed.Iterations.Should().Be(1);
        replayed.Counterexample!.Value.Should().Be(10);
    }

    [Fact]
    public void test_001_S8_R24_tunit_each_holding_form_passes()
    {
        var run = Forms.Value;
        run.Results.Should().NotBeEmpty(because: run.Run.Output);
        var methods = new[]
        {
            "Void_Holds", "Bool_Holds", "Property_Holds", "Gen_Holds",
            "AsyncVoid_Holds", "AsyncBool_Holds", "AsyncProperty_Holds", "AsyncGen_Holds",
        };
        foreach (var method in methods)
        {
            var found = run.Of(method);
            found.Should().HaveCount(1, because: $"{method} is one test. Output: {run.Run.Output}");
            found[0].Outcome.Should().BeEquivalentTo("Passed", because: $"{method}: {found[0].Message}");
        }
    }

    [Fact]
    public void test_001_S8_R24_tunit_each_falsified_form_fails()
    {
        var run = Forms.Value;
        run.Results.Should().NotBeEmpty(because: run.Run.Output);
        var methods = new[]
        {
            "Void_Falsified", "Bool_Falsified", "Property_Falsified", "Gen_Falsified",
            "AsyncVoid_Falsified", "AsyncBool_Falsified", "AsyncProperty_Falsified", "AsyncGen_Falsified",
        };
        foreach (var method in methods)
        {
            var found = run.Of(method);
            found.Should().HaveCount(1, because: $"{method} is one test. Output: {run.Run.Output}");
            found[0].Outcome.Should().BeEquivalentTo("Failed", because: $"{method}: {found[0].Message}");
        }
    }

    [Fact]
    public void test_001_S8_R25_tunit_replay_runs_the_function_one_time_and_fails_with_the_same_counterexample()
    {
        var (run, replay, counterexample) = Replayed.Value;
        var result = Single(run, "ReplayedBelow10");
        result.Outcome.Should().BeEquivalentTo("Failed", because: run.Run.Output);
        run.Log("ReplayedBelow10").Should().Equal(new[] { counterexample }, because: "the replay runs one test case with the counterexample. Output: " + run.Run.Output);
        result.Message.Should().Contain(counterexample);
    }

    [Fact]
    public void test_001_S8_R25_tunit_member_gen_gives_five_in_each_test_case()
    {
        var run = Forms.Value;
        var result = Single(run, "MemberGenFive");
        result.Outcome.Should().BeEquivalentTo("Passed", because: result.Message + run.Run.Output);
        var lines = run.Log("MemberGenFive");
        lines.Should().NotBeEmpty(because: run.Run.Output);
        lines.Should().OnlyContain(l => l == "5");
    }

    [Fact]
    public void test_001_S8_R26_tunit_sample_test_fails_and_lists_the_sampled_values()
    {
        var run = Forms.Value;
        var result = Single(run, "SampleConstant");
        result.Outcome.Should().BeEquivalentTo("Failed", because: run.Run.Output);
        result.Message.Should().Contain("7777", because: "the message lists the sampled values");
    }

    [Fact]
    public void test_001_S8_R27_tunit_snapshot_with_no_change_passes()
    {
        var (unchanged, _) = Snapshots.Value;
        var result = Single(unchanged, "Values");
        result.Outcome.Should().BeEquivalentTo("Passed", because: result.Message + unchanged.Run.Output);
    }

    [Fact]
    public void test_001_S8_R27_tunit_snapshot_fails_when_the_generator_changes_its_values()
    {
        var (_, changed) = Snapshots.Value;
        var result = Single(changed, "Values");
        result.Outcome.Should().BeEquivalentTo("Failed", because: changed.Run.Output);
    }

    [Fact]
    public void test_001_S8_R28_tunit_two_arguments_rows_run_two_tests_with_a_generated_second_parameter()
    {
        var run = Forms.Value;
        run.Results.Should().NotBeEmpty(because: run.Run.Output);
        var rows = run.Of("ArgumentRows");
        rows.Should().HaveCount(2, because: run.Run.Output);
        rows.Should().OnlyContain(r => r.Outcome.Equals("Passed", StringComparison.OrdinalIgnoreCase), because: run.Run.Output);

        var pairs = run.Log("ArgumentRows").Select(l => l.Split(',')).ToList();
        pairs.Should().NotBeEmpty();
        pairs.Select(p => p[0]).Distinct().Should().BeEquivalentTo(new[] { "1", "2" });
        pairs.Select(p => p[1]).Distinct().Count().Should().BeGreaterThan(1, because: "the second parameter is generated");
    }

    [Fact]
    public void test_001_S8_R29_tunit_default_iterations_of_the_global_configuration_run_the_function_seven_times()
    {
        var run = Config.Value;
        var result = Single(run, "CountedRun");
        result.Outcome.Should().BeEquivalentTo("Passed", because: result.Message + run.Run.Output);
        run.Log("CountedRun").Should().HaveCount(7, because: run.Run.Output);
    }
}
