# Interface

Status: approved

Stack: C# libraries. The tests call public types of the `GalaxyCheck` packages, run commands, and read files.

## Surface

The test writer sees only this file, not the code. Give it all that it must know to call the surface.

The stubs fail with `System.NotImplementedException`. The message holds "not implemented".

`Status: draft` becomes `Status: approved` only after the user approves the surface, before the test writer runs.

### Existing surface that the tests use

- `using GalaxyCheck; using GalaxyCheck.Gens;`
- `gen.Sample(iterations: n, seed: s, size: z)`: returns `List<T>`. `gen.SampleOne(seed, size)`.
- `gen.Minimum(seed: s, pred: x => ...)`: returns the shrunk value for which `pred` is true. With no `pred`, each value counts.
- `Property.ForAll(gen, x => bool)` and `Property.ForAll(gen, x => { })`: return `Property<T>`. `Property.ForAllAsync(gen, async x => ...)`: returns `AsyncProperty<T>`.
- `property.Check(iterations, seed, size, shrinkLimit, replay)`: returns `CheckResult<T>` with `Falsified` (bool), `Counterexample` (null, or `.Value` and `.Replay`), and `Iterations`. `property.Assert(...)` throws `GalaxyCheck.Runners.PropertyFailedException` on a failure. `CheckAsync` and `AssertAsync` are the async forms.
- `Gen.Create<T>()`, `Gen.Factory().RegisterType<T>(gen).Create<T>()`.
- A generator error: the run throws `GalaxyCheck.Exceptions.GenErrorException`. `Exceptions` is a class, so write the full name and no `using` for it. Exposes: PropertyRun.X3.
- `gen.Where(pred)`, `gen.Select(f)`, `Gen.Int32()`, `Gen.Boolean()`, `Gen.Enum<T>()`, `Gen.Element(values)`.

### Integer generators (R1 to R4)

- `Gen.SByte()`, `Gen.UInt16()`, `Gen.UInt32()`, `Gen.UInt64()`, `Gen.IntPtr()`, `Gen.UIntPtr()`, `Gen.Int128()`, `Gen.UInt128()`, `Gen.BigInteger()`: each returns `IIntGen<T>` for its type (`nint` for `IntPtr`, `System.Numerics.BigInteger`). Exposes: NumericGenerator.
- `IIntGen<T>`: `GreaterThanEqual(T min)`, `LessThanEqual(T max)`, `ShrinkTowards(T origin)`, `WithBias(Gen.Bias bias)`. Extensions for each `T`: `Between(T x, T y)` (both inclusive), `GreaterThan(T)`, `LessThan(T)`.
- Errors: a minimum above the maximum gives a `GenErrorException` whose message holds `'min' cannot be greater than 'max'`. An origin outside the bounds gives a message that holds `'origin'`.
- `Gen.BigInteger()` with no bound has the range from -(2^127) to 2^127.

### Real number generators (R5 to R10, R13, R18)

- `Gen.Half()`, `Gen.Single()`, `Gen.Double()`, `Gen.Decimal()`: each returns `IRealGen<T>`. Exposes: NumericGenerator, SpecialValues.
- `IRealGen<T>`: `GreaterThanEqual(T min)`, `LessThanEqual(T max)`, `Between(T x, T y)`, `ShrinkTowards(T origin)`, `WithoutSpecialValues()`. Errors as for the integers.
- `Gen.Complex()`: returns `IComplexGen` (`IGen<System.Numerics.Complex>`) with `WithoutSpecialValues()`.
- On net11.0 only: `Gen.Decimal32()`, `Gen.Decimal64()`, `Gen.Decimal128()`, `Gen.BFloat16()` return `IRealGen<T>`. `Gen.ComplexOf<T>(IGen<T> real, IGen<T> imaginary)` returns `IGen<System.Numerics.Complex<T>>`.

### Time, Rune, and text generators (R11, R12, R34, R35, R59)

- `Gen.TimeSpan()`, `Gen.DateTimeOffset()`, `Gen.DateOnly()`, `Gen.TimeOnly()`: each returns `IRangeGen<T>` with `From(T min)`, `Until(T max)`, and `Within(T x, T y)`, all inclusive. The origins are `TimeSpan.Zero`, `2000-01-01T00:00:00+00:00`, `2000-01-01`, and `00:00`. With bounds that exclude the origin, the origin is the bound nearest to it.
- `Gen.Rune()`: returns `IGen<System.Text.Rune>`.
- `Gen.StringMatching(string pattern)`: returns `IGen<string>`. Errors: a construct that it does not support gives a `GenErrorException` whose message names the construct, for example `lookbehind`, `lookahead`, or `backreference`. Exposes: PatternGenerator.
- `Gen.Uri()`: `IGen<System.Uri>`. `Gen.Version()`: `IGen<System.Version>`. `Gen.Email()`: `IGen<string>`.
- `Gen.Realistic.FirstName()`, `LastName()`, `FullName()`, `StreetAddress()`, `City()`, `Country()`, `PhoneNumber()`, `CompanyName()`: each returns `IGen<string>`. `Gen.Realistic.FirstNames` and `Gen.Realistic.LastNames` are `IReadOnlyList<string>`. Exposes: DataList.

### Container generators (R14, R15, R31, R53)

- `Gen.Dictionary(IGen<TKey> keyGen, IGen<TValue> valueGen)`: returns `IDictionaryGen<TKey, TValue>`, an `IGen<IReadOnlyDictionary<TKey, TValue>>`, with `WithCount(int)`, `WithCountGreaterThanEqual(int)`, `WithCountLessThanEqual(int)`, and `WithCountBetween(int, int)`. Errors: the count errors of the list generator.
- `Gen.KeyValuePair(keyGen, valueGen)`: `IGen<KeyValuePair<TKey, TValue>>`.
- `Gen.ValueTuple(g1)` to `Gen.ValueTuple(g1, ..., g7)`, and `Gen.Tuple(g1)` to `Gen.Tuple(g1, ..., g7)`.
- `Gen.Recursive<T>(IGen<T> leafGen, Func<IGen<T>, IGen<T>> wrap, int maxDepth = 5)`: returns `IGen<T>`. A leaf has depth 0. Exposes: RecursiveGenerator.

### Automatic generation (R16, R17, R31, R32, R33, R54)

- `Gen.Create<T>()` works for each type of the spec. The depth limit for a type that refers to itself is 5.
- `Gen.Factory().RegisterImplementations<T>(params Type[] candidateTypes).Create<T>()`: sets the candidate types of an interface or an abstract class.
- Errors: an interface or abstract class with no concrete type gives a `GenErrorException` whose message holds the full name of the type.

### Properties (R36, R37, R39, R40, R41, R55, R56)

- `property.Label(string label, Func<T, bool> when)` and `property.Cover(string label, double minPercent, Func<T, bool> when)`: return the property. `Cover` also gives the label.
- `CheckResult<T>.Labels`: `IReadOnlyDictionary<string, double>`, the share of each label as a percentage. `CheckResult<T>.Exhaustive`: bool. `CheckResult<T>.Report`: the text of the run. A passed run lists each label as `<share>% <label>` and holds the word `Exhaustive` for an exhaustive run.
- A cover rule that is not met: `Check` gives `Falsified` true, and `Assert` throws `PropertyFailedException` whose message holds the label, the required share as `50%`, and the actual share.
- New optional arguments of `Check`, `Assert`, `CheckAsync`, and `AssertAsync`: `timeout: TimeSpan?`, `exhaustive: bool = true`, `suppressHealthChecks: HealthCheck[]?`.
- A timeout: `Falsified` is true, and the message of `Assert` holds `Timed out after 100 ms` (with the limit) and the input.
- `Property.ForAllAsync(gen, async (x, cancellationToken) => ...)`: the token is cancelled at the time limit.
- `enum GalaxyCheck.HealthCheck { TooManyDiscards, SlowGenerator, TestFunctionNeverRuns }`. A tripped check: `Check` and `Assert` throw `GalaxyCheck.HealthCheckException` with a `Check` property. The message holds the name of the enum member and `suppressHealthChecks`.
- `GalaxyCheck.CSharpExpression.TryFormat(object? value, out string expression)`: returns false for a value with no expression. The failure message of `Assert` holds a section `Counterexample as C#:` with the expression of each value, or the old text.
- Exposes: Property, PropertyRun, Report.

### Stateful testing (R42 to R45)

- `using GalaxyCheck.Stateful;`
- `abstract class Command<TSystem, TModel>`: `virtual bool Precondition(TModel model)` (default true), `abstract void Run(TSystem system)`, `abstract TModel NextModel(TModel model)`, `virtual bool Postcondition(TSystem system, TModel modelBefore, TModel modelAfter)` (default true), `virtual bool Expects(Exception exception)` (default false), and `ToString()` for the report.
- `StatefulGen.Commands<TSystem, TModel>(Func<TModel> newModel, Func<TModel, IGen<Command<TSystem, TModel>>> nextCommand)`: returns `IGen<IReadOnlyList<Command<TSystem, TModel>>>`.
- `StatefulProperty.Create<TSystem, TModel>(Func<TSystem> newSystem, Func<TModel> newModel, Func<TModel, IGen<Command<TSystem, TModel>>> nextCommand)`: returns `Property<IReadOnlyList<Command<TSystem, TModel>>>`.
- The failure message lists the commands as `1. <ToString()>`, one line each, and marks the failed command with `<- failed`.
- Exposes: Command, StatefulRun.

### Framework packages (R22 to R30, R38, R52)

- Projects: `src/GalaxyCheck.Xunit` (xunit 2.9.3), `src/GalaxyCheck.Xunit.V3` (xunit.v3 4.0.3), `src/GalaxyCheck.TUnit` (TUnit 1.87.12), `src/GalaxyCheck.NUnit` (NUnit 5.0.0), `src/GalaxyCheck.MSTest` (MSTest 4.5.3). A test project uses one of them through a `ProjectReference`.
- Each package has these types in the namespace `GalaxyCheck`: `PropertyAttribute` (`Iterations`, `ShrinkLimit`, `Seed`, `Size`, `TimeoutMilliseconds`, `Exhaustive`), `SampleAttribute`, `GenSnapshotAttribute` (`Iterations`, `Size`), `ReplayAttribute(string replay)`, `MemberGenAttribute(string memberName)`, `ExampleAttribute(params object?[] inputs)`, and `SuppressHealthCheckAttribute(params HealthCheck[] checks)`.
- TUnit only (D20): a property is written `[Test, PropertyTest]`, with TUnit's own `[Test]` attribute. `PropertyTestAttribute` has the options of `PropertyAttribute`. `[Sample]` and `[GenSnapshot]` also need `[Test]` beside them. A data row is `[PropertyArguments(params object?[] values)]`: each attribute is one row for the first parameters. `PropertyAttribute` still exists in the TUnit package; a file that uses it needs `using PropertyAttribute = GalaxyCheck.PropertyAttribute;`, because TUnit imports its own attribute of that name.
- xunit v3 sample projects: a project file sets `<IsTestingPlatformApplication>false</IsTestingPlatformApplication>` and `<UseMicrosoftTestingPlatformRunner>false</UseMicrosoftTestingPlatformRunner>`, and refers to `xunit.runner.visualstudio`, so that `dotnet test --logger trx` runs it.
- Global configuration: a class that implements `GalaxyCheck.Configuration.IConfigureGlobal` with `void Configure(IGlobalConfiguration instance)`. `instance.Properties.DefaultIterations`, `instance.Properties.DefaultShrinkLimit`, `instance.GenSnapshots.DefaultIterations`, `instance.FailureMemory.Enabled` (default true).
- A property method returns `void`, `bool`, `Property`, `IGen<Test>`, or `Task`, `Task<bool>`, `AsyncProperty`, `IGen<AsyncTest>`. A `CancellationToken` parameter is not generated.
- Run a test project and read its results: for xunit v2, xunit v3, NUnit, and MSTest, `dotnet test <project> --logger "trx;LogFileName=<file>"`. For TUnit, `dotnet run --project <project> -- --report-trx --results-directory <folder>`. Each result is a `UnitTestResult` element with `testName`, `outcome`, and a message.
- Failure memory: the file `.galaxycheck/failures.json` in the folder of the test project. It is a JSON object: the key is the full name of the test, and the value is the replay string.
- Exposes: FrameworkAdapter, GlobalConfiguration, SavedFailure.

### Source generator (R57, R58)

- Project `src/GalaxyCheck.SourceGeneration`, used as an analyzer reference. With it, `Gen.Create<T>()` for a type that the compiler knows uses generated code.
- A type with no support: the compiler reports the diagnostic `GCSG001`, and its message holds the name of the type.
- Publish check: `dotnet publish <project> -r linux-x64 -p:PublishAot=true -p:TreatWarningsAsErrors=false`. The output holds no line with both `warning IL` and `GalaxyCheck`.

### Benchmarks and gate (R46 to R50)

- Command: `dotnet run -c Release --project src/GalaxyCheck.Benchmarks -- --report <path>`. It writes a JSON file: `{ "baseline": "f127a67", "cases": [ { "name": "check int", "baseline": { "valuesPerSecond": 1.0, "bytesPerValue": 1.0 }, "current": { "valuesPerSecond": 1.0, "bytesPerValue": 1.0 }, "throughputRatio": 1.0, "allocationRatio": 1.0 } ] }`.
- Case names: `check int`, `sample int`, `sample string`, `sample list of ints`, `sample record`, `shrink list of ints`.
- `throughputRatio` is current over baseline for `valuesPerSecond`. `allocationRatio` is current over baseline for `bytesPerValue`.
- Gate: `dotnet run -c Release --project src/GalaxyCheck.Benchmarks -- --gate <path>` reads a report. It exits 0 when each `throughputRatio` is at least 100 and each `allocationRatio` is at most 0.01. Else it exits 1 and prints one line for each failing case, with the case name and the measured ratio.
- Exposes: BenchmarkCase, PerformanceGate.

### Build, packages, docs, and CI (R19, R20, R51, R60, R61, R62)

- `dotnet pack src/<Project> -c Release -o <folder>` writes `<Project>.<version>.nupkg`. A package is a zip file with `lib/<target>/` folders.
- Each library builds for `net10.0` and `net11.0`. A test runs code on one target with `dotnet run -f <target>` or `dotnet test -f <target>`.
- `nuget.config` at the root: an XML file with `packageSources`. The local source is the folder `packages-local`.
- `.github/workflows/dotnet.yml`: the CI workflow.
- `docs/generators.md`: one `### Gen.<Name>` heading for each public generator method of `Gen` and of `Gen.Realistic`, followed by one fenced `csharp` code block.
- `README.md`: each fenced `csharp` block is a complete example for a file with `using GalaxyCheck;`.

### Agent experience (R63 to R66)

- The message of `PropertyFailedException`, and the failure message of a `[Property]` test in each framework, holds a line `<galaxycheck-failure>`, then one JSON object, then a line `</galaxycheck-failure>`.
- The JSON object: `{ "counterexample": ["10"], "csharp": ["10"], "replay": "...", "seed": 0, "size": 0, "iterations": 31, "shrinks": 5 }`. In a framework it also has `"test"` (the full name of the test) and `"rerun"` (one command line that runs only that test). A value with no C# expression has `null` in `csharp`.
- The message of each `GenErrorException`, and of each error of `[Replay]`, `[MemberGen]`, and `[Example]`, ends with a line that starts with `Fix: `.
- `docs/llms.txt`: plain text, at most 40,000 characters. Each generator method of `Gen` and `Gen.Realistic`, each option method of a generator interface, each new argument of `Check`, and each attribute appears with its name and a one-line example. Each package holds `llms.txt` at its root and an XML documentation file next to each assembly in `lib/<target>/`.
- Environment variables: `GALAXYCHECK_AGENT=1` gives seed 0 to a property with no seed, no output for a passed property, and a failure message of at most 40 lines. `GALAXYCHECK_SEED=<n>` gives the seed n to each property with no seed option. A seed option of the property wins over both.
- Exposes: AgentMode, FailureRecord, ReferenceEntry.

- The tests folder is the xunit 2.9.3 project `src/GalaxyCheck.Tests.Slice`. It runs on net11.0, and it references `GalaxyCheck` and `GalaxyCheck.Xunit`. FluentAssertions 7.2.2 and `Microsoft.CodeAnalysis.CSharp.Scripting` are available. Do not change the project file or `TestSupport.cs`.
- A precondition: a test function calls `Property.Precondition(bool condition)`. A false condition discards the test case.
- The tests of slice Sn go in files named `Sn_<Topic>Tests.cs`, in the namespace `GalaxyCheck.Tests.Slice`. Each test is a `[Fact]` method named `test_001_Sn_Rn_<what>`.
- A sample project that a test runs goes in `samples/Sn/<Name>/`. It refers to a library with a relative `ProjectReference`, for example `..\..\..\..\GalaxyCheck.TUnit\GalaxyCheck.TUnit.csproj`. A sample test project targets `net10.0`.
- `Repo.Root`, `Repo.Src`, `Repo.Samples`: paths.
- `Cli.Dotnet(arguments, workingDirectory)` and `Cli.Run(fileName, arguments, workingDirectory)`: return `CliResult(ExitCode, Output)`.
- `Trx.RunProject(projectFolder, framework)`: runs a sample test project, with the framework `"xunit2"`, `"xunit3"`, `"nunit"`, `"mstest"`, or `"tunit"`. It returns `Results` (each with `TestName`, `Outcome`, `Message`) and `Run` (the `CliResult`).
- A test that runs a command puts the `Output` of the command in its failure message, for example with the `because` argument of FluentAssertions.
- A command that is slow, for example the benchmark command, runs one time for a test class, in a static `Lazy<T>`.
- To check that the tests of slice Sn compile: `dotnet build src/GalaxyCheck.Tests.Slice --no-dependencies -p:SliceOnly=Sn -p:SliceWork=/tmp/slice-work/Sn/`.
- To run code on net10.0, use a sample console project with `<TargetFrameworks>net10.0;net11.0</TargetFrameworks>` and `dotnet run -f net10.0`.

## Security

- Input validation: each generator option checks its arguments and gives a `GenErrorException`, not a wrong value or an endless loop. A regular expression with a construct that has no support is refused.
- Denial of service: `Gen.StringMatching` builds strings from the pattern and never runs a match of a user pattern against untrusted input with no time limit. An unbounded repeat (`*`, `+`) has a size limit.
- Injection: `CSharpExpression.TryFormat` escapes each string and char, so that the expression cannot hold code from a value.
- File access: the failure memory reads and writes only `.galaxycheck/failures.json` in the folder of the test project. A test name is a JSON key, never a path. A replay string that does not decode is an error, and is never run as code.
- Deserialization: the failure file and the benchmark report are read as plain JSON data, with no type names.
- Secrets: no report, message, or file holds an environment variable or a machine path that the user did not give.

## S1

- Uses: `dotnet pack`, `nuget.config`, `.github/workflows/dotnet.yml`; the benchmark command and its report
- Stubs: `src/GalaxyCheck.Benchmarks/Program.cs`
- Changed: R19, R61, R62

## S2

- Uses: the benchmark report, cases `check int` and `sample int`; the benchmark report, cases `sample string` and `sample list of ints`; the benchmark report, case `sample record`; the benchmark report, case `shrink list of ints`; the gate command, `README.md`
- Stubs: `src/GalaxyCheck.Benchmarks/Program.cs`
- Changed: R48, R49, R51

## S3

- Uses: `Gen.SByte`, `Gen.UInt16`, `Gen.UInt32`, `Gen.UInt64`, `Gen.IntPtr`, `Gen.UIntPtr`, `IIntGen<T>`, `Sample`, `Minimum`; `Gen.Int128`, `Gen.UInt128`, `Gen.BigInteger`, `IIntGen<T>`; `Gen.Half`, `Gen.Single`, `Gen.Double`, `IRealGen<T>`; `Gen.Half`, `Gen.Single`, `Gen.Double`, `WithoutSpecialValues`; `Gen.Decimal`, `IRealGen<decimal>`; `Gen.Rune`, `Gen.Complex`, `IComplexGen`
- Stubs: `src/GalaxyCheck/Gens/IntegerGens.cs`, `src/GalaxyCheck/Gens/RealGens.cs`, `src/GalaxyCheck/Gens/RuneGen.cs`
- Changed: none

## S4

- Uses: `Gen.TimeSpan`, `Gen.DateTimeOffset`, `Gen.DateOnly`, `Gen.TimeOnly`, `IRangeGen<T>`; `Gen.Dictionary`, `IDictionaryGen<TKey, TValue>`; `Gen.KeyValuePair`, `Gen.ValueTuple`, `Gen.Tuple`; `Gen.Create<T>`, the explicit generators, `[Property]` of GalaxyCheck.Xunit; `Gen.Decimal32`, `Gen.Decimal64`, `Gen.Decimal128`, `Gen.BFloat16`, `Gen.ComplexOf<T>`, `Gen.Create<T>`, both target assemblies; each generator of the spec, `Sample` on both targets, `Check` with `replay`; `Gen.Create<T>` for the collection types; `Gen.Create<T>`, `RegisterImplementations<T>`
- Stubs: `src/GalaxyCheck/Gens/TimeGens.cs`, `src/GalaxyCheck/Gens/DictionaryGen.cs`, `src/GalaxyCheck/Gens/TupleGens.cs`, `src/GalaxyCheck/Gens/Net11Gens.cs`, `src/GalaxyCheck/Gens/FactoryGen.cs`
- Changed: R16, R17, R18, R20, R21, R31, R32, R33

## S5

- Uses: `Gen.StringMatching`; `Gen.Uri`, `Gen.Version`, `Gen.Email`, `Gen.Create<T>`; `Gen.Realistic`; `Gen.Recursive<T>`, `Gen.Create<T>`
- Stubs: `src/GalaxyCheck/Gens/PatternGen.cs`, `src/GalaxyCheck/Gens/TextGens.cs`, `src/GalaxyCheck/Gens/RealisticGens.cs`, `src/GalaxyCheck/Gens/RecursiveGen.cs`
- Changed: R54

## S6

- Uses: `Label`, `Cover`, `CheckResult<T>.Labels`, `CheckResult<T>.Report`, `Assert`; `Check` with `exhaustive`, `CheckResult<T>.Exhaustive`, `CheckResult<T>.Report`; `HealthCheck`, `HealthCheckException`, `Check` with `suppressHealthChecks`; `CSharpExpression.TryFormat`, the message of `Assert`; `Check` and `Assert` with `timeout`, `Property.ForAllAsync` with a token, `[Property(TimeoutMilliseconds = ...)]` of GalaxyCheck.Xunit
- Stubs: `src/GalaxyCheck/Properties/Labels.cs`, `src/GalaxyCheck/Runners/CheckOptions.cs`, `src/GalaxyCheck/Runners/CSharpExpression.cs`, `src/GalaxyCheck.Xunit/PropertyAttribute.cs`
- Changed: R39, R40, R55, R56

## S7

- Uses: `Command<TSystem, TModel>`, `StatefulGen.Commands`, `StatefulProperty.Create`; `StatefulProperty.Create`, `Check`, the message of `Assert`
- Stubs: `src/GalaxyCheck/Stateful/Stateful.cs`
- Changed: none

## S8

- Uses: `src/GalaxyCheck.Xunit.V3`, `[Property]`, `dotnet test` with a trx file, `dotnet pack`, the test project of GalaxyCheck.Xunit; `src/GalaxyCheck.Xunit.V3`, each attribute, `IConfigureGlobal`; `src/GalaxyCheck.TUnit`, `[Property]`, `dotnet run` with a trx report, `dotnet pack`; `src/GalaxyCheck.TUnit`, each attribute, `IConfigureGlobal`
- Stubs: `src/GalaxyCheck.Xunit.V3/Attributes.cs`, `src/GalaxyCheck.TUnit/Attributes.cs`
- Changed: R22, R23, R24, R25, R26, R27, R28, R29, R30

## S9

- Uses: `src/GalaxyCheck.NUnit`, `[Property]`, `dotnet test` with a trx file, `dotnet pack`; `src/GalaxyCheck.NUnit`, each attribute, `IConfigureGlobal`; `src/GalaxyCheck.MSTest`, `[Property]`, `dotnet test` with a trx file, `dotnet pack`; `src/GalaxyCheck.MSTest`, each attribute, `IConfigureGlobal`; `.galaxycheck/failures.json`, `[Property]` and `[Replay]` in each framework, `IGlobalConfiguration.FailureMemory`; `[Example]` in each framework
- Stubs: `src/GalaxyCheck.NUnit/Attributes.cs`, `src/GalaxyCheck.MSTest/Attributes.cs`, `src/GalaxyCheck.Xunit/Configuration/IGlobalConfiguration.cs`, `src/GalaxyCheck.Xunit.V3/Attributes.cs`, `src/GalaxyCheck.TUnit/Attributes.cs`, `src/GalaxyCheck.Xunit/ExampleAttribute.cs`
- Changed: R22, R23, R24, R25, R26, R27, R28, R29, R38, R52

## S10

- Uses: `src/GalaxyCheck.SourceGeneration`, `Gen.Create<T>`, `dotnet publish` with NativeAOT, the diagnostic `GCSG001`; `docs/generators.md`, the public methods of `Gen` on net11.0
- Stubs: `src/GalaxyCheck.SourceGeneration/GenCreateGenerator.cs`
- Changed: R57, R58, R60

## S11

- Uses: the message of `Assert`, each framework package, `GenErrorException`, `docs/llms.txt`, `dotnet pack`, the environment variables `GALAXYCHECK_AGENT` and `GALAXYCHECK_SEED`
- Stubs: none
- Changed: R63, R64, R65, R66

## S12

- Uses: the project files of `GalaxyCheck.sln`, the `.cs` files of `src/GalaxyCheck.Tests` and `src/GalaxyCheck.Tests.V3` (read at run time), the folder `packages-local`, `dotnet test` with a trx file
- Stubs: none
- Changed: R67

## S13

- Uses: each public generator and operator of the library, `Sample`, `Check`, `CheckAsync`, `Assert`, the `replay` argument, `CheckResult<T>` (`Iterations`, `Shrinks`, `Counterexample`, `Exhaustive`), `Property.Precondition`, a child process for the two-process check, `docs/reference-suite.md`
- Stubs: none
- Changed: R68, R69, R70, R71, R72, R73, R74, R75, R76, R77

