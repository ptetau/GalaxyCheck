# Migration from an older GalaxyCheck

This page is for a user of an older version of GalaxyCheck: a version that built for net8.0. The documented `Gen`
and `Property` API stays the same. The changes are in the target frameworks, in the generated values, in the
replay strings, and in the advanced API.

## Target frameworks

GalaxyCheck and each framework package build for net10.0 and net11.0. The net8.0 target is gone. Move each test
project to net10.0 or net11.0.

The generators for the types that only .NET 11 has exist only on net11.0: `Gen.Decimal32()`, `Gen.Decimal64()`,
`Gen.Decimal128()`, `Gen.BFloat16()`, and `Gen.ComplexOf(...)`.

## Changed values

The same seed gives other values than in an older version. These are the causes:

- The engine changed. Read [Performance](performance.md).
- `Gen.Create<T>()`, and a `[Property]` parameter, now use a real generator for `sbyte`, `ushort`, `uint`,
  `ulong`, `float`, `double`, `decimal`, `TimeSpan`, and `DateTimeOffset`. An older version cast an integer to
  these types.

The new generators make values that an older version did not make:

- `float` and `double` parameters now get values with a fraction part. They also get the special values: NaN,
  the infinities, negative zero, the smallest positive value, the minimum, and the maximum. A property that fails on
  NaN now finds NaN. To keep only finite values, use `Gen.Double().WithoutSpecialValues()` with `[MemberGen]`.
- `decimal` parameters now get values with a fraction part, and the minimum and the maximum.
- `DateTimeOffset` parameters now get offsets other than zero.

Make the snapshots of each `[GenSnapshot]` test again, after you make sure that the new values are sensible.

## Changed replay strings

A replay string now holds the recorded choices of a test case. A replay string of an older version can fail with
an error, or it can run another test case. Remove each old `[Replay("...")]` attribute and each old `replay:`
argument. Then run the property again, and copy the new replay string from the failure message.

## Changed behaviour

- Failure memory is on. A failed test now writes `.galaxycheck/failures.json` in the folder of the test project.
  Read [Failure memory](failure-memory.md).
- Health checks are on. A property that discards more than 90% of its values now fails. Read
  [Health checks](health-checks.md).
- A small input space now runs each value one time, so a run can have fewer than the set iterations. Read
  [Exhaustive runs](exhaustive-runs.md).
- `Property.Precondition` is available again. Read [Preconditions and filters](preconditions-and-filters.md).
- The failure message has new sections: `Counterexample as C#:` and the JSON failure block. Read
  [Use by LLM agents](agents.md).
- `IGlobalConfiguration` has a new member, `FailureMemory`.

## Removed and changed API

The advanced and internal API changed. The documented API of `Gen`, of the operators, and of `Property` did not
change.

| Older API | Now |
| --- | --- |
| `IGenAdvanced<T>.Run(GenParameters)`, which returned `IGenIteration<T>` values | Removed. `IGenAdvanced<T>.Generate(GenSource)` makes one value from a source of choices. Use `Sample`, `Check`, or `Minimum`. |
| `IGenIteration<T>`, `IGenInstance<T>`, `IGenDiscard<T>`, `IGenError<T>`, and their data types | Removed. |
| `IExampleSpace<T>`, `IExample<T>`, and `ExampleId` | Removed. The engine has no example spaces. |
| `SampleExampleSpaces`, `SampleOneExampleSpace`, and `SampleExampleSpacesWithMetrics` | Removed. Use `Sample` or `SampleWithMetrics`. |
| `gen.Unfold(...)`, a custom shrink function | Removed. A generator shrinks through its choices. Build the value with LINQ from generators that shrink. |
| `IRng` | Removed. |

These types are new: `IRealGen<T>`, `IRangeGen<T>`, `IDictionaryGen<TKey, TValue>`, `IComplexGen`, `HealthCheck`,
`HealthCheckException`, `CSharpExpression`, `Gen.Realistic`, and the types of `GalaxyCheck.Stateful`.

## New packages

GalaxyCheck.Xunit stays on xunit v2. To use xunit v3, change the package to GalaxyCheck.Xunit.V3. The new
packages GalaxyCheck.TUnit, GalaxyCheck.NUnit, and GalaxyCheck.MSTest support the other frameworks. Read
[Getting started](getting-started.md).
