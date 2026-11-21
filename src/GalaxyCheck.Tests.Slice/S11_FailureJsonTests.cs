using System.Text.Json;
using FluentAssertions;
using GalaxyCheck.Runners;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S11_FailureJsonTests
{
    private static readonly string[] Frameworks = { "Xunit2", "Xunit3", "NUnit", "MSTest", "TUnit" };

    private static readonly Dictionary<string, Lazy<(IReadOnlyList<TrxResult> Results, CliResult Run)>> Runs =
        Frameworks.ToDictionary(
            name => name,
            name => new Lazy<(IReadOnlyList<TrxResult>, CliResult)>(() =>
                Trx.RunProject(Path.Combine(Repo.Samples, "S11", "Agent" + name), name.ToLowerInvariant())));

    private static (JsonDocument Json, string Message, CliResult Run) FrameworkFailure(string framework, string test)
    {
        var (results, run) = Runs[framework].Value;
        var result = results.FirstOrDefault(r => r.TestName.Contains(test));
        result.Should().NotBeNull($"the run holds a result for {test}. Output:\n{run.Output}");
        result!.Outcome.Should().Be("Failed", $"{test} must fail. Output:\n{run.Output}");
        return (S11Support.ExtractFailure(result.Message), result.Message, run);
    }

    [Fact]
    public void test_001_S11_R63_assert_message_holds_the_json_block()
    {
        var property = Property.ForAll(Gen.Int32(), x => x < 10);

        var act = () => property.Assert(iterations: 1000, seed: 1);

        var exception = act.Should().Throw<PropertyFailedException>().Which;
        using var json = S11Support.ExtractFailure(exception.Message);
        var root = json.RootElement;
        root.ValueKind.Should().Be(JsonValueKind.Object, exception.Message);
        root.GetProperty("counterexample").EnumerateArray().Select(e => e.GetString()).Should().Equal("10");
        root.GetProperty("csharp").EnumerateArray().Select(e => e.GetString()).Should().Equal("10");
        root.GetProperty("replay").GetString().Should().NotBeNullOrEmpty();
        foreach (var key in new[] { "seed", "size", "iterations", "shrinks" })
        {
            root.TryGetProperty(key, out var value).Should().BeTrue($"the key {key} is missing");
            value.ValueKind.Should().Be(JsonValueKind.Number, key);
        }
    }

    [Fact]
    public void test_001_S11_R63_assert_replay_string_reproduces_the_failure()
    {
        var property = Property.ForAll(Gen.Int32(), x => x < 10);
        var exception = Record.Exception(() => property.Assert(iterations: 1000, seed: 1));
        exception.Should().BeOfType<PropertyFailedException>();
        using var json = S11Support.ExtractFailure(exception!.Message);

        var result = property.Check(replay: json.RootElement.GetProperty("replay").GetString());

        result.Falsified.Should().BeTrue();
        result.Counterexample!.Value.Should().Be(10);
    }

    [Fact]
    public void test_001_S11_R63_xunit2_message_holds_test_and_rerun()
    {
        AssertFrameworkJson("Xunit2");
    }

    [Fact]
    public void test_001_S11_R63_xunit3_message_holds_test_and_rerun()
    {
        AssertFrameworkJson("Xunit3");
    }

    [Fact]
    public void test_001_S11_R63_nunit_message_holds_test_and_rerun()
    {
        AssertFrameworkJson("NUnit");
    }

    [Fact]
    public void test_001_S11_R63_mstest_message_holds_test_and_rerun()
    {
        AssertFrameworkJson("MSTest");
    }

    [Fact]
    public void test_001_S11_R63_tunit_message_holds_test_and_rerun()
    {
        AssertFrameworkJson("TUnit");
    }

    [Fact]
    public void test_001_S11_R63_xunit2_rerun_command_runs_only_that_test()
    {
        AssertRerun("Xunit2");
    }

    [Fact]
    public void test_001_S11_R63_xunit3_rerun_command_runs_only_that_test()
    {
        AssertRerun("Xunit3");
    }

    [Fact]
    public void test_001_S11_R63_nunit_rerun_command_runs_only_that_test()
    {
        AssertRerun("NUnit");
    }

    [Fact]
    public void test_001_S11_R63_mstest_rerun_command_runs_only_that_test()
    {
        AssertRerun("MSTest");
    }

    [Fact]
    public void test_001_S11_R63_tunit_rerun_command_runs_only_that_test()
    {
        AssertRerun("TUnit");
    }

    private static void AssertFrameworkJson(string framework)
    {
        var (json, message, run) = FrameworkFailure(framework, "IntBelow10");
        using (json)
        {
            var root = json.RootElement;
            var because = $"Message:\n{message}\nOutput:\n{run.Output}";
            root.GetProperty("counterexample").EnumerateArray().Select(e => e.GetString()).Should().Equal(new[] { "10" }, because);
            root.GetProperty("csharp").EnumerateArray().Select(e => e.GetString()).Should().Equal(new[] { "10" }, because);
            root.GetProperty("replay").GetString().Should().NotBeNullOrEmpty(because);
            foreach (var key in new[] { "seed", "size", "iterations", "shrinks" })
            {
                root.TryGetProperty(key, out _).Should().BeTrue($"the key {key} is missing. {because}");
            }

            root.GetProperty("test").GetString().Should().Contain("AgentSample.AgentTests.IntBelow10", because);
            root.GetProperty("rerun").GetString().Should().NotBeNullOrWhiteSpace(because);
        }
    }

    private static void AssertRerun(string framework)
    {
        var (json, message, run) = FrameworkFailure(framework, "IntBelow10");
        string rerun;
        using (json)
        {
            rerun = json.RootElement.GetProperty("rerun").GetString()!;
        }

        rerun.Should().NotBeNullOrWhiteSpace($"Message:\n{message}");
        var script = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", Guid.NewGuid().ToString("N") + ".sh");
        Directory.CreateDirectory(Path.GetDirectoryName(script)!);
        File.WriteAllText(script, "#!/usr/bin/env bash\n" + rerun + "\n");
        var folder = Path.Combine(Repo.Samples, "S11", "Agent" + framework);

        var rerunResult = Cli.Run("bash", script, folder);

        var because = $"Rerun command: {rerun}\nOutput of the rerun:\n{rerunResult.Output}\nOutput of the first run:\n{run.Output}";
        rerunResult.Output.Should().Contain("IntBelow10", because);
        rerunResult.Output.Should().NotContain("OtherBelow5", because);
    }
}
