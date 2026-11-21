using FluentAssertions;
using GalaxyCheck;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public abstract record S7Tree;

public sealed record S7Leaf(int Value) : S7Tree;

public sealed record S7TreeNode(S7Tree Left, S7Tree Right) : S7Tree;

public class S7NodeClass
{
    public List<S7NodeClass>? Children { get; set; }
}

public class S7_RecursiveTests
{
    private static IGen<S7Tree> TreeGen(int maxDepth)
    {
        IGen<S7Tree> leaf = Gen.Int32().Between(0, 9).Select(i => (S7Tree)new S7Leaf(i));
        return Gen.Recursive<S7Tree>(
            leaf,
            smaller => Gen.ValueTuple(smaller, smaller).Select(t => (S7Tree)new S7TreeNode(t.Item1, t.Item2)),
            maxDepth);
    }

    private static int Depth(S7Tree tree) => tree switch
    {
        S7TreeNode n => 1 + Math.Max(Depth(n.Left), Depth(n.Right)),
        _ => 0,
    };

    private static int Depth(S7NodeClass node) =>
        node.Children == null || node.Children.Count == 0 ? 0 : 1 + node.Children.Max(c => Depth(c));

    [Fact]
    public void test_001_S7_R53_recursive_generator_respects_depth_limit_and_reaches_it()
    {
        var values = TreeGen(3).Sample(iterations: 1000, seed: 0);

        var depths = values.Select(Depth).ToList();
        depths.Max().Should().Be(3);
    }

    [Fact]
    public void test_001_S7_R53_recursive_failure_shrinks_to_node_of_leaves()
    {
        var result = Property.ForAll(TreeGen(3), (S7Tree t) => t is not S7TreeNode).Check(seed: 0);

        result.Falsified.Should().BeTrue();
        var node = result.Counterexample!.Value.Should().BeOfType<S7TreeNode>().Subject;
        node.Left.Should().BeOfType<S7Leaf>();
        node.Right.Should().BeOfType<S7Leaf>();
    }

    [Fact]
    public void test_001_S7_R54_create_self_referencing_type_ends_within_limit()
    {
        var values = Gen.Create<S7NodeClass>().Sample(iterations: 100, seed: 0);

        values.Should().HaveCount(100);
        values.Select(Depth).Max().Should().BeLessThanOrEqualTo(5);
    }
}
