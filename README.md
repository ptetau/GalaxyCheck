# GalaxyCheck

GalaxyCheck is a property-based testing library for .NET, in the style of
[FsCheck](https://github.com/fscheck/FsCheck). The design of GalaxyCheck comes more directly from
[fast-check](https://github.com/dubzzz/fast-check), [jqwik](https://github.com/jlink/jqwik), and
[Hedgehog](https://github.com/hedgehogqa/fsharp-hedgehog).

GalaxyCheck uses LINQ expressions to join generators and properties. In GalaxyCheck, a property _is_ a generator
of tests.

GalaxyCheck builds for net10.0 and net11.0. It runs in xunit v2, xunit v3, TUnit, NUnit, and MSTest.

[![Nuget](https://img.shields.io/nuget/v/galaxycheck)](https://www.nuget.org/packages/GalaxyCheck/)

## Packages

| Package | Use |
| --- | --- |
| [GalaxyCheck](https://www.nuget.org/packages/GalaxyCheck/) | The core library: generators, properties, and runners. Use it alone, or in any test framework. |
| [GalaxyCheck.Xunit](https://www.nuget.org/packages/GalaxyCheck.Xunit/) | The `[Property]` attribute and the other attributes for xunit v2. |
| GalaxyCheck.Xunit.V3 | The same attributes for xunit v3. |
| GalaxyCheck.TUnit | The same attributes for TUnit, with the `[Test, PropertyTest]` form. |
| GalaxyCheck.NUnit | The same attributes for NUnit. |
| GalaxyCheck.MSTest | The same attributes for MSTest, with no `[GenSnapshot]`. |
| GalaxyCheck.SourceGeneration | A source generator that writes the code of `Gen.Create<T>()` at compile time, for trimming and NativeAOT. |
| [GalaxyCheck.Xunit.CodeAnalysis](https://www.nuget.org/packages/GalaxyCheck.Xunit.CodeAnalysis/) | Roslyn analyzers for GalaxyCheck.Xunit. They find a `[MemberGen]` that names a member that does not exist or has the wrong type. |

Each framework package refers to the GalaxyCheck package. Thus, you add only one package to a test project.

## Quickstart

Add the package for xunit v2:

```shell
dotnet add package GalaxyCheck.Xunit
```

Then write a property. GalaxyCheck generates the parameters of the method, and the test passes when the
property holds for each generated value.

```csharp
using GalaxyCheck;
using System.Linq;

public class AboutSort
{
    [Property]
    public Property SortIsIdempotent() =>
        Gen.Int32().ListOf().ForAll(xs => xs.Order().SequenceEqual(xs.Order().Order()));

    [Property]
    public bool AdditionIsCommutative(int x, int y) => x + y == y + x;
}
```

To use another test framework, read [Getting started](docs/getting-started.md).

## A tour of the features

### Shrinking

When a property fails, GalaxyCheck shrinks the counterexample to a small value that still fails. Each shrunk
value obeys the constraints of its generator.

This property is wrong on purpose:

```csharp
using GalaxyCheck;

public class AboutShrinking
{
    [Property]
    public Property MyProperty() =>
        Property.ForAll(
            from x in Gen.Int32().GreaterThanEqual(1)
            from y in Gen.Int32().GreaterThanEqual(x + 5)
            select (x, y),
            input => input.x < 10 || input.y < input.x);
}
```

The value `x` is sometimes less than 10, but `y` is always at least `x + 5`. Thus, the property fails. GalaxyCheck
gives the smallest counterexample, a replay string, and a C# expression of the counterexample:

```
GalaxyCheck.Runners.PropertyFailedException :

Falsified after 11 tests (1 shrink)
Reproduction:
    [Replay("ArHH1ZcECgIJAA==")]
Counterexample:
    [0] = 10
    [1] = 15
Counterexample as C#:
    (10, 15)

Property function returned false

<galaxycheck-failure>
{"counterexample":["(Item1 = 10, Item2 = 15)"],"csharp":["(10, 15)"],"replay":"ArHH1ZcECgIJAA==","seed":1123378097,"size":10,"iterations":11,"shrinks":1,"test":"AboutShrinking.MyProperty","rerun":"dotnet test --filter FullyQualifiedName=AboutShrinking.MyProperty"}
</galaxycheck-failure>
```

The seed is random for each run. Thus, the count of tests and the replay string change from run to run. The
block of JSON at the end is for a tool or an agent. Read [Use by LLM agents](docs/agents.md).

GalaxyCheck uses integrated shrinking. Each generator knows how to shrink its values, so you do not write a
shrink function. When you join generators with LINQ, the shrinks of each generator join too.

### Composability

A custom generator is a LINQ expression. This generator makes a point:

```csharp
using GalaxyCheck;

public static class Points
{
    public static IGen<(int x, int y)> Point =>
        from x in Gen.Int32()
        from y in Gen.Int32()
        select (x, y);
}
```

To make only points with no negative coordinate, add a bound to each generator:

```csharp
using GalaxyCheck;

public static class PositivePoints
{
    public static IGen<(int x, int y)> Point =>
        from x in Gen.Int32().GreaterThanEqual(0)
        from y in Gen.Int32().GreaterThanEqual(0)
        select (x, y);
}
```

A range has a minimum and a maximum in order. The second generator reads the value of the first generator:

```csharp
using GalaxyCheck;

public static class Ranges
{
    public static IGen<(int min, int max)> Range =>
        from min in Gen.Int32().GreaterThanEqual(0)
        from max in Gen.Int32().GreaterThanEqual(min)
        select (min, max);
}
```

### Repeatability

The failure message of the shrinking example holds the attribute `[Replay("ArHH1ZcECgIJAA==")]`. Put the attribute on
the property, and the test runs only the counterexample:

```csharp
using GalaxyCheck;

public class AboutReplaying
{
    [Property]
    [Replay("ArHH1ZcECgIJAA==")]
    public Property MyProperty() =>
        Property.ForAll(
            from x in Gen.Int32().GreaterThanEqual(1)
            from y in Gen.Int32().GreaterThanEqual(x + 5)
            select (x, y),
            input => input.x < 10 || input.y < input.x);
}
```

Many frameworks give only the seed of a failure. Then the debugger stops at each test case before the
counterexample, and at each shrink. A replay string of GalaxyCheck holds the exact choices of the counterexample.
Thus, the debugger goes directly to the counterexample.

GalaxyCheck also saves each failure in the file `.galaxycheck/failures.json`, and the next run tries the failure
first. Read [Failure memory](docs/failure-memory.md).

### Transparency

The `[Sample]` attribute replaces `[Property]`. The test then fails with a list of the values that the property
receives:

```csharp
using GalaxyCheck;

public class AboutSampling
{
    [Sample]
    public Property MyProperty() =>
        Property.ForAll(
            from x in Gen.Int32().GreaterThanEqual(1)
            from y in Gen.Int32().GreaterThanEqual(x + 5)
            select (x, y),
            input => input.x < 10 || input.y < input.x);
}
```

The sample shows a problem in the test. When `x` is near `int.MaxValue`, `x + 5` overflows. Thus, the generator
of `x` needs an upper bound:

```csharp
using GalaxyCheck;

public class AboutSamplingWithABound
{
    [Sample]
    public Property MyProperty() =>
        Property.ForAll(
            from x in Gen.Int32().Between(1, int.MaxValue - 5)
            from y in Gen.Int32().GreaterThanEqual(x + 5)
            select (x, y),
            input => input.x < 10 || input.x > input.y);
}
```

### Generators for the base types

GalaxyCheck has a generator for each C# base type, for the time types, for containers, and for text. A
floating-point generator also gives the special values, for example NaN and the infinities:

```csharp
using GalaxyCheck;
using System;

public class AboutGenerators
{
    [Property]
    public Property PricesRoundTripThroughText() =>
        Gen.Decimal().Between(0m, 1000m).ForAll(price =>
            decimal.Parse(price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.InvariantCulture) == price);

    [Property]
    public Property DatesAreInRange() =>
        Gen.DateOnly().From(new DateOnly(2020, 1, 1)).ForAll(date => date.Year >= 2020);

    [Property]
    public Property CodesHaveTheFormat() =>
        Gen.StringMatching("[A-Z]{2}-[0-9]{3}").ForAll(code => code.Length == 6);
}
```

`Gen.Create<T>()` builds a generator for a record or a class from the generators of its members.

### Property features

- [Explicit examples](docs/explicit-examples.md): `[Example(...)]` gives fixed inputs that run first.
- [Exhaustive runs](docs/exhaustive-runs.md): a small input space runs each value one time.
- [Preconditions and filters](docs/preconditions-and-filters.md): `Property.Precondition` discards a test case.
- [Labels and cover](docs/labels-and-cover.md): the report gives the share of each label.
- [Health checks](docs/health-checks.md): a property that discards too much fails with a clear message.
- [Timeouts and cancellation](docs/timeouts-and-cancellation.md): a slow test case fails the property.
- [Stateful testing](docs/stateful-testing.md): sequences of commands against a model.

### Performance

The current engine is fast. In one measured run on one machine, each benchmark case ran between 142 and 1,460
times the throughput of the old engine. Read [Performance](docs/performance.md).

## Documentation

The [documentation index](docs/index.md) lists each page:

- [Getting started](docs/getting-started.md) in xunit v2, xunit v3, TUnit, NUnit, and MSTest
- [Writing properties](docs/writing-properties.md)
- [Configuration](docs/configuration.md)
- [Base types](docs/base-types.md), [time types](docs/time-types.md), [containers](docs/containers.md),
  [text and pattern strings](docs/text.md), and [realistic data](docs/realistic-data.md)
- [Recursive generators](docs/recursive-generators.md) and [automatic generation](docs/automatic-generation.md)
- [Explicit examples](docs/explicit-examples.md), [exhaustive runs](docs/exhaustive-runs.md),
  [preconditions and filters](docs/preconditions-and-filters.md), [health checks](docs/health-checks.md),
  [labels and cover](docs/labels-and-cover.md), and [timeouts and cancellation](docs/timeouts-and-cancellation.md)
- [Replay and counterexamples](docs/replay-and-counterexamples.md) and [failure memory](docs/failure-memory.md)
- [Stateful testing](docs/stateful-testing.md)
- [The source generator and NativeAOT](docs/source-generator.md)
- [Use by LLM agents](docs/agents.md)
- [Performance](docs/performance.md)
- [Migration from an older GalaxyCheck](docs/migration.md)
- [Generator reference](docs/generators.md) and the compact reference [llms.txt](docs/llms.txt)
- [Decisions](docs/decisions.md), [deprecated features](docs/deprecated-features.md), and the
  [reference suite](docs/reference-suite.md)
