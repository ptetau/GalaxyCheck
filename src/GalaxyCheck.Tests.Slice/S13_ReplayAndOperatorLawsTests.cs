using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>The replay laws (R72), the operator laws (R73), and the exhaustive laws (R76).</summary>
public class S13_ReplayAndOperatorLawsTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    // An exception that an engine bug throws, not an error that the library reports.
    private static bool IsCrash(Exception e) =>
        e is NullReferenceException or IndexOutOfRangeException or ArgumentOutOfRangeException or InvalidCastException
            or KeyNotFoundException or OverflowException or FormatException or StackOverflowException or TimeoutException;

    private static Property<int> Failing() => Property.ForAll(Gen.Int32().Between(0, 1000), x => x < 10);

    // ---- R72: replay laws ----

    [Fact]
    public void test_001_S13_R72_replay_runs_one_test_case_and_makes_no_shrink()
    {
        var problems = new List<string>();
        foreach (var seed in S13.Seeds)
        {
            var property = Property.ForAll(Gen.Int32().ListOf(), xs => xs.Count < 3);
            var first = S13.Within(Limit, () => property.Check(iterations: 100, seed: seed));
            if (!first.Falsified || first.Counterexample is null)
            {
                problems.Add($"seed {seed}: the property did not fail");
                continue;
            }

            var calls = 0;
            var replayProperty = Property.ForAll(Gen.Int32().ListOf(), xs =>
            {
                Interlocked.Increment(ref calls);
                return xs.Count < 3;
            });
            var replayed = S13.Within(Limit, () => replayProperty.Check(replay: first.Counterexample.Replay));
            if (!replayed.Falsified || replayed.Counterexample is null)
            {
                problems.Add($"seed {seed}: the replay did not fail");
                continue;
            }

            if (replayed.Iterations != 1) problems.Add($"seed {seed}: the replay ran {replayed.Iterations} test cases");
            if (calls != 1) problems.Add($"seed {seed}: the replay called the test function {calls} times");
            if (replayed.Shrinks != 0) problems.Add($"seed {seed}: the replay made {replayed.Shrinks} shrinks");
            if (S13.Show(replayed.Counterexample.Value) != S13.Show(first.Counterexample.Value))
            {
                problems.Add($"seed {seed}: the replay gave {S13.Show(replayed.Counterexample.Value)}, not {S13.Show(first.Counterexample.Value)}");
            }
        }

        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    /// <summary>Runs a replay that must give an error, and returns a problem or null.</summary>
    private static string? ExpectReplayError(Func<object> run, string what, bool mustSayInvalid)
    {
        try
        {
            var result = S13.Within(Limit, run);
            return $"{what}: the run gave a result ({S13.Show(result)}) and no error";
        }
        catch (Exception e) when (IsCrash(e))
        {
            return $"{what}: the run crashed or hung: {e.GetType().Name}: {e.Message}";
        }
        catch (Exception e)
        {
            var message = e.Message.ToLowerInvariant();
            if (!message.Contains("replay"))
            {
                return $"{what}: the error does not name the replay: {e.GetType().Name}: {e.Message}";
            }

            if (mustSayInvalid && !message.Contains("valid"))
            {
                return $"{what}: the error does not say that the replay is no longer valid: {e.Message}";
            }

            return null;
        }
    }

    [Fact]
    public void test_001_S13_R72_replay_of_a_property_with_another_generator_is_no_longer_valid()
    {
        var problems = new List<string>();
        var used = 0;
        foreach (var seed in S13.Seeds)
        {
            var first = S13.Within(Limit, () => Failing().Check(iterations: 100, seed: seed));
            if (!first.Falsified || first.Counterexample is null || first.Shrinks < 2)
            {
                // A replay with fewer than two shrinks can be a valid path in the other generator too.
                continue;
            }

            used++;
            var replay = first.Counterexample.Replay;
            var other = Property.ForAll(Gen.Boolean(), b => false);
            var problem = ExpectReplayError(() => other.Check(replay: replay), $"seed {seed}, replay {replay}", mustSayInvalid: true);
            if (problem != null) problems.Add(problem);
        }

        Assert.True(used >= 10, $"the setup needs at least 10 seeds whose counterexample made two shrinks; {used} did");
        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    [Fact]
    public void test_001_S13_R72_replay_that_is_cut_short_gives_an_error_and_not_a_crash()
    {
        var problems = new List<string>();
        foreach (var seed in new[] { 0, 1, 2, 3, 4 })
        {
            var first = S13.Within(Limit, () => Failing().Check(iterations: 100, seed: seed));
            Assert.True(first.Falsified && first.Counterexample != null, $"seed {seed}: the property must fail");
            var replay = first.Counterexample!.Replay;
            foreach (var cut in new[] { replay[..(replay.Length / 2)], replay[..^1] }.Where(c => c.Length > 0).Distinct())
            {
                var problem = ExpectReplayError(() => Failing().Check(replay: cut), $"seed {seed}, replay \"{cut}\" cut from \"{replay}\"", mustSayInvalid: false);
                if (problem != null) problems.Add(problem);
            }
        }

        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    [Fact]
    public void test_001_S13_R72_replay_that_is_changed_gives_an_error_and_not_a_crash()
    {
        var problems = new List<string>();
        foreach (var seed in new[] { 0, 1, 2, 3, 4 })
        {
            var first = S13.Within(Limit, () => Failing().Check(iterations: 100, seed: seed));
            Assert.True(first.Falsified && first.Counterexample != null, $"seed {seed}: the property must fail");
            var replay = first.Counterexample!.Replay;
            var changed = new List<string> { replay + "!", "#" + replay, "not a replay" };
            var middle = replay.Length / 2;
            changed.Add(replay[..middle] + (replay[middle] == '~' ? "!" : "~") + replay[(middle + 1)..]);
            foreach (var bad in changed)
            {
                var problem = ExpectReplayError(() => Failing().Check(replay: bad), $"seed {seed}, replay \"{bad}\" changed from \"{replay}\"", mustSayInvalid: false);
                if (problem != null) problems.Add(problem);
            }
        }

        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    // ---- R73: operator laws ----

    /// <summary>Two generators must give the same values and the same shrunk counterexample for each seed.</summary>
    private static void SameRuns<T>(string law, IGen<T> left, IGen<T> right, Func<T, bool> holds)
    {
        var problems = new List<string>();
        foreach (var seed in S13.Seeds)
        {
            var leftValues = S13.ShowAll(left.Sample(iterations: 30, seed: seed));
            var rightValues = S13.ShowAll(right.Sample(iterations: 30, seed: seed));
            if (!leftValues.SequenceEqual(rightValues))
            {
                problems.Add($"seed {seed}: the values differ: [{string.Join(", ", leftValues)}] and [{string.Join(", ", rightValues)}]");
            }

            var leftResult = S13.Within(Limit, () => Property.ForAll(left, holds).Check(iterations: 100, seed: seed, exhaustive: false));
            var rightResult = S13.Within(Limit, () => Property.ForAll(right, holds).Check(iterations: 100, seed: seed, exhaustive: false));
            if (!leftResult.Falsified || !rightResult.Falsified)
            {
                problems.Add($"seed {seed}: the property must fail on both sides (left {leftResult.Falsified}, right {rightResult.Falsified})");
                continue;
            }

            var leftShrunk = S13.Show(leftResult.Counterexample!.Value);
            var rightShrunk = S13.Show(rightResult.Counterexample!.Value);
            if (leftShrunk != rightShrunk)
            {
                problems.Add($"seed {seed}: the shrunk counterexamples differ: {leftShrunk} and {rightShrunk}");
            }
        }

        Assert.True(problems.Count == 0, $"{law}: {S13.Problems(problems)}");
    }

    private static IGen<int> Base() => Gen.Int32().Between(0, 1000);

    private static IGen<int> K(int x) => Gen.Int32().Between(x, x + 1000);

    private static IGen<(int, int)> H(int y) => Gen.Int32().Between(0, y).Select(z => (y, z));

    [Fact]
    public void test_001_S13_R73_select_keeps_the_identity_law()
    {
        SameRuns("identity", Base().Select(x => x), Base(), x => x < 300);
    }

    [Fact]
    public void test_001_S13_R73_select_keeps_the_composition_law()
    {
        Func<int, int> f = x => x * 2;
        Func<int, int> g = x => x + 7;
        SameRuns("composition", Base().Select(f).Select(g), Base().Select(x => g(f(x))), x => x < 300);
    }

    [Fact]
    public void test_001_S13_R73_select_many_keeps_the_left_identity_law()
    {
        SameRuns("left identity", Gen.Constant(5).SelectMany(K), K(5), x => x < 300);
    }

    [Fact]
    public void test_001_S13_R73_select_many_keeps_the_right_identity_law()
    {
        SameRuns("right identity", Base().SelectMany(x => Gen.Constant(x)), Base(), x => x < 300);
    }

    [Fact]
    public void test_001_S13_R73_select_many_keeps_the_associativity_law()
    {
        SameRuns(
            "associativity",
            Base().SelectMany(K).SelectMany(H),
            Base().SelectMany(x => K(x).SelectMany(H)),
            t => t.Item1 < 600 || t.Item2 < 100);
    }

    // ---- R76: exhaustive laws ----

    private static (List<string> Calls, bool Exhaustive, int Iterations) RunExhaustive<T>(IGen<T> gen, int iterations)
    {
        var recorder = new S13Recorder<T>(_ => true);
        var result = S13.Within(Limit, () => Property.ForAll(gen, (Func<T, bool>)recorder.Invoke).Check(iterations: iterations, seed: 0));
        return (recorder.Calls.Select(c => c.Shown).ToList(), result.Exhaustive, result.Iterations);
    }

    private static void AssertEachOnce(List<string> calls, IEnumerable<string> domain, bool exhaustive)
    {
        var expected = domain.OrderBy(v => v, StringComparer.Ordinal).ToList();
        var actual = calls.OrderBy(v => v, StringComparer.Ordinal).ToList();
        Assert.True(exhaustive, "the run must be exhaustive");
        Assert.True(expected.SequenceEqual(actual), $"each value must run one time. Expected [{string.Join(", ", expected)}], ran [{string.Join(", ", actual)}]");
    }

    [Fact]
    public void test_001_S13_R76_exhaustive_run_through_select_gives_each_value_once()
    {
        var (calls, exhaustive, _) = RunExhaustive(Gen.Int32().Between(0, 11).Select(x => x * 3), 100);
        AssertEachOnce(calls, Enumerable.Range(0, 12).Select(x => S13.Show(x * 3)), exhaustive);
    }

    [Fact]
    public void test_001_S13_R76_exhaustive_run_through_a_filter_gives_each_kept_value_once()
    {
        var (calls, exhaustive, _) = RunExhaustive(Gen.Int32().Between(0, 11).Where(x => x % 2 == 0), 100);
        AssertEachOnce(calls, new[] { 0, 2, 4, 6, 8, 10 }.Select(x => S13.Show(x)), exhaustive);
    }

    [Fact]
    public void test_001_S13_R76_exhaustive_run_through_select_many_gives_each_value_once()
    {
        var gen =
            from a in Gen.Int32().Between(0, 2)
            from b in Gen.Int32().Between(0, 3)
            select a * 4 + b;
        var (calls, exhaustive, _) = RunExhaustive(gen, 100);
        AssertEachOnce(calls, Enumerable.Range(0, 12).Select(x => S13.Show(x)), exhaustive);
    }

    [Fact]
    public void test_001_S13_R76_domain_that_is_too_large_runs_the_normal_random_test_cases()
    {
        var (calls, exhaustive, iterations) = RunExhaustive(Gen.Int32().Between(0, 999), 100);
        Assert.False(exhaustive, "a domain of 1,000 values with 100 iterations must not be exhaustive");
        Assert.Equal(100, iterations);
        Assert.Equal(100, calls.Count);
    }
}
