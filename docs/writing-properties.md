# Writing properties

A property joins a generator and a test function. GalaxyCheck runs the test function for many generated values.
When a test case fails, GalaxyCheck shrinks the value and reports the smallest value that still fails.

The core package, GalaxyCheck, runs a property with no test framework. The framework packages run the same
properties through attributes. Read [Getting started](getting-started.md).

## Words used here

- Generator: an `IGen<T>`, which makes values of `T`.
- Counterexample: a value for which the property fails.
- Shrink: a change of a counterexample to a smaller value that still fails.
- Replay string: a short text that holds the choices of one test case. A run with the replay string runs only
  that test case.

## Make a property

`Property.ForAll` makes a property from one to four generators. The test function returns a `bool`, or it
throws an exception to fail:

```csharp
using GalaxyCheck;

var commutes = Property.ForAll(Gen.Int32(), Gen.Int32(), (x, y) => x + y == y + x);
commutes.Assert();

var nonNegative = Gen.Int32().ListOf().ForAll(xs =>
{
    if (xs.Count < 0) throw new Exception("A count is never negative.");
});
nonNegative.Assert();

Console.WriteLine("Both properties hold.");
```

`Property.ForAllAsync` makes an async property. Run it with `CheckAsync` or `AssertAsync`:

```csharp
using GalaxyCheck;

var property = Property.ForAllAsync(Gen.Int32(), async x =>
{
    await Task.Yield();
    return x * 1 == x;
});

await property.AssertAsync();
Console.WriteLine("The async property holds.");
```

## Run a property

`Assert` throws `GalaxyCheck.Runners.PropertyFailedException` when the property fails. `Check` returns a result
that you can read:

```csharp
using GalaxyCheck;

var result = Property.ForAll(Gen.Int32(), x => x < 10).Check(seed: 0);

Console.WriteLine(result.Falsified);
Console.WriteLine(result.Counterexample!.Value);
Console.WriteLine(result.Counterexample.Replay);
Console.WriteLine(result.Iterations);
Console.WriteLine(result.Shrinks);
```

This program writes these lines:

```
True
10
AgAMAgoA
13
1
```

The result also has `Labels`, `Exhaustive`, and `Report`. Read [Labels and cover](labels-and-cover.md) and
[Exhaustive runs](exhaustive-runs.md).

## The options of a run

`Check`, `CheckAsync`, `Assert`, and `AssertAsync` take the same options:

| Option | Default | Effect |
| --- | --- | --- |
| `iterations` | 100 | The count of test cases. |
| `seed` | random | The seed of the run. The same seed gives the same test cases. |
| `size` | grows from 0 to 100 | A fixed size from 0 to 100 for each test case. |
| `shrinkLimit` | 500 | The maximum count of shrinks. |
| `replay` | none | A replay string. The run runs only that test case. |
| `deepCheck` | true | When false, the run stops at the first counterexample size. |
| `timeout` | none | The time limit of one test case. Read [Timeouts and cancellation](timeouts-and-cancellation.md). |
| `exhaustive` | true | Tries each value one time when the input space is small. |
| `suppressHealthChecks` | none | The health checks to switch off. Read [Health checks](health-checks.md). |

The `[Property]` attribute has the options `Iterations`, `ShrinkLimit`, `Seed`, `Size`, `TimeoutMilliseconds`,
and `Exhaustive`.

The environment variables `GALAXYCHECK_SEED` and `GALAXYCHECK_AGENT` set the seed of a run that has no seed
option. Read [Use by LLM agents](agents.md).

## Look at the values of a generator

`Sample` returns a list of values. `SampleOne` returns one value. `Minimum` returns the smallest value for which
a condition is true:

```csharp
using GalaxyCheck;

List<int> values = Gen.Int32().Between(0, 9).Sample(iterations: 5, seed: 0);
Console.WriteLine(string.Join(", ", values));

int smallest = Gen.Int32().Minimum(seed: 0, pred: x => x > 100);
Console.WriteLine(smallest);
```

The last line of the output is `101`.

## Build a generator

A generator is a LINQ expression. `Select` changes each value. `SelectMany`, or a second `from`, uses a value to
build the next generator. `Where` keeps only the values that pass a condition:

```csharp
using GalaxyCheck;

IGen<(int Min, int Max)> ranges =
    from min in Gen.Int32().Between(0, 100)
    from max in Gen.Int32().Between(min, 100)
    select (min, max);

IGen<int> evens = Gen.Int32().Where(x => x % 2 == 0);

Property.ForAll(ranges, range => range.Min <= range.Max).Assert();
Property.ForAll(evens, x => x % 2 == 0).Assert();
Console.WriteLine("Both properties hold.");
```

Each shrink of a built generator obeys the same constraints. In this example, a shrunk range still has
`Min <= Max`.

The [generator reference](generators.md) lists each generator, and [llms.txt](llms.txt) lists each operator.
