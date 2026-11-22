namespace GalaxyCheck.Tests.Slice;

/// <summary>A node of a binary search tree that maps int keys to int values. Null is the empty tree.</summary>
internal sealed record S13Node(S13Node? Left, int Key, int Value, S13Node? Right);

/// <summary>
/// A finite map as a binary search tree, after the example of "How to Specify It!" (Hughes, 2019).
/// This class is the correct version. Each subclass plants one bug, as the paper describes it.
/// </summary>
internal class S13Bst
{
    public virtual string Bug => "none (the correct tree)";

    public static int? Find(int key, S13Node? tree)
    {
        while (tree != null)
        {
            if (key < tree.Key) tree = tree.Left;
            else if (key > tree.Key) tree = tree.Right;
            else return tree.Value;
        }

        return null;
    }

    public virtual S13Node? Insert(int key, int value, S13Node? tree)
    {
        if (tree is null) return new S13Node(null, key, value, null);
        if (key < tree.Key) return tree with { Left = Insert(key, value, tree.Left) };
        if (key > tree.Key) return tree with { Right = Insert(key, value, tree.Right) };
        return tree with { Value = value };
    }

    public virtual S13Node? Delete(int key, S13Node? tree)
    {
        if (tree is null) return null;
        if (key < tree.Key) return tree with { Left = Delete(key, tree.Left) };
        if (key > tree.Key) return tree with { Right = Delete(key, tree.Right) };
        return Join(tree.Left, tree.Right);
    }

    /// <summary>The union of two maps. The first map takes priority when both hold a key.</summary>
    public virtual S13Node? Union(S13Node? first, S13Node? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        var (less, _, greater) = Split(first.Key, second);
        return new S13Node(Union(first.Left, less), first.Key, first.Value, Union(first.Right, greater));
    }

    public S13Node? FromList(IEnumerable<(int Key, int Value)> bindings) =>
        bindings.Aggregate((S13Node?)null, (tree, b) => Insert(b.Key, b.Value, tree));

    public static List<(int Key, int Value)> ToList(S13Node? tree)
    {
        var result = new List<(int, int)>();
        void Walk(S13Node? t)
        {
            if (t is null) return;
            Walk(t.Left);
            result.Add((t.Key, t.Value));
            Walk(t.Right);
        }

        Walk(tree);
        return result;
    }

    /// <summary>A tree is valid when its keys, in order, strictly increase.</summary>
    public static bool Valid(S13Node? tree)
    {
        var keys = ToList(tree).Select(b => b.Key).ToList();
        return keys.Zip(keys.Skip(1)).All(p => p.First < p.Second);
    }

    /// <summary>Joins two trees when each key of the first is below each key of the second.</summary>
    protected static S13Node? Join(S13Node? left, S13Node? right) =>
        left is null ? right : left with { Right = Join(left.Right, right) };

    protected static (S13Node? Less, int? Found, S13Node? Greater) Split(int key, S13Node? tree)
    {
        if (tree is null) return (null, null, null);
        if (key < tree.Key)
        {
            var (less, found, greater) = Split(key, tree.Left);
            return (less, found, tree with { Left = greater });
        }

        if (key > tree.Key)
        {
            var (less, found, greater) = Split(key, tree.Right);
            return (tree with { Right = less }, found, greater);
        }

        return (tree.Left, tree.Value, tree.Right);
    }

    public static IReadOnlyList<S13Bst> Buggy { get; } = new S13Bst[]
    {
        new Bug1(), new Bug2(), new Bug3(), new Bug4(), new Bug5(), new Bug6(), new Bug7(), new Bug8(),
    };

    /// <summary>Bug 1: insert discards the existing tree, and gives a tree of the new binding alone.</summary>
    private sealed class Bug1 : S13Bst
    {
        public override string Bug => "1: insert discards the existing tree";

        public override S13Node? Insert(int key, int value, S13Node? tree) => new S13Node(null, key, value, null);
    }

    /// <summary>Bug 2: insert adds a second node for a key that is already present, instead of replacing its value.</summary>
    private sealed class Bug2 : S13Bst
    {
        public override string Bug => "2: insert duplicates a key that is present";

        public override S13Node? Insert(int key, int value, S13Node? tree)
        {
            if (tree is null) return new S13Node(null, key, value, null);
            if (key < tree.Key) return tree with { Left = Insert(key, value, tree.Left) };
            return tree with { Right = Insert(key, value, tree.Right) };
        }
    }

    /// <summary>Bug 3: insert keeps the old value when the key is present.</summary>
    private sealed class Bug3 : S13Bst
    {
        public override string Bug => "3: insert does not update the value of a present key";

        public override S13Node? Insert(int key, int value, S13Node? tree)
        {
            if (tree is null) return new S13Node(null, key, value, null);
            if (key < tree.Key) return tree with { Left = Insert(key, value, tree.Left) };
            if (key > tree.Key) return tree with { Right = Insert(key, value, tree.Right) };
            return tree;
        }
    }

    /// <summary>Bug 4: delete does not rebuild the tree correctly when it finds the key: it loses the right subtree.</summary>
    private sealed class Bug4 : S13Bst
    {
        public override string Bug => "4: delete does not rebuild the tree when it finds the key";

        public override S13Node? Delete(int key, S13Node? tree)
        {
            if (tree is null) return null;
            if (key < tree.Key) return tree with { Left = Delete(key, tree.Left) };
            if (key > tree.Key) return tree with { Right = Delete(key, tree.Right) };
            return tree.Left;
        }
    }

    /// <summary>Bug 5: delete compares the keys the wrong way round, so it searches the wrong subtree.</summary>
    private sealed class Bug5 : S13Bst
    {
        public override string Bug => "5: delete searches the wrong subtree";

        public override S13Node? Delete(int key, S13Node? tree)
        {
            if (tree is null) return null;
            if (key > tree.Key) return tree with { Left = Delete(key, tree.Left) };
            if (key < tree.Key) return tree with { Right = Delete(key, tree.Right) };
            return Join(tree.Left, tree.Right);
        }
    }

    /// <summary>Bug 6: union assumes that each key of the first tree is below each key of the second.</summary>
    private sealed class Bug6 : S13Bst
    {
        public override string Bug => "6: union assumes the first tree is below the second";

        public override S13Node? Union(S13Node? first, S13Node? second) => Join(first, second);
    }

    /// <summary>
    /// Bug 7: union assumes that when the root key of one tree is below the root key of the other,
    /// each key of the first tree is below the root key of the other.
    /// </summary>
    private sealed class Bug7 : S13Bst
    {
        public override string Bug => "7: union assumes a whole tree is below the other root";

        public override S13Node? Union(S13Node? first, S13Node? second)
        {
            if (first is null) return second;
            if (second is null) return first;
            if (first.Key < second.Key) return second with { Left = Union(first, second.Left) };
            if (first.Key > second.Key) return first with { Left = Union(first.Left, second) };
            return first with { Left = Union(first.Left, second.Left), Right = Union(first.Right, second.Right) };
        }
    }

    /// <summary>Bug 8: union gives the second tree priority when both trees hold a key.</summary>
    private sealed class Bug8 : S13Bst
    {
        public override string Bug => "8: union does not give the first tree priority";

        public override S13Node? Union(S13Node? first, S13Node? second)
        {
            if (first is null) return second;
            if (second is null) return first;
            var (less, found, greater) = Split(first.Key, second);
            return new S13Node(Union(first.Left, less), first.Key, found ?? first.Value, Union(first.Right, greater));
        }
    }
}
