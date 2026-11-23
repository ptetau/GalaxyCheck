# Generators for containers

GalaxyCheck makes lists, sets, dictionaries, key-value pairs, and tuples from the generators of their elements.
A failing container shrinks its count first, and then it shrinks its elements.

## Lists and sets

`Gen.List(elementGen)`, or `elementGen.ListOf()`, makes `IReadOnlyList<T>` values. `Gen.Set(elementGen)`, or
`elementGen.SetOf()`, makes `IReadOnlySet<T>` values. The count options are:

- `WithCount(count)`: an exact count.
- `WithCountGreaterThanEqual(min)`, `WithCountLessThanEqual(max)`, and `WithCountBetween(x, y)`: inclusive
  bounds of the count.
- `WithCountGreaterThan(min)` and `WithCountLessThan(max)`: exclusive bounds of the count.
- `WithCountBias(bias)`: the bias of the count.

A list has at most 1000 elements. To make a larger list, call `DisableCountLimitUnsafe()` on the list
generator.

```csharp
using GalaxyCheck;

var shortLists = Gen.Int32().ListOf().WithCountBetween(1, 5);
var tags = Gen.String().WithLengthBetween(1, 3).SetOf().WithCountLessThanEqual(4);

Property.ForAll(shortLists, xs => xs.Count is >= 1 and <= 5).Assert();
Property.ForAll(tags, set => set.Distinct().Count() == set.Count).Assert();

var failing = Gen.Int32().ListOf().Minimum(seed: 0, pred: xs => xs.Sum() > 10);
Console.WriteLine($"[{string.Join(", ", failing)}]");
```

The program writes `[11]`. The shrinker removes each element that it can, and then it shrinks the elements.

## Dictionaries

`Gen.Dictionary(keyGen, valueGen)` makes `IReadOnlyDictionary<TKey, TValue>` values. The keys of a value are
distinct. The count options are `WithCount`, `WithCountGreaterThanEqual`, `WithCountLessThanEqual`, and
`WithCountBetween`. A dictionary has at most 1000 entries.

```csharp
using GalaxyCheck;

var stock = Gen.Dictionary(Gen.String().WithLengthBetween(1, 8), Gen.Int32().Between(0, 100))
    .WithCountLessThanEqual(5);

Property.ForAll(stock, items => items.Count <= 5 && items.Values.All(v => v is >= 0 and <= 100)).Assert();
Console.WriteLine("The property holds.");
```

## Pairs and tuples

These generators take one generator for each element, and they shrink each element:

- `Gen.KeyValuePair(keyGen, valueGen)` makes `KeyValuePair<TKey, TValue>` values.
- `Gen.ValueTuple(g1, ..., g7)` makes value tuples with 1 to 7 elements.
- `Gen.Tuple(g1, ..., g7)` makes `System.Tuple` values with 1 to 7 elements.
- `Gen.Zip(g1, ..., g4)` makes value tuples from 2 to 4 generators.
- `gen.Two()`, `gen.Three()`, and `gen.Four()` make value tuples from one generator.

```csharp
using GalaxyCheck;

var pair = Gen.KeyValuePair(Gen.String(), Gen.Int32());
var point = Gen.ValueTuple(Gen.Int32(), Gen.Int32(), Gen.Int32());
var record = Gen.Tuple(Gen.Boolean(), Gen.Guid());

Console.WriteLine(pair.SampleOne(seed: 0).GetType().Name);
Console.WriteLine(point.SampleOne(seed: 0).GetType().Name);
Console.WriteLine(record.SampleOne(seed: 0).GetType().Name);
```

## Containers through automatic generation

`Gen.Create<T>()`, and a `[Property]` parameter, also make these containers:

- Arrays, `List<T>`, `IList<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `IEnumerable<T>`, and
  `ImmutableList<T>`.
- `HashSet<T>`, `ISet<T>`, `IReadOnlySet<T>`, and `ImmutableHashSet<T>`.
- `Queue<T>`, `Stack<T>`, `SortedSet<T>`, `LinkedList<T>`, `ImmutableArray<T>`, `ReadOnlyCollection<T>`,
  `Memory<T>`, and `ReadOnlyMemory<T>`. Each shrinks as a list shrinks.
- `Dictionary<TKey, TValue>`, `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>`, and
  `ImmutableDictionary<TKey, TValue>`.
- `KeyValuePair<TKey, TValue>`, and `ValueTuple` and `Tuple` with 1 to 7 elements.

```csharp
using GalaxyCheck;
using System.Collections.Immutable;

Property.ForAll(Gen.Create<Queue<int>>(), queue => queue.Count >= 0).Assert();
Property.ForAll(Gen.Create<ImmutableDictionary<string, bool>>(), map => map.Count >= 0).Assert();
Property.ForAll(Gen.Create<(int, string, bool)>(), tuple => tuple.Item2 != null).Assert();
Console.WriteLine("Each property holds.");
```

Read [Automatic generation](automatic-generation.md) for the other types that `Gen.Create<T>()` makes.
