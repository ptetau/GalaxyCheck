using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>
/// The bug-finding table (R77): the five property kinds of "How to Specify It!" (validity, postcondition,
/// metamorphic, inductive, and model-based), written with the library, against a correct binary search tree
/// and eight versions with one planted bug each.
/// </summary>
public class S13_BugFindingTests
{
    private sealed record Input(
        IReadOnlyList<(int Key, int Value)> First,
        IReadOnlyList<(int Key, int Value)> Second,
        int Key,
        int Value,
        int Key2,
        int Value2);

    private static IGen<Input> Inputs()
    {
        var key = Gen.Int32().Between(0, 15);
        var value = Gen.Int32().Between(0, 15);
        var bindings = Gen.ValueTuple(key, value).ListOf().Select(xs => (IReadOnlyList<(int Key, int Value)>)xs.Select(b => (b.Item1, b.Item2)).ToList());
        return Gen.ValueTuple(bindings, bindings, key, value, key, value)
            .Select(t => new Input(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6));
    }

    private static bool Same(S13Node? a, S13Node? b) => S13Bst.ToList(a).SequenceEqual(S13Bst.ToList(b));

    private static List<(int Key, int Value)> Sorted(IEnumerable<(int Key, int Value)> bindings) =>
        bindings.OrderBy(b => b.Key).ToList();

    // The model of a tree is its sorted list of bindings.
    private static List<(int Key, int Value)> ModelInsert(int key, int value, List<(int Key, int Value)> model) =>
        Sorted(model.Where(b => b.Key != key).Append((key, value)));

    private static List<(int Key, int Value)> ModelDelete(int key, List<(int Key, int Value)> model) =>
        model.Where(b => b.Key != key).ToList();

    private static List<(int Key, int Value)> ModelUnion(List<(int Key, int Value)> first, List<(int Key, int Value)> second) =>
        Sorted(first.Concat(second.Where(b => first.All(f => f.Key != b.Key))));

    private static readonly (string Kind, string Name, Func<S13Bst, Input, bool> Holds)[] Properties =
    {
        // Validity
        ("validity", "generated tree is valid", (t, i) => S13Bst.Valid(t.FromList(i.First))),
        ("validity", "insert keeps validity", (t, i) => S13Bst.Valid(t.Insert(i.Key, i.Value, t.FromList(i.First)))),
        ("validity", "delete keeps validity", (t, i) => S13Bst.Valid(t.Delete(i.Key, t.FromList(i.First)))),
        ("validity", "union keeps validity", (t, i) => S13Bst.Valid(t.Union(t.FromList(i.First), t.FromList(i.Second)))),

        // Postcondition
        ("postcondition", "find after insert", (t, i) => S13Bst.Find(i.Key, t.Insert(i.Key, i.Value, t.FromList(i.First))) == i.Value),
        ("postcondition", "find after delete", (t, i) => S13Bst.Find(i.Key, t.Delete(i.Key, t.FromList(i.First))) is null),
        ("postcondition", "find after union", (t, i) =>
        {
            var a = t.FromList(i.First);
            var b = t.FromList(i.Second);
            return S13Bst.Find(i.Key, t.Union(a, b)) == (S13Bst.Find(i.Key, a) ?? S13Bst.Find(i.Key, b));
        }),

        // Metamorphic
        ("metamorphic", "insert after insert", (t, i) =>
        {
            var tree = t.FromList(i.First);
            var expected = i.Key == i.Key2
                ? t.Insert(i.Key, i.Value, tree)
                : t.Insert(i.Key2, i.Value2, t.Insert(i.Key, i.Value, tree));
            return Same(t.Insert(i.Key, i.Value, t.Insert(i.Key2, i.Value2, tree)), expected);
        }),
        ("metamorphic", "delete after insert", (t, i) =>
        {
            var tree = t.FromList(i.First);
            var expected = i.Key == i.Key2 ? t.Delete(i.Key, tree) : t.Insert(i.Key2, i.Value2, t.Delete(i.Key, tree));
            return Same(t.Delete(i.Key, t.Insert(i.Key2, i.Value2, tree)), expected);
        }),
        ("metamorphic", "delete after union", (t, i) =>
        {
            var a = t.FromList(i.First);
            var b = t.FromList(i.Second);
            return Same(t.Delete(i.Key, t.Union(a, b)), t.Union(t.Delete(i.Key, a), t.Delete(i.Key, b)));
        }),

        // Inductive
        ("inductive", "union with the empty tree", (t, i) => Same(t.Union(null, t.FromList(i.First)), t.FromList(i.First))),
        ("inductive", "union after insert", (t, i) =>
        {
            var a = t.FromList(i.First);
            var b = t.FromList(i.Second);
            return Same(t.Union(t.Insert(i.Key, i.Value, a), b), t.Insert(i.Key, i.Value, t.Union(a, b)));
        }),

        // Model-based
        ("model-based", "insert matches the model", (t, i) =>
        {
            var tree = t.FromList(i.First);
            return S13Bst.ToList(t.Insert(i.Key, i.Value, tree)).SequenceEqual(ModelInsert(i.Key, i.Value, S13Bst.ToList(tree)));
        }),
        ("model-based", "delete matches the model", (t, i) =>
        {
            var tree = t.FromList(i.First);
            return S13Bst.ToList(t.Delete(i.Key, tree)).SequenceEqual(ModelDelete(i.Key, S13Bst.ToList(tree)));
        }),
        ("model-based", "union matches the model", (t, i) =>
        {
            var a = t.FromList(i.First);
            var b = t.FromList(i.Second);
            return S13Bst.ToList(t.Union(a, b)).SequenceEqual(ModelUnion(S13Bst.ToList(a), S13Bst.ToList(b)));
        }),
    };

    private static readonly int[] Seeds = Enumerable.Range(0, 10).ToArray();

    /// <summary>Gives the names of the properties that fail on the tree for the seed, with the default iterations.</summary>
    private static List<string> Failures(S13Bst tree, int seed, bool stopAtFirst)
    {
        var failures = new List<string>();
        foreach (var (kind, name, holds) in Properties)
        {
            var result = S13.Within(TimeSpan.FromSeconds(30), () => Property.ForAll(Inputs(), i => holds(tree, i)).Check(seed: seed));
            if (result.Falsified)
            {
                failures.Add($"{kind}: {name} (counterexample {S13.Show(result.Counterexample?.Value)})");
                if (stopAtFirst) break;
            }
        }

        return failures;
    }

    public static IEnumerable<object[]> BugNumbers() => Enumerable.Range(1, 8).Select(n => new object[] { n });

    [Theory]
    [MemberData(nameof(BugNumbers))]
    public void test_001_S13_R77_each_planted_bug_is_found_on_seeds_0_to_9(int bug)
    {
        var tree = S13Bst.Buggy[bug - 1];
        var missed = Seeds.Where(seed => Failures(tree, seed, stopAtFirst: true).Count == 0).ToList();
        Assert.True(missed.Count == 0, $"bug {tree.Bug} was not found by any property on the seeds {string.Join(", ", missed)}");
    }

    [Fact]
    public void test_001_S13_R77_no_property_fails_on_the_correct_tree()
    {
        var tree = new S13Bst();
        var failures = Seeds.SelectMany(seed => Failures(tree, seed, stopAtFirst: false).Select(f => $"seed {seed}: {f}")).ToList();
        Assert.True(failures.Count == 0, "no property may fail on the correct tree:\n" + string.Join("\n", failures));
    }
}
