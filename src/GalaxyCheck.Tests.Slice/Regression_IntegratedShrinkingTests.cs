using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GalaxyCheck;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>
/// Integrated shrinking: each value that the test function sees, also while the counterexample shrinks, obeys the
/// rule of its generator. The counterexample is the smallest one, and its replay string gives it again.
/// </summary>
public class Regression_IntegratedShrinkingTests
{
    private const int Seeds = 60;

    private static void Check<T>(IGen<T> gen, Func<T, bool> holds, Func<T, bool> obeysGenerator, Func<T, bool> isSmallest, Func<T, string> show)
    {
        var faults = new List<string>();
        for (var seed = 0; seed < Seeds; seed++)
        {
            var seen = new List<T>();
            var result = gen.ForAll(x =>
            {
                seen.Add(x);
                return holds(x);
            }).Check(seed: seed, iterations: 200);

            if (!result.Falsified)
            {
                faults.Add($"seed {seed}: the property was not falsified");
                continue;
            }

            var counterexample = result.Counterexample!.Value;
            var broke = seen.Where(x => !obeysGenerator(x)).ToList();
            if (broke.Count > 0) faults.Add($"seed {seed}: {broke.Count} values broke the rule of the generator, for example {show(broke[0])}");
            if (!isSmallest(counterexample)) faults.Add($"seed {seed}: the counterexample {show(counterexample)} is not the smallest");

            var again = gen.ForAll(holds).Check(replay: result.Counterexample.Replay);
            if (!again.Falsified || show(again.Counterexample!.Value) != show(counterexample))
            {
                faults.Add($"seed {seed}: the replay does not give {show(counterexample)} again");
            }
        }

        faults.Should().BeEmpty();
    }

    private static string Show(IEnumerable<int> xs) => "[" + string.Join(",", xs) + "]";

    [Fact]
    public void test_001_fix_shrinking_keeps_the_bounds_of_an_int() =>
        Check(Gen.Int32().Between(10, 1000), x => x < 50, x => x >= 10 && x <= 1000, x => x == 50, x => x.ToString());

    [Fact]
    public void test_001_fix_shrinking_keeps_the_rule_of_a_select() =>
        Check(Gen.Int32().Between(0, 10000).Select(x => x * 2), x => x < 100, x => x % 2 == 0, x => x == 100, x => x.ToString());

    [Fact]
    public void test_001_fix_shrinking_keeps_the_rule_of_a_where() =>
        Check(Gen.Int32().Between(0, 10000).Where(x => x % 2 == 1), x => x < 100, x => x % 2 == 1, x => x == 101, x => x.ToString());

    [Fact]
    public void test_001_fix_shrinking_keeps_a_dependent_pair_in_order() =>
        Check(
            from lo in Gen.Int32().Between(0, 100)
            from hi in Gen.Int32().Between(lo, 200)
            select (lo, hi),
            p => p.hi - p.lo < 10,
            p => p.lo >= 0 && p.lo <= p.hi && p.hi <= 200,
            p => p == (0, 10),
            p => p.ToString());

    [Fact]
    public void test_001_fix_shrinking_keeps_a_list_at_the_length_of_its_prefix_and_finds_the_smallest() =>
        Check(
            from n in Gen.Int32().Between(0, 20)
            from xs in Gen.Int32().Between(0, 9).ListOf().WithCount(n)
            select (n, xs),
            p => p.xs.Sum() < 15,
            p => p.xs.Count == p.n && p.xs.All(x => x >= 0 && x <= 9),
            p => p.n == 2 && p.xs.SequenceEqual(new[] { 6, 9 }),
            p => p.n + ":" + Show(p.xs));

    [Fact]
    public void test_001_fix_shrinking_takes_a_list_to_one_element() =>
        Check(Gen.Int32().Between(0, 100000).ListOf(), xs => xs.All(x => x < 100), xs => xs.All(x => x >= 0), xs => xs.Count == 1 && xs[0] == 100, xs => Show(xs));

    [Fact]
    public void test_001_fix_shrinking_keeps_a_list_sorted_and_distinct() =>
        Check(
            Gen.Int32().Between(0, 50).ListOf().Select(xs => xs.Distinct().Order().ToList()),
            xs => xs.Count < 3,
            xs => xs.SequenceEqual(xs.Distinct().Order()),
            xs => xs.SequenceEqual(new[] { 0, 1, 2 }),
            xs => Show(xs));

    [Fact]
    public void test_001_fix_shrinking_keeps_the_length_of_a_string() =>
        Check(
            Gen.String().WithLengthBetween(3, 10),
            s => !s.Contains('b'),
            s => s.Length >= 3 && s.Length <= 10,
            s => s.Length == 3 && s.Count(c => c == 'b') == 1,
            s => "\"" + s + "\"");

    [Fact]
    public void test_001_fix_shrinking_lowers_two_values_that_fail_together() =>
        Check(
            from a in Gen.Int32().Between(0, 1000)
            from b in Gen.Int32().Between(0, 1000)
            select (a, b),
            p => p.a + p.b < 500,
            p => p.a >= 0 && p.a <= 1000 && p.b >= 0 && p.b <= 1000,
            p => p.a + p.b == 500,
            p => p.ToString());
}
