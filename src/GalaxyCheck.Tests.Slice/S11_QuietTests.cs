using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S11_QuietTests
{
    private static readonly Lazy<(IReadOnlyList<S11TrxResult> Results, CliResult Run)> AgentRun = new(() =>
        S11Support.RunWithEnvironment(Path.Combine(Repo.Samples, "S11", "AgentQuiet"), "GALAXYCHECK_AGENT=1"));

    private static List<int> Run(int? seed)
    {
        var seen = new List<int>();
        var property = Property.ForAll(Gen.Int32(), x => { seen.Add(x); return true; });
        if (seed is null)
        {
            property.Check(iterations: 20);
        }
        else
        {
            property.Check(iterations: 20, seed: seed.Value);
        }

        return seen;
    }

    private static T WithEnvironment<T>(string name, string? value, Func<T> action)
    {
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            return action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
        }
    }

    [Fact]
    public void test_001_S11_R66_agent_mode_gives_the_values_of_seed_0_in_each_run()
    {
        var seed0 = Run(0);
        var seed7 = Run(7);
        seed0.Should().NotEqual(seed7, "the two seeds must give different values for this check to mean anything");

        var first = WithEnvironment("GALAXYCHECK_AGENT", "1", () => Run(null));
        var second = WithEnvironment("GALAXYCHECK_AGENT", "1", () => Run(null));

        first.Should().Equal(second);
        first.Should().Equal(seed0);
    }

    [Fact]
    public void test_001_S11_R66_agent_mode_passed_test_has_no_output()
    {
        var (results, run) = AgentRun.Value;
        var passed = results.FirstOrDefault(r => r.TestName.Contains("Passes"));
        passed.Should().NotBeNull($"Output:\n{run.Output}");
        passed!.Outcome.Should().Be("Passed", run.Output);
        passed.StdOut.Trim().Should().BeEmpty(run.Output);
        passed.Message.Trim().Should().BeEmpty(run.Output);
    }

    [Fact]
    public void test_001_S11_R66_agent_mode_failure_message_is_short_and_holds_the_block()
    {
        var (results, run) = AgentRun.Value;
        var failed = results.FirstOrDefault(r => r.TestName.Contains("IntBelow10"));
        failed.Should().NotBeNull($"Output:\n{run.Output}");
        failed!.Outcome.Should().Be("Failed", run.Output);
        failed.Message.Replace("\r\n", "\n").TrimEnd().Split('\n').Length.Should().BeLessThanOrEqualTo(40, failed.Message);
        using var json = S11Support.ExtractFailure(failed.Message);
        json.RootElement.GetProperty("counterexample").EnumerateArray().Select(e => e.GetString()).Should().Equal("10");
    }

    [Fact]
    public void test_001_S11_R66_seed_variable_gives_the_values_of_that_seed()
    {
        var seed7 = Run(7);
        var seed3 = Run(3);
        seed7.Should().NotEqual(seed3, "the two seeds must give different values for this check to mean anything");

        var fromVariable = WithEnvironment("GALAXYCHECK_SEED", "7", () => Run(null));
        var withOption = WithEnvironment("GALAXYCHECK_SEED", "7", () => Run(3));

        fromVariable.Should().Equal(seed7);
        withOption.Should().Equal(seed3);
    }
}
