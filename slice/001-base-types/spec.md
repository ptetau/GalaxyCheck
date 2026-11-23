# Spec: base-types

Status: ratified

## Purpose

GalaxyCheck generates good values for each base C# type, for a user who writes property
tests. Each type has a generator that the user can configure, and the automatic generator
uses the same generator. Today many base types have no generator, or only produce whole
numbers. The user can run properties in xunit v2, xunit v3, TUnit, NUnit, and MSTest.

## Decisions

- D1 (type scope): The base types are the C# keyword types, the common System value types, and the containers: dictionaries, KeyValuePair, and tuples.
- D2 (API surface): Each type has an explicit generator on `Gen`, and `Gen.Create<T>()` and `[Property]` parameters use that generator.
- D3 (special values): A floating-point or decimal generator produces special values by default. The user can opt out.
- D4 (compatibility): The new generators replace the generators that cast an integer. The values for an existing seed or replay string can change.
- D5 (targets): GalaxyCheck and GalaxyCheck.Xunit build for net10.0 and net11.0. The net8.0 target goes. The types that only .NET 11 has are supported on net11.0 only.
- D6 (names): A generator has the name of its System type, as `Gen.Int32()` has today: for example `Gen.UInt64()`, `Gen.IntPtr()`, `Gen.Single()`.
- D7 (xunit): A new package, GalaxyCheck.Xunit.V3, supports xunit v3. The GalaxyCheck.Xunit package stays on xunit v2.
- D8 (TUnit): A new package, GalaxyCheck.TUnit, supports TUnit, with full parity to GalaxyCheck.Xunit.
- D9 (NUnit): A new package, GalaxyCheck.NUnit, supports NUnit, with full parity to GalaxyCheck.Xunit.
- D10 (quality of life): The spec also adds more collections, interfaces and base classes, pattern strings, value statistics, failure memory, copyable counterexamples, timeouts and cancellation, and stateful testing.
- D11 (performance target): Each benchmark reaches at least 100 times the baseline throughput, and at most 1% of the baseline allocation. The baseline is commit f127a67.
- D12 (performance limits): The performance work can change the values that a seed or a replay string of an older version gives, and can change the advanced and internal API. The documented `Gen` and `Property` API stays.
- D13 (testing features): The spec also adds explicit examples, recursive generators, exhaustive runs of small domains, and health checks.
- D14 (platform): The spec also adds a GalaxyCheck.MSTest package with full parity, a source generator for NativeAOT and trimming, realistic data generators, generator reference pages, and a repair of the CI workflow and the package sources.
- D15 (agent experience): LLM agents write most of the tests. The library gives them machine-readable failures, errors that state the fix, one compact reference file, and a quiet mode with a fixed seed.
- D16 (old tests): The old test projects are ported. A test of an internal part that no longer exists is deleted. A snapshot of generated values is made again, after a check that the new values are sensible. A test of public behaviour still passes.
- D17 (README examples): The code examples of README.md did not compile against the library before this spec. Slice S2 rewrote each example as a complete file against the documented API. R51 holds for the rewritten examples.
- D18 (self-testing): The tests of the library use the current library for their own property tests, as soon as the fast core exists. The old prebuilt copies of the library go.
- D19 (reference suite): The library has a reference suite of test cases that other property-based testing libraries use on themselves, with a preference for novel cases. Each case is written new from the idea. No test code is copied. `docs/reference-suite.md` names the source of each case.
- D20 (TUnit forms): TUnit finds a test only through its own `[Test]` attribute, has its own `[Property]` attribute, and refuses a data row that does not fill each parameter. So a TUnit property is written `[Test, PropertyTest]`: the TUnit package has `PropertyTestAttribute` with the options of `PropertyAttribute`, and keeps `PropertyAttribute` for a user who adds a using alias. A data row in TUnit is `[PropertyArguments(...)]`, which gives the first parameters.
- D21 (MSTest forms): MSTest finds only a test method that returns void, Task, or ValueTask. So an MSTest property is a void or Task method that asserts or throws. The MSTest package has no `[GenSnapshot]`, and R27 does not apply to it. The other attributes work as in the other packages.
- D22 (NUnit rows): An NUnit data row stays `[TestCase(...)]` and `[TestCaseSource]`, which NUnit engineers know. The package makes a row that gives only the first parameters runnable. An NUnit test class has NUnit's own `[TestFixture]`, because NUnit loads only an assembly that refers to NUnit.

## Requirements

- R1 (integers): `Gen.SByte()`, `Gen.UInt16()`, `Gen.UInt32()`, `Gen.UInt64()`, `Gen.IntPtr()`, `Gen.UIntPtr()`, `Gen.Int128()`, and `Gen.UInt128()` each produce values over the full range of the type, including its minimum and maximum.
- R2 (integer options): Each generator of R1 has the options that `Gen.Int32()` has: a lower bound, an upper bound, a shrink origin, and a bias. A value is never outside the bounds. An invalid bound gives a generator error with a message that names the option.
- R3 (integer shrinking): A failing value from a generator of R1 shrinks to the value in the bounds that is nearest to the origin. The default origin is zero, or the bound nearest to zero.
- R4 (BigInteger): `Gen.BigInteger()` produces values below `Int64.MinValue` and above `Int64.MaxValue` by default, has the options of R2, and shrinks as R3 states.
- R5 (binary floats): `Gen.Half()`, `Gen.Single()`, and `Gen.Double()` each produce values with a fraction part, negative and positive values, and values over the full finite range of the type.
- R6 (float options): Each generator of R5 and `Gen.Decimal()` has a lower bound, an upper bound, and a shrink origin. A value that is a number is never outside the bounds.
- R7 (float special values): With no option, each generator of R5 produces NaN, positive infinity, negative infinity, negative zero, the smallest positive value, the minimum, and the maximum, in the first 100 values of most seeds. A special value outside the bounds of R6 is never produced. NaN and the infinities are never produced when a bound is set.
- R8 (special value opt-out): Each generator of R5 and `Gen.Decimal()` has an option that removes the special values. With the option, a generator of R5 produces only finite values.
- R9 (float shrinking): A failing value from a generator of R5 or from `Gen.Decimal()` shrinks to the simplest value that still fails. Zero is simpler than a whole number, and a whole number is simpler than a value with a fraction part. A special value shrinks to a finite value if a finite value fails.
- R10 (decimal): `Gen.Decimal()` produces values with a fraction part, negative and positive values, and values over the full range of the type. Its special values are zero, the minimum, and the maximum.
- R11 (time types): `Gen.TimeSpan()`, `Gen.DateTimeOffset()`, `Gen.DateOnly()`, and `Gen.TimeOnly()` each produce values over the full range of the type, have a lower and an upper bound, and shrink toward a documented origin. `Gen.DateTimeOffset()` produces offsets other than zero.
- R12 (Rune): `Gen.Rune()` produces only valid Unicode scalar values, with values inside and outside the Basic Multilingual Plane, and shrinks toward `'a'`.
- R13 (Complex): `Gen.Complex()` produces `System.Numerics.Complex` values whose real and imaginary parts come from `Gen.Double()`, and passes the opt-out of R8 to both parts.
- R14 (dictionaries): A dictionary generator takes a key generator and a value generator. It has a minimum count, a maximum count, and an exact count. The keys of a value are distinct. It shrinks the count first, then the keys and the values.
- R15 (pairs and tuples): There is a generator for `KeyValuePair<TKey, TValue>`, for `ValueTuple` with 1 to 7 elements, and for `Tuple` with 1 to 7 elements. Each takes one generator for each element, and shrinks each element.
- R16 (automatic generation): `Gen.Create<T>()` and a `[Property]` parameter of type T work for each type of R1, R4, R5, R10, R11, R12, and R13, for `Dictionary<,>`, `IDictionary<,>`, `IReadOnlyDictionary<,>`, `ImmutableDictionary<,>`, `KeyValuePair<,>`, and for `ValueTuple` and `Tuple` with 1 to 7 elements. The values come from the explicit generator with no option.
- R17 (replaced generators): `Gen.Create<T>()` for `sbyte`, `ushort`, `uint`, `ulong`, `float`, `double`, `decimal`, `TimeSpan`, and `DateTimeOffset` uses the generator of this spec, not a cast of an integer.
- R18 (.NET 11 types): On net11.0, `Gen.Decimal32()`, `Gen.Decimal64()`, `Gen.Decimal128()`, `Gen.BFloat16()`, and a generator for `Complex<T>` exist, and R5 to R9 and R16 hold for them. On net10.0 these members do not exist.
- R19 (targets): The GalaxyCheck and GalaxyCheck.Xunit packages each hold a net10.0 and a net11.0 assembly, and no net8.0 assembly. The CI workflow builds and tests both targets.
- R20 (same values on both targets): For a type that both targets have, a generator with the same seed and size gives the same values on net10.0 and on net11.0.
- R21 (replay): A replay string of a failing property that uses a generator of this spec reproduces the same counterexample.
- R22 (framework packages): GalaxyCheck.Xunit.V3, GalaxyCheck.TUnit, GalaxyCheck.NUnit, and GalaxyCheck.MSTest each have `[Property]`, `[Sample]`, `[GenSnapshot]`, `[Replay]`, `[MemberGen]`, and the global configuration, with the names and the options that GalaxyCheck.Xunit has. Each package holds a net10.0 and a net11.0 assembly.
- R23 (property result): In xunit v3, TUnit, NUnit, and MSTest, a `[Property]` test that holds is one passed test. A property that is falsified is one failed test. Its message holds the shrunk counterexample and the replay string, in the text that GalaxyCheck.Xunit gives.
- R24 (property forms): In xunit v3, TUnit, NUnit, and MSTest, `[Property]` accepts each method form that GalaxyCheck.Xunit accepts: a void, a boolean, a `Property`, and a generator return, the async forms of each, and parameters that R16 generates.
- R25 (replay and member generators): In xunit v3, TUnit, NUnit, and MSTest, `[Replay]` runs only the replayed example, and `[MemberGen]` takes the generator of a parameter from the named member. An invalid replay string or member is one failed test with the error text that GalaxyCheck.Xunit gives.
- R26 (sample): In xunit v3, TUnit, NUnit, and MSTest, a `[Sample]` test fails and its message lists the sampled values, as in GalaxyCheck.Xunit.
- R27 (generator snapshot): In xunit v3, TUnit, NUnit, and MSTest, a `[GenSnapshot]` test compares the values of each seed with the stored snapshot, and fails on a difference, as in GalaxyCheck.Xunit.
- R28 (data rows): In xunit v3, a `[Property]` with `[InlineData]` or `[MemberData]` runs one property for each row. In TUnit, a `[PropertyTest]` with `[PropertyArguments]` runs one property for each row. In NUnit, a `[Property]` with `[TestCase]` or `[TestCaseSource]` runs one property for each row. In MSTest, a `[Property]` with `[DataRow]` or `[DynamicData]` runs one property for each row. The row gives the first parameters, and R16 generates the rest.
- R29 (global configuration): In xunit v3, TUnit, NUnit, and MSTest, the global configuration of an assembly sets the default iterations, the default generators, and the snapshot settings, as in GalaxyCheck.Xunit.
- R30 (xunit v2 unchanged): The GalaxyCheck.Xunit package still runs each test of its test project with the same result.
- R31 (more collections): `Gen.Create<T>()` works for `Queue<>`, `Stack<>`, `SortedSet<>`, `LinkedList<>`, `ImmutableArray<>`, `ReadOnlyCollection<>`, `Memory<>`, and `ReadOnlyMemory<>`. Each shrinks as a list shrinks.
- R32 (interfaces and base classes): `Gen.Create<T>()` for an interface or an abstract class produces instances of the concrete types in the assembly of T that are assignable to T. Each such type appears in the values. With no such type, the result is a generator error that names T. The user can give the list of candidate types.
- R33 (unions): On net11.0, `Gen.Create<T>()` for a C# union type produces each case of the union.
- R34 (pattern strings): A string generator takes a regular expression, and each value matches it. A failing value shrinks to a shorter or simpler string that matches. A construct that the generator does not support gives a generator error that names the construct.
- R35 (Uri, Version, email): `Gen.Uri()` produces different absolute http and https URIs. `Gen.Version()` produces `System.Version` values with 2 to 4 parts. An email generator produces strings with one `@`, a local part, and a domain with a dot. `Gen.Create<T>()` uses the first two for `Uri` and `Version`.
- R36 (labels): A property can give one or more labels to a test case. The report of a passed and of a failed property lists each label with the share of test cases that have it.
- R37 (cover): A property can require a minimum share for a label. If the share is lower after the last iteration, the property fails, and the message names the label, the required share, and the actual share.
- R38 (failure memory): In each of the five frameworks, a failed property saves its replay string in a file in a documented folder of the test project, under the full name of the test. The next run of that test tries the saved replay first. A saved replay that passes is removed. `[Replay]` on the test takes precedence. The feature is on by default, and the global configuration can switch it off.
- R39 (copyable counterexamples): The failure report holds each counterexample value as a C# expression. The expression compiles and equals the value for the base types of this spec, strings, enums, collections, and types with a public constructor or settable properties. For any other value the report holds the text that it holds today.
- R40 (timeout): A property takes a time limit, as an option of `[Property]` and of the check and assert methods. When one test case runs over the limit, the property fails, and the message names the limit and the input of that test case.
- R41 (cancellation): An async property with a `CancellationToken` parameter receives a token that the library does not generate. The token is cancelled when the time limit of R40 passes, and when the test framework cancels the test.
- R42 (commands): The user defines a command with a precondition on a model, an action on the real system, a change to the model, and a postcondition. A generator produces sequences of commands in which each precondition holds at its position.
- R43 (stateful property): A stateful property runs each sequence on a new system and a new model. It fails when a postcondition fails or an action throws an exception that the command does not expect.
- R44 (stateful shrinking): A failing sequence shrinks to a shorter sequence that still fails, and each precondition still holds in the shrunk sequence. The inputs of each remaining command shrink too.
- R45 (stateful report): The failure report of a stateful property lists the commands of the shrunk sequence in order, with the inputs of each, and names the command that failed.
- R46 (benchmark suite): A benchmark suite measures the values for each second and the bytes allocated for each value, for these cases: check a property of an int, sample an int, sample a string, sample a list of ints, sample a record with four members from `Gen.Create<T>()`, and find and shrink a failing list of ints. One command runs the suite.
- R47 (baseline in the same run): The suite runs each case on the library at commit f127a67 and on the current library, on the same machine in the same run, and reports the ratio of the two for throughput and for allocation.
- R48 (throughput): For each case of R46, the throughput of the current library is at least 100 times the throughput of the baseline.
- R49 (allocation): For each case of R46, the bytes for each value of the current library are at most 1% of the bytes of the baseline.
- R50 (performance gate): A test fails when a case of R46 is below the target of R48 or above the target of R49, and its message names the case and the measured ratio.
- R51 (documented API stays): Each code example in README.md compiles and runs against the current library with no change, apart from the values that it prints.
- R52 (explicit examples): In each of the five frameworks, a property can have one or more `[Example]` attributes with fixed inputs. Each example runs before the generated test cases. A failing example fails the test, the message holds the inputs of the example, and the example does not shrink. An example with the wrong number or type of inputs is one failed test with a message that names the parameter.
- R53 (recursive generators): A helper builds a generator for a recursive type from a generator of leaves and a function that wraps a generator of smaller values. The depth of a value is never above a limit that the user can set. A failing value shrinks toward a leaf.
- R54 (recursive automatic generation): `Gen.Create<T>()` for a type that refers to itself ends, and no value is deeper than the limit of R53.
- R55 (exhaustive small domains): When the inputs of a property have at most as many different values as the property has iterations, the property tries each value one time, and the report says that the run was exhaustive. This holds for `bool`, enums, bounded integers, element choices, and tuples of these. An option switches it off.
- R56 (health checks): A property fails with a named health check when more than 90% of the generated values are discarded, when a second value of the run takes longer than one second to generate (one slow value can be a pause of the machine), or when no test case reaches the test function. The message names the check and how to suppress it. The user can suppress each check by name.
- R57 (source generator): A source generator package produces the code of `Gen.Create<T>()` at compile time for each type that a test project asks for. A test project that is published with trimming and with NativeAOT runs its properties with no trim warning and no AOT warning from a GalaxyCheck package.
- R58 (source generator parity): For a type that both paths support, the generated code and the reflection path give the same values for the same seed. For a type that the source generator does not support, the compiler reports a diagnostic that names the type.
- R59 (realistic data): Generators exist for a first name, a last name, a full name, a street address, a city, a country, a phone number, and a company name. The values come from lists in the library, in English. A failing value shrinks toward the first entry of its list.
- R60 (reference pages): `docs/` holds a reference entry with one code example for each public generator method. A test fails when a public generator method has no entry.
- R61 (CI): The CI workflow uses only action versions that GitHub supports, and builds and tests each target of R19 on Linux, Windows, and macOS.
- R62 (package sources): The solution restores from nuget.org and from a folder in the repository only. No restore reads the MyGet feed.
- R63 (machine-readable failures): The failure message of a property, from the assert methods and in each of the five frameworks, holds one JSON object between the lines `<galaxycheck-failure>` and `</galaxycheck-failure>`. The object holds the counterexample values as text, the C# expression of each value, the replay string, the seed, the size, the iterations, and the shrinks. In a framework it also holds the full name of the test and one command line that runs only that test.
- R64 (errors that state the fix): The message of each generator error, and of each error of a `[Replay]`, `[MemberGen]`, or `[Example]` attribute, ends with a line that starts with `Fix:` and gives a corrected call or the option to change.
- R65 (agent reference file): The file `docs/llms.txt` lists each public generator method, each option of a generator, each property option, and each attribute, with a one-line example for each, in at most 40,000 characters. Each package holds the file. Each public type and member of each package has an XML documentation summary, and each package holds the XML documentation file.
- R66 (quiet mode with a fixed seed): With the environment variable `GALAXYCHECK_AGENT=1`, a property with no seed option uses seed 0, a passed property writes no output, and the failure message holds at most 40 lines and still holds the block of R63. The environment variable `GALAXYCHECK_SEED` sets the seed of each property with no seed option.
- R67 (self-testing): No project of the solution refers to a `NebulaCheck` or `GalaxyCheck.Stable` package, and no test file uses their namespaces. Each property test of the old test projects runs on the current library through a project reference. Each old test project uses the same test runner version as the other test projects. The folder `packages-local` holds no package of an old copy of the library.
- R68 (generator laws): For each public generator of this spec, these laws hold over at least 20 seeds: the same seed and size give the same values; a failing property ends its shrinking; no value appears twice in the sequence of accepted shrinks; the shrunk value still fails; and a second run that starts from the replay of the shrunk value makes no further shrink.
- R69 (shrinking challenge): For the challenges reverse, lengthlist, nestedlists, deletion, distinct, coupling, difference (its three tests), large union list, and bound5 of the shrinking challenge, the counterexample is the best known minimum on each of the seeds 0 to 19. For binheap and calculator, the counterexample is no larger than the best known minimum on each of those seeds.
- R70 (shrink budget): When the first failing value of a property is already the minimum, shrinking calls the test function at most 50 more times.
- R71 (distribution): A chi-squared check at the 0.001 level passes for `Gen.Boolean()`, `Gen.Element` of 10 values, and each bounded integer generator with no bias over 10 buckets, with 10,000 values each. The same check fails for a generator that never gives one of its buckets. Each integer generator with no option gives its minimum and its maximum at least one time in 2,000 values on each of 20 seeds.
- R72 (replay laws): A replay of a counterexample runs one test case and makes no shrink. A replay string from a property with another generator gives the error that the replay is no longer valid. A replay string that is cut short or changed gives an error. None of these crashes or hangs.
- R73 (operator laws): For a seed, `Select` keeps the identity and composition laws, and `SelectMany` keeps the left identity, right identity, and associativity laws, on the generated values and on the shrunk counterexample.
- R74 (random programs): A generator that builds random compositions of the generators and operators of the library (select, filter, bind, list, tuple, choice) never makes the engine crash or hang: each run of such a composition ends inside a time limit, and each failure shrinks and replays.
- R75 (runner equivalence): For a seed, a property and its async form give the same iterations and the same counterexample. Two runs in one process and two runs in two processes give the same result.
- R76 (exhaustive laws): An exhaustive run gives each value of the domain one time through `Select`, through a filter, and through `SelectMany`. A domain that is too large runs the normal number of random test cases.
- R77 (bug-finding table): A sample binary search tree has eight versions, each with one planted bug. The five property kinds of "How to Specify It" (validity, postcondition, metamorphic, inductive, and model-based) are written with the library. Each planted bug is found by at least one property with the default iterations on each of the seeds 0 to 9, and no property fails on the correct tree.

## Out of scope

- Realistic data in a language other than English.
- `object`, `dynamic`, pointer types, and `Span<T>`.
- Changes to the generators that exist today for `bool`, `byte`, `short`, `int`, `long`, `char`, `string`, `Guid`, `DateTime`, enums, lists, and sets.
- The Roslyn analyzer package and the Visual Studio extension.
- Stateful tests that run commands in parallel.
- A move of the GalaxyCheck.Xunit package to xunit v3, and a Roslyn analyzer for the four new packages.
