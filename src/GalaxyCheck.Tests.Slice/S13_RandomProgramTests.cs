using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>
/// Random programs (R74): random compositions of the generators and operators of the library (select, filter,
/// bind, list, tuple, choice), built from System.Random with a fixed seed, so that the test is deterministic.
/// </summary>
public class S13_RandomProgramTests
{
    private const int Programs = 200;
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private sealed record Program(IGen<int> Gen, string Text);

    private static Program Build(Random random, int depth)
    {
        var kind = depth == 0 ? 0 : random.Next(9);
        switch (kind)
        {
            case 0:
                switch (random.Next(3))
                {
                    case 0:
                        var low = random.Next(-100, 100);
                        var width = random.Next(20, 200);
                        return new Program(Gen.Int32().Between(low, low + width), $"int({low}..{low + width})");
                    case 1:
                        return new Program(Gen.Boolean().Select(b => b ? 1 : 0), "bool");
                    default:
                        var values = Enumerable.Range(0, 5).Select(_ => random.Next(-50, 50)).ToArray();
                        return new Program(Gen.Element(values), $"element({string.Join(",", values)})");
                }

            case 1:
            {
                var child = Build(random, depth - 1);
                var k = random.Next(1, 5);
                var c = random.Next(-10, 10);
                return new Program(child.Gen.Select(x => unchecked(x * k + c)), $"select({child.Text}, x*{k}+{c})");
            }

            case 2:
            {
                var child = Build(random, depth - 1);
                var v = random.Next(-5, 5);
                return new Program(child.Gen.Where(x => x != v), $"where({child.Text}, x!={v})");
            }

            case 3:
            {
                var child = Build(random, depth - 1);
                var other = Build(random, depth - 1);
                return new Program(child.Gen.SelectMany(x => other.Gen.Select(y => unchecked(x + y))), $"bind({child.Text}, x => {other.Text}+x)");
            }

            case 4:
            {
                var child = Build(random, depth - 1);
                var max = random.Next(1, 6);
                return new Program(
                    child.Gen.ListOf().WithCountBetween(0, max).Select(xs => xs.Aggregate(xs.Count, (a, b) => unchecked(a * 31 + b))),
                    $"list({child.Text}, 0..{max})");
            }

            case 5:
            {
                var left = Build(random, depth - 1);
                var right = Build(random, depth - 1);
                return new Program(Gen.ValueTuple(left.Gen, right.Gen).Select(t => unchecked(t.Item1 - t.Item2)), $"tuple({left.Text}, {right.Text})");
            }

            case 7:
            {
                var left = Build(random, depth - 1);
                var right = Build(random, depth - 1);
                var third = Build(random, depth - 1);
                var w1 = random.Next(1, 5);
                var w2 = random.Next(1, 5);
                var w3 = random.Next(1, 5);
                return new Program(
                    Gen.Choose<int>().WithChoice(left.Gen, w1).WithChoice(right.Gen, w2).WithChoice(third.Gen, w3),
                    $"choose({left.Text}:{w1}, {right.Text}:{w2}, {third.Text}:{w3})");
            }

            case 8:
            {
                var leaf = Build(random, 0);
                var k = random.Next(1, 4);
                var max = random.Next(1, 4);
                return new Program(
                    Gen.Recursive<int>(leaf.Gen, g => g.ListOf().WithCountBetween(1, 3).Select(xs => xs.Aggregate(k, (a, b) => unchecked(a * 31 + b))), max),
                    $"recursive({leaf.Text}, depth {max}, k {k})");
            }

            default:
            {
                var left = Build(random, depth - 1);
                var right = Build(random, depth - 1);
                return new Program(Gen.Element(new[] { left.Gen, right.Gen }).SelectMany(g => g), $"choice({left.Text}, {right.Text})");
            }
        }
    }

    private static bool IsOwnError(Exception e)
    {
        // An exception that the engine wraps (inner exception, or a message that names the wrapped exception) is a crash.
        if (e.InnerException is not null || e.Message.Contains("Exception thrown", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("Exception:", StringComparison.Ordinal))
        {
            return false;
        }

        return e.Message.Contains("filter", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("discard", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("cannot be greater", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("'origin'", StringComparison.Ordinal)
            || e.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Holds(int x) => x % 3 != 0;

    [Fact]
    public void test_001_S13_R74_random_compositions_never_crash_or_hang_and_each_failure_shrinks_and_replays()
    {
        var random = new Random(1234);
        var problems = new List<string>();
        var falsified = 0;
        var reported = 0;
        for (var i = 0; i < Programs; i++)
        {
            var program = Build(random, random.Next(1, 5));
            var recorder = new S13Recorder<int>(Holds);
            var property = Property.ForAll(program.Gen, (Func<int, bool>)recorder.Invoke);
            try
            {
                var result = S13.Within(Limit, () => property.Check(iterations: 100, seed: i));
                if (!result.Falsified)
                {
                    continue;
                }

                falsified++;
                if (result.Counterexample is null)
                {
                    problems.Add($"{program.Text}: the property failed with no counterexample");
                    continue;
                }

                var shrunk = result.Counterexample.Value;
                if (Holds(shrunk))
                {
                    problems.Add($"{program.Text}: the shrunk value {shrunk} does not fail");
                }

                var failing = S13.Collapse(recorder.Failing);
                if (failing.Count == 0 || failing[^1] != S13.Show(shrunk))
                {
                    problems.Add($"{program.Text}: the last failing input is not the counterexample {shrunk}");
                }

                var replayed = S13.Within(Limit, () => Property.ForAll(program.Gen, Holds).Check(replay: result.Counterexample.Replay));
                if (!replayed.Falsified || replayed.Counterexample is null || replayed.Counterexample.Value != shrunk || replayed.Shrinks != 0)
                {
                    problems.Add($"{program.Text}: the replay gave falsified {replayed.Falsified}, value {replayed.Counterexample?.Value}, shrinks {replayed.Shrinks}, not {shrunk} with no shrink");
                }
            }
            catch (TimeoutException e)
            {
                problems.Add($"{program.Text}: the run passed the time limit: {e.Message}");
            }
            catch (HealthCheckException e) when (e.Check == HealthCheck.TooManyDiscards)
            {
                // A filter can remove each value of a small domain: the library reports that as a health check.
                reported++;
            }
            catch (GalaxyCheck.Exceptions.GenErrorException e) when (IsOwnError(e))
            {
                // An error that the library raises on purpose, for example an exhausted filter.
                reported++;
            }
            catch (Exception e)
            {
                problems.Add($"{program.Text}: the engine crashed: {e.GetType().Name}: {e.Message}");
            }
        }

        Assert.True(problems.Count == 0, S13.Problems(problems));
        Assert.True(reported <= Programs / 10, $"at most {Programs / 10} of the {Programs} programs may give a reported error; {reported} did");
        Assert.True(falsified >= 20, $"at least 20 of the {Programs} programs must fail, so that shrinking and replay are tested; {falsified} failed and {reported} gave a reported error");
    }
}
