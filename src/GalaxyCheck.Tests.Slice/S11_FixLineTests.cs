using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public abstract class S11_AbstractThing
{
    public abstract int Value { get; }
}

public class S11_FixLineTests
{
    private static readonly Lazy<(IReadOnlyList<S11TrxResult> Results, CliResult Run)> QuietRun = new(() =>
        S11Support.RunWithEnvironment(Path.Combine(Repo.Samples, "S11", "AgentQuiet"), "DOTNET_NOLOGO=1"));

    private static void AssertFixLine(Action act)
    {
        var exception = Record.Exception(act);
        exception.Should().BeOfType<GalaxyCheck.Exceptions.GenErrorException>("the run must give a generator error");
        S11Support.LastLine(exception!.Message).Should().StartWith("Fix: ", exception.Message);
    }

    private static void AssertFailedTestFixLine(string test)
    {
        var (results, run) = QuietRun.Value;
        var result = results.FirstOrDefault(r => r.TestName.Contains(test));
        result.Should().NotBeNull($"the run holds a result for {test}. Output:\n{run.Output}");
        result!.Outcome.Should().Be("Failed", run.Output);
        S11Support.LastLine(result.Message).Should().StartWith("Fix: ", $"Message:\n{result.Message}\nOutput:\n{run.Output}");
    }

    [Fact]
    public void test_001_S11_R64_min_above_max_error_ends_with_fix_line()
    {
        AssertFixLine(() => Gen.UInt16().GreaterThanEqual(5).LessThanEqual(4).Sample(iterations: 1, seed: 0));
    }

    [Fact]
    public void test_001_S11_R64_negative_list_count_error_ends_with_fix_line()
    {
        AssertFixLine(() => Gen.Int32().ListOf().WithCount(-1).Sample(iterations: 1, seed: 0));
    }

    [Fact]
    public void test_001_S11_R64_unsupported_pattern_error_ends_with_fix_line()
    {
        AssertFixLine(() => Gen.StringMatching("(?<=a)b").Sample(iterations: 1, seed: 0));
    }

    [Fact]
    public void test_001_S11_R64_abstract_class_without_concrete_type_error_ends_with_fix_line()
    {
        AssertFixLine(() => Gen.Create<S11_AbstractThing>().Sample(iterations: 1, seed: 0));
    }

    [Fact]
    public void test_001_S11_R64_bad_replay_attribute_error_ends_with_fix_line()
    {
        AssertFailedTestFixLine("BadReplay");
    }

    [Fact]
    public void test_001_S11_R64_bad_member_gen_attribute_error_ends_with_fix_line()
    {
        AssertFailedTestFixLine("BadMemberGen");
    }

    [Fact]
    public void test_001_S11_R64_bad_example_attribute_error_ends_with_fix_line()
    {
        AssertFailedTestFixLine("BadExample");
    }
}
