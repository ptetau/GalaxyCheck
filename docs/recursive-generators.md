# Recursive generators

A recursive type holds values of its own type, for example a tree or an expression. `Gen.Recursive` builds a
generator for such a type from two parts:

- A generator of leaves: the values that hold no smaller value.
- A function that wraps a generator of smaller values in one more level.

`Gen.Recursive(leafGen, wrap, maxDepth)` does not make a value deeper than `maxDepth`. The default `maxDepth` is 5,
and a leaf has depth 0. A failing value shrinks toward a leaf.

```csharp
using GalaxyCheck;

var expressions = Gen.Recursive<Expr>(
    Gen.Int32().Between(0, 9).Select(n => (Expr)new Num(n)),
    smaller => Gen.Choose(
        smaller,
        from left in smaller
        from right in smaller
        select (Expr)new Add(left, right)),
    maxDepth: 4);

Property.ForAll(expressions, e => Depth(e) <= 4).Assert();

var failing = expressions.Minimum(seed: 0, pred: e => Eval(e) > 12);
Console.WriteLine(failing);

static int Depth(Expr e) => e is Add a ? 1 + Math.Max(Depth(a.Left), Depth(a.Right)) : 0;
static int Eval(Expr e) => e is Add a ? Eval(a.Left) + Eval(a.Right) : ((Num)e).Value;

abstract record Expr;
record Num(int Value) : Expr;
record Add(Expr Left, Expr Right) : Expr;
```

The program writes `Add { Left = Num { Value = 4 }, Right = Num { Value = 9 } }`: a small expression with a
value above 12.

## Recursive types in automatic generation

`Gen.Create<T>()` also ends for a type that refers to itself. A value nests at most 5 levels below its root, so
this tree has at most 6 levels of nodes:

```csharp
using GalaxyCheck;

var trees = Gen.Create<Node>();
Property.ForAll(trees, node => Depth(node) <= 6).Assert();
Console.WriteLine("The property holds.");

static int Depth(Node? node) => node == null ? 0 : 1 + Math.Max(Depth(node.Left), Depth(node.Right));

record Node(int Value, Node? Left, Node? Right);
```
