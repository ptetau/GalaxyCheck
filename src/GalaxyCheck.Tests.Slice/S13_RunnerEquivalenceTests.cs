using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>Runner equivalence (R75): sync and async forms, two runs in one process, and two runs in two processes.</summary>
public class S13_RunnerEquivalenceTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    private static IGen<IReadOnlyList<int>> Lists() => Gen.Int32().ListOf().Select(xs => (IReadOnlyList<int>)xs.ToList());

    private static bool Holds(IReadOnlyList<int> xs) => xs.Count(x => x > 10) < 2;

    [Fact]
    public async Task test_001_S13_R75_property_and_its_async_form_give_the_same_iterations_and_counterexample()
    {
        var problems = new List<string>();
        foreach (var seed in S13.Seeds)
        {
            var sync = Property.ForAll(Lists(), Holds).Check(iterations: 100, seed: seed);
            var async = await Property.ForAllAsync(Lists(), async xs =>
            {
                await Task.Yield();
                return Holds(xs);
            }).CheckAsync(iterations: 100, seed: seed);

            if (sync.Falsified != async.Falsified)
            {
                problems.Add($"seed {seed}: falsified {sync.Falsified} and async {async.Falsified}");
            }

            if (sync.Iterations != async.Iterations)
            {
                problems.Add($"seed {seed}: iterations {sync.Iterations} and async {async.Iterations}");
            }

            var syncValue = S13.Show(sync.Counterexample?.Value);
            var asyncValue = S13.Show(async.Counterexample?.Value);
            if (syncValue != asyncValue)
            {
                problems.Add($"seed {seed}: counterexample {syncValue} and async {asyncValue}");
            }
        }

        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    private static string RunText(int seed)
    {
        var result = S13.Within(Limit, () => Property.ForAll(Lists(), Holds).Check(iterations: 100, seed: seed));
        return $"falsified {result.Falsified}, iterations {result.Iterations}, shrinks {result.Shrinks}, counterexample {S13.Show(result.Counterexample?.Value)}, replay {result.Counterexample?.Replay}";
    }

    [Fact]
    public void test_001_S13_R75_two_runs_in_one_process_give_the_same_result()
    {
        var problems = new List<string>();
        foreach (var seed in S13.Seeds)
        {
            var first = RunText(seed);
            var second = RunText(seed);
            if (first != second)
            {
                problems.Add($"seed {seed}: \"{first}\" and then \"{second}\"");
            }
        }

        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    private static readonly Lazy<(CliResult First, CliResult Second)> TwoProcesses = new(() =>
    {
        var project = Path.Combine(Repo.Samples, "S13", "RunnerEquivalence");
        var first = Cli.Dotnet($"run --project \"{project}\"");
        var second = Cli.Dotnet($"run --no-build --project \"{project}\"");
        return (first, second);
    });

    private static string? Runs(string output)
    {
        var start = output.IndexOf("<s13-runs>", StringComparison.Ordinal);
        var end = output.IndexOf("</s13-runs>", StringComparison.Ordinal);
        return start < 0 || end < start ? null : output[start..end].Replace("\r\n", "\n");
    }

    [Fact]
    public void test_001_S13_R75_two_runs_in_two_processes_give_the_same_text()
    {
        var (first, second) = TwoProcesses.Value;
        var because = $"Output of the first process:\n{first.Output}\nOutput of the second process:\n{second.Output}";
        Assert.True(first.ExitCode == 0 && second.ExitCode == 0, "each process must exit with 0. " + because);
        var firstRuns = Runs(first.Output);
        var secondRuns = Runs(second.Output);
        Assert.True(firstRuns != null && secondRuns != null, "each process must print its runs. " + because);
        Assert.True(firstRuns!.Split('\n').Count(l => l.StartsWith("seed ")) == 20, "each process must print 20 runs. " + because);
        Assert.True(firstRuns == secondRuns, "the two processes must print the same text. " + because);
    }
}
