using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>
/// The shrinking challenge (R69) and the shrink budget (R70). Each challenge is written new from its
/// problem statement. Each runs on the seeds 0 to 19, and its counterexample must be the best known minimum.
/// </summary>
public class S13_ShrinkingChallengeTests
{
    private const int Iterations = 1000;

    private static void Challenge<T>(
        string name,
        IGen<T> gen,
        Func<T, bool> holds,
        Func<T, bool> isBest,
        string best,
        bool suppressDiscards = false)
    {
        var problems = new List<string>();
        var suppress = suppressDiscards ? new[] { HealthCheck.TooManyDiscards } : null;
        foreach (var seed in S13.Seeds)
        {
            try
            {
                var result = S13.Within(TimeSpan.FromSeconds(60), () =>
                    Property.ForAll(gen, holds).Check(iterations: Iterations, seed: seed, suppressHealthChecks: suppress));
                if (!result.Falsified || result.Counterexample is null)
                {
                    problems.Add($"seed {seed}: the property did not fail in {Iterations} iterations");
                }
                else if (!isBest(result.Counterexample.Value))
                {
                    problems.Add($"seed {seed}: the counterexample is {S13.Show(result.Counterexample.Value)}, not {best}");
                }
            }
            catch (Exception e)
            {
                problems.Add($"seed {seed}: {e.GetType().Name}: {e.Message}");
            }
        }

        Assert.True(problems.Count == 0, $"{name}: {S13.Problems(problems)}");
    }

    private static bool Is(IEnumerable<int> xs, params int[] expected) => xs.SequenceEqual(expected);

    [Fact]
    public void test_001_S13_R69_reverse_shrinks_to_0_1()
    {
        // A list of ints is equal to its reverse.
        Challenge(
            "reverse",
            Gen.Int32().ListOf(),
            xs => xs.Reverse().SequenceEqual(xs),
            xs => Is(xs, 0, 1),
            "[0, 1]");
    }

    [Fact]
    public void test_001_S13_R69_lengthlist_shrinks_to_900()
    {
        // A length from 1 to 100, then (by bind) a list of that length of ints from 0 to 1000. Fails if the maximum is 900 or more.
        var gen =
            from length in Gen.Int32().Between(1, 100)
            from xs in Gen.Int32().Between(0, 1000).ListOf().WithCount(length)
            select xs;
        Challenge(
            "lengthlist",
            gen,
            xs => xs.Max() < 900,
            xs => Is(xs, 900),
            "[900]");
    }

    [Fact]
    public void test_001_S13_R69_nestedlists_shrinks_to_one_list_of_11_zeros()
    {
        // The sum of the lengths of the element lists is at most 10.
        Challenge(
            "nestedlists",
            Gen.Int32().ListOf().ListOf(),
            xss => xss.Sum(xs => xs.Count) <= 10,
            xss => xss.Count == 1 && xss[0].Count == 11 && xss[0].All(x => x == 0),
            "one list of 11 zeros");
    }

    [Fact]
    public void test_001_S13_R69_deletion_shrinks_to_0_0_and_0()
    {
        // Remove an element of the list (only its first instance is removed); the element is no longer in the list.
        var gen =
            from xs in Gen.Int32().ListOf().WithCountGreaterThanEqual(1)
            from x in Gen.Element(xs.ToArray())
            select (List: xs, Element: x);
        Challenge(
            "deletion",
            gen,
            t =>
            {
                var copy = t.List.ToList();
                copy.Remove(t.Element);
                return !copy.Contains(t.Element);
            },
            t => Is(t.List, 0, 0) && t.Element == 0,
            "([0, 0], 0)");
    }

    [Fact]
    public void test_001_S13_R69_distinct_shrinks_to_0_1_minus1_or_0_1_2()
    {
        // A list of ints has fewer than three distinct elements.
        Challenge(
            "distinct",
            Gen.Int32().ListOf(),
            xs => xs.Distinct().Count() < 3,
            xs => Is(xs, 0, 1, -1) || Is(xs, 0, 1, 2),
            "[0, 1, -1] or [0, 1, 2]");
    }

    [Fact]
    public void test_001_S13_R69_coupling_shrinks_to_1_0()
    {
        // Each element is an index into the list. For each i with xs[i] != i, xs[xs[i]] != i.
        var gen = Gen.Int32().Between(0, 10).ListOf().Where(xs => xs.All(i => i < xs.Count));
        Challenge(
            "coupling",
            gen,
            xs => Enumerable.Range(0, xs.Count).All(i => xs[i] == i || xs[xs[i]] != i),
            xs => Is(xs, 1, 0),
            "[1, 0]",
            suppressDiscards: true);
    }

    private static IGen<(int X, int Y)> TwoPositiveInts() =>
        Gen.ValueTuple(Gen.Int32().GreaterThan(0), Gen.Int32().GreaterThan(0)).Select(t => (X: t.Item1, Y: t.Item2));

    [Fact]
    public void test_001_S13_R69_difference_must_not_be_zero_shrinks_to_10_10()
    {
        Challenge(
            "difference 1",
            TwoPositiveInts(),
            t => t.X < 10 || Math.Abs(t.X - t.Y) != 0,
            t => t == (10, 10),
            "[10, 10]");
    }

    [Fact]
    public void test_001_S13_R69_difference_must_not_be_small_shrinks_to_10_6()
    {
        Challenge(
            "difference 2",
            TwoPositiveInts(),
            t => t.X < 10 || Math.Abs(t.X - t.Y) < 1 || Math.Abs(t.X - t.Y) > 4,
            t => t == (10, 6),
            "[10, 6]");
    }

    [Fact]
    public void test_001_S13_R69_difference_must_not_be_one_shrinks_to_10_9()
    {
        Challenge(
            "difference 3",
            TwoPositiveInts(),
            t => t.X < 10 || Math.Abs(t.X - t.Y) != 1,
            t => t == (10, 9),
            "[10, 9]");
    }

    [Fact]
    public void test_001_S13_R69_large_union_list_shrinks_to_one_list_of_five_smallest_distinct_ints()
    {
        // There are no more than four distinct ints across all the lists.
        Challenge(
            "large union list",
            Gen.Int32().ListOf().ListOf(),
            xss => xss.SelectMany(xs => xs).Distinct().Count() <= 4,
            xss => xss.Count == 1
                && xss[0].Count == 5
                && xss[0].Distinct().Count() == 5
                && xss[0].Sum(x => Math.Abs((long)x)) == 6,
            "one list of five distinct ints of the smallest size, the set {0, 1, -1, 2, -2}");
    }

    private static short Sum16(IEnumerable<short> xs) => xs.Aggregate((short)0, (a, b) => unchecked((short)(a + b)));

    [Fact]
    public void test_001_S13_R69_bound5_shrinks_to_minus32768_and_minus1()
    {
        // A 5-tuple of lists of 16-bit ints. If each list sums (with 16-bit overflow) to less than 256,
        // then the sum of all the values is less than 5 * 256.
        var list = Gen.Int16().ListOf();
        var gen = Gen.ValueTuple(list, list, list, list, list)
            .Select(t => new[] { t.Item1, t.Item2, t.Item3, t.Item4, t.Item5 });
        Challenge(
            "bound5",
            gen,
            lists =>
            {
                Property.Precondition(lists.All(xs => Sum16(xs) < 256));
                return Sum16(lists.SelectMany(xs => xs)) < 5 * 256;
            },
            lists => lists[0].SequenceEqual(new short[] { -32768 })
                && lists[1].SequenceEqual(new short[] { -1 })
                && lists.Skip(2).All(xs => xs.Count == 0),
            "([-32768], [-1], [], [], [])",
            suppressDiscards: true);
    }

    // The binary heap challenge. A heap is empty (null) or a node whose value is at most the values of its children.
    internal sealed record Heap(long Value, Heap? Left, Heap? Right);

    private static int Nodes(Heap? h) => h is null ? 0 : 1 + Nodes(h.Left) + Nodes(h.Right);

    private static Heap? Merge(Heap? a, Heap? b)
    {
        if (a is null) return b;
        if (b is null) return a;
        return a.Value <= b.Value ? new Heap(a.Value, Merge(a.Right, b), a.Left) : Merge(b, a);
    }

    // Lists the values of a heap in an order that is not sorted: the node, then the right child, then the left child.
    private static IEnumerable<long> Values(Heap? h) =>
        h is null ? Enumerable.Empty<long>() : new[] { h.Value }.Concat(Values(h.Right)).Concat(Values(h.Left));

    // The wrong conversion to a sorted list: the root, then the values of the merged children, in the order of Values.
    private static List<long> WrongToSortedList(Heap? h) =>
        h is null ? new List<long>() : new[] { h.Value }.Concat(Values(Merge(h.Left, h.Right))).ToList();

    private static Heap? Offset(Heap? h, long by) =>
        h is null ? null : new Heap(h.Value + by, Offset(h.Left, h.Value + by), Offset(h.Right, h.Value + by));

    [Fact]
    public void test_001_S13_R69_binheap_shrinks_to_at_most_four_nodes()
    {
        // A heap is built from a tree of non-negative steps: the value of a node is the value of its parent plus its step,
        // so each generated heap is valid. The wrong conversion must give the sorted values of the heap.
        var steps = Gen.Recursive<Heap?>(
            Gen.Constant<Heap?>(null),
            g => Gen.ValueTuple(Gen.Int32().GreaterThanEqual(0), g, g).Select(t => (Heap?)new Heap(t.Item1, t.Item2, t.Item3)));
        var heaps = steps.Select(h => Offset(h, 0));

        // Sanity: the four-node heap of the challenge fails the property.
        var fourNodes = new Heap(0, null, new Heap(0, new Heap(0, null, null), new Heap(1, null, null)));
        Assert.False(WrongToSortedList(fourNodes).SequenceEqual(WrongToSortedList(fourNodes).OrderBy(v => v)));

        Challenge(
            "binheap",
            heaps,
            h => WrongToSortedList(h).SequenceEqual(WrongToSortedList(h).OrderBy(v => v)),
            h => Nodes(h) <= 4,
            "no more nodes than (0, None, (0, (0, None, None), (1, None, None)))");
    }

    // The calculator challenge: integers, additions, and divisions.
    internal abstract record Expr;

    internal sealed record Lit(int N) : Expr;

    internal sealed record Add(Expr L, Expr R) : Expr;

    internal sealed record Div(Expr L, Expr R) : Expr;

    private static int Terms(Expr e) => e switch
    {
        Add a => 1 + Terms(a.L) + Terms(a.R),
        Div d => 1 + Terms(d.L) + Terms(d.R),
        _ => 1,
    };

    private static bool NoLiteralZeroDivisor(Expr e) => e switch
    {
        Div { R: Lit { N: 0 } } => false,
        Add a => NoLiteralZeroDivisor(a.L) && NoLiteralZeroDivisor(a.R),
        Div d => NoLiteralZeroDivisor(d.L) && NoLiteralZeroDivisor(d.R),
        _ => true,
    };

    private static long Eval(Expr e) => e switch
    {
        Lit l => l.N,
        Add a => Eval(a.L) + Eval(a.R),
        Div d => Eval(d.L) / Eval(d.R),
        _ => throw new InvalidOperationException(),
    };

    [Fact]
    public void test_001_S13_R69_calculator_shrinks_to_at_most_five_terms()
    {
        // If no subterm has the form x / 0, then the expression evaluates with no division by zero.
        var exprs = Gen.Recursive<Expr>(
            Gen.Int32().Select(n => (Expr)new Lit(n)),
            g => Gen.ValueTuple(Gen.Boolean(), g, g).Select(t => t.Item1 ? (Expr)new Add(t.Item2, t.Item3) : new Div(t.Item2, t.Item3)));
        Challenge(
            "calculator",
            exprs,
            e =>
            {
                Property.Precondition(NoLiteralZeroDivisor(e));
                try
                {
                    Eval(e);
                    return true;
                }
                catch (DivideByZeroException)
                {
                    return false;
                }
            },
            e => Terms(e) <= 5,
            "no more terms than 0 / (0 + 0)",
            suppressDiscards: true);
    }

    [Fact]
    public void test_001_S13_R70_minimal_first_failure_shrinks_with_at_most_50_more_calls()
    {
        // "The int is below 10" on the ints from 0 to 10: the only failing value is 10, which is already the minimum.
        var problems = new List<string>();
        foreach (var seed in S13.Seeds)
        {
            var calls = new List<int>();
            var property = Property.ForAll(Gen.Int32().Between(0, 10), x =>
            {
                lock (calls) calls.Add(x);
                return x < 10;
            });
            var result = S13.Within(TimeSpan.FromSeconds(30), () => property.Check(iterations: 1000, seed: seed, exhaustive: false));
            var firstFailure = calls.FindIndex(x => x >= 10);
            if (!result.Falsified || firstFailure < 0)
            {
                problems.Add($"seed {seed}: the property did not fail");
                continue;
            }

            var more = calls.Count - firstFailure - 1;
            if (more > 50)
            {
                problems.Add($"seed {seed}: shrinking called the test function {more} more times after the first failing value 10");
            }
        }

        Assert.True(problems.Count == 0, S13.Problems(problems));
    }
}
