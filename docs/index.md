# GalaxyCheck documentation

This index lists each page of the documentation. Start with [Getting started](getting-started.md) and
[Writing properties](writing-properties.md).

## Start

- [Getting started](getting-started.md): the packages, and a first property in xunit v2, xunit v3, TUnit, NUnit,
  and MSTest.
- [Writing properties](writing-properties.md): `Property.ForAll`, `Check`, `Assert`, and the options of a run.
- [Configuration](configuration.md): the global defaults of a test assembly, and generator snapshots.

## Generators

- [Base types](base-types.md): integers, floating-point numbers, decimals, complex numbers, and the other base
  types.
- [Time types](time-types.md): `TimeSpan`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, and `DateTime`.
- [Containers](containers.md): lists, sets, dictionaries, pairs, and tuples.
- [Text and pattern strings](text.md): strings, regular expressions, URIs, versions, and email addresses.
- [Realistic data](realistic-data.md): names, addresses, phone numbers, and companies.
- [Recursive generators](recursive-generators.md): trees and other types that hold themselves.
- [Automatic generation](automatic-generation.md): `Gen.Create<T>()`, factories, interfaces, and abstract classes.
- [Generator reference](generators.md): one entry and one example for each generator.

## Property features

- [Explicit examples](explicit-examples.md): fixed inputs with `[Example]`.
- [Exhaustive runs](exhaustive-runs.md): each value of a small input space, one time.
- [Preconditions and filters](preconditions-and-filters.md): `Property.Precondition` and `Where`.
- [Health checks](health-checks.md): the checks that fail a property that wastes its test cases.
- [Labels and cover](labels-and-cover.md): the distribution of the test cases.
- [Timeouts and cancellation](timeouts-and-cancellation.md): time limits, and the `CancellationToken` of a test.
- [Replay and counterexamples](replay-and-counterexamples.md): replay strings, and counterexamples as C#.
- [Failure memory](failure-memory.md): the file `.galaxycheck/failures.json`.
- [Stateful testing](stateful-testing.md): sequences of commands against a model.

## Tools and platforms

- [The source generator and NativeAOT](source-generator.md): generated code for `Gen.Create<T>()`, and the
  diagnostics GCSG001 (no generated code for a type) and GCSG002 (a call that uses reflection).
- [Use by LLM agents](agents.md): the JSON failure block, `Fix:` lines, `llms.txt`, and the quiet mode.
- [llms.txt](llms.txt): a compact reference of each generator, option, and attribute.
- [Performance](performance.md): the engine, and the measured numbers.
- [Migration from an older GalaxyCheck](migration.md): changed values, replay strings, and API.

## Background

- [Decisions](decisions.md): the design decisions of the library.
- [Deprecated features](deprecated-features.md): the features that the library removed, and why.
- [Reference suite](reference-suite.md): the sources of the test cases that the library uses on itself.
