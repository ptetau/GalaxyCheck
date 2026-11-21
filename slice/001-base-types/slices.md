# Slices

## S1: Targets, package sources, and CI

- Story: As a maintainer, I build and test the libraries on net10.0 and net11.0 from nuget.org and the repository only, so that the build is supported and repeatable.
- Requirements: R19 (targets), R61 (CI), R62 (package sources)
- Acceptance:
  - Given a pack of GalaxyCheck and of GalaxyCheck.Xunit, when a test lists the `lib/` folders of each package, then it finds `net10.0` and `net11.0` and no `net8.0`. (R19)
  - Given `.github/workflows/dotnet.yml`, when a test reads it, then each `uses:` line names a major version that GitHub supports, the matrix holds `ubuntu-latest`, `windows-latest`, and `macos-latest`, and the test step runs for net10.0 and net11.0. (R19, R61)
  - Given `nuget.config`, when a test reads the package sources, then the only sources are nuget.org and one folder in the repository, and no source holds `myget.org`. (R62)
- Layers: build, packaging, ci
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S2: Benchmark suite with a baseline

- Story: As a maintainer, I run one command and see the speed of the current library next to the speed of commit f127a67, so that I can measure each gain.
- Requirements: R46 (benchmark suite), R47 (baseline in the same run)
- Acceptance:
  - Given the benchmark command, when it ends, then its report holds the six cases: check int, sample int, sample string, sample list of ints, sample record, and shrink list of ints. (R46)
  - Given the report, when a test reads a case, then the case holds the values for each second and the bytes for each value, for the baseline and for the current library. (R46, R47)
  - Given the report, when a test reads a case, then the case holds the ratio of current to baseline for throughput and for allocation, and the baseline names commit f127a67. (R47)
- Layers: benchmarks, build
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S3: Fast generation of integers

- Story: As a user, I check a property of an int 100 times faster, so that a large test suite runs quickly.
- Requirements: R48 (throughput), R49 (allocation)
- Acceptance:
  - Given the case "check int", when the suite runs, then the throughput ratio is at least 100. (R48)
  - Given the case "sample int", when the suite runs, then the throughput ratio is at least 100. (R48)
  - Given the cases "check int" and "sample int", when the suite runs, then the allocation ratio of each is at most 0.01. (R49)
- Layers: generator core, runner
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S4: Fast generation of strings and lists

- Story: As a user, I generate strings and lists 100 times faster, so that properties of collections run quickly.
- Requirements: R48 (throughput), R49 (allocation)
- Acceptance:
  - Given the case "sample string", when the suite runs, then the throughput ratio is at least 100 and the allocation ratio is at most 0.01. (R48, R49)
  - Given the case "sample list of ints", when the suite runs, then the throughput ratio is at least 100 and the allocation ratio is at most 0.01. (R48, R49)
- Layers: generator core, collection generators
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S5: Fast automatic generation of records

- Story: As a user, I generate a record from `Gen.Create<T>()` 100 times faster, so that properties of domain types run quickly.
- Requirements: R48 (throughput), R49 (allocation)
- Acceptance:
  - Given the case "sample record", with a record of an int, a string, a DateTime, and a Guid, when the suite runs, then the throughput ratio is at least 100. (R48)
  - Given the case "sample record", when the suite runs, then the allocation ratio is at most 0.01. (R49)
- Layers: automatic generator
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S6: Fast shrinking

- Story: As a user, I get the shrunk counterexample of a failing property 100 times faster, so that a failure does not slow the run.
- Requirements: R48 (throughput), R49 (allocation)
- Acceptance:
  - Given the case "shrink list of ints", with the property that a list has fewer than 5 items or a sum below 100, when the suite runs, then the throughput ratio is at least 100. (R48)
  - Given the case "shrink list of ints", when the suite runs, then the allocation ratio is at most 0.01. (R49)
- Layers: shrinking, runner
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S7: Performance gate and README examples

- Story: As a maintainer, I get a failed test when the library is slower than the target, so that the speed stays.
- Requirements: R50 (performance gate), R51 (documented API stays)
- Acceptance:
  - Given a report in which the case "sample int" has a throughput ratio of 99, when the gate reads it, then the gate fails, and the message holds "sample int" and "99". (R50)
  - Given a report in which the case "sample string" has an allocation ratio of 0.02, when the gate reads it, then the gate fails, and the message holds "sample string" and "0.02". (R50)
  - Given a report in which each throughput ratio is 100 and each allocation ratio is 0.01, when the gate reads it, then the gate passes. (R50)
  - Given each C# code block of README.md, when a test compiles and runs it against the current library, then it compiles with no change and runs. (R51)
- Layers: benchmarks, tests, docs
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S8: Unsigned and native integers

- Story: As a user, I generate each built-in integer type with the options of `Gen.Int32()`, so that I do not cast by hand.
- Requirements: R1 (integers), R2 (integer options), R3 (integer shrinking)
- Acceptance:
  - Given `Gen.SByte()`, `Gen.UInt16()`, `Gen.UInt32()`, `Gen.UInt64()`, `Gen.IntPtr()`, and `Gen.UIntPtr()`, when each samples 100 values for each seed from 0 to 99, then the minimum and the maximum of its type both appear. (R1)
  - Given `Gen.UInt32().Between(10, 20)`, when it samples 1000 values, then each value is from 10 to 20, and 10 and 20 both appear. (R2)
  - Given `Gen.UInt16().GreaterThanEqual(5).LessThanEqual(4)`, when it runs, then the result is a generator error whose message names the bound option. (R2)
  - Given `Gen.UInt64()` and the property "the value is below 1000", when the property fails, then the counterexample is 1000. (R3)
  - Given `Gen.SByte().Between(-50, -10)` and a property that always fails, when the property fails, then the counterexample is -10. (R3)
- Layers: generator api, generator core
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S9: Int128, UInt128, and BigInteger

- Story: As a user, I generate very large integers, so that I can test code that uses them.
- Requirements: R1 (integers), R2 (integer options), R3 (integer shrinking), R4 (BigInteger)
- Acceptance:
  - Given `Gen.Int128()` and `Gen.UInt128()`, when each samples 100 values for each seed from 0 to 99, then the minimum and the maximum of its type both appear. (R1)
  - Given `Gen.Int128().Between(-5, 5)`, when it samples 1000 values, then each value is from -5 to 5. (R2)
  - Given `Gen.BigInteger()`, when it samples 100 values for each seed from 0 to 99, then a value below `Int64.MinValue` and a value above `Int64.MaxValue` both appear. (R4)
  - Given `Gen.BigInteger()` and the property "the value is below 10 to the power 30", when the property fails, then the counterexample is 10 to the power 30. (R3, R4)
- Layers: generator api, generator core
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S10: Binary floating-point numbers

- Story: As a user, I generate Half, float, and double values with fractions, so that I test real arithmetic.
- Requirements: R5 (binary floats), R6 (float options), R9 (float shrinking)
- Acceptance:
  - Given `Gen.Half()`, `Gen.Single()`, and `Gen.Double()` with the special values removed, when each samples 1000 values, then a value with a fraction part, a negative value, and a positive value all appear. (R5)
  - Given `Gen.Double()` with the special values removed, when it samples 100 values for each seed from 0 to 99, then a value above 1e300 and a value below -1e300 both appear. (R5)
  - Given `Gen.Double().Between(1.5, 2.5)`, when it samples 1000 values, then each value is from 1.5 to 2.5. (R6)
  - Given `Gen.Double()` and the property "the value is below 3.5", when the property fails, then the counterexample is 4. (R9)
  - Given `Gen.Single()` and a property that always fails, when the property fails, then the counterexample is 0. (R9)
- Layers: generator api, generator core
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S11: Special floating-point values

- Story: As a user, I get NaN, the infinities, and negative zero without asking, so that my property meets the values that break code.
- Requirements: R7 (float special values), R8 (special value opt-out)
- Acceptance:
  - Given `Gen.Double()` with no option, when it samples 100 values for each seed from 0 to 99, then at least 90 of the 100 seeds give each of NaN, positive infinity, negative infinity, negative zero, `double.Epsilon`, `double.MinValue`, and `double.MaxValue`. (R7)
  - Given `Gen.Single().Between(-1, 1)`, when it samples 10000 values, then no value is NaN or an infinity, and no value is outside -1 to 1. (R7)
  - Given `Gen.Double()` with the option that removes the special values, when it samples 10000 values, then each value is finite. (R8)
  - Given `Gen.Double()` and the property "the value is not NaN and is below 1e10", when the property fails, then the counterexample is 1e10. (R9)
- Layers: generator api, generator core
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S12: Decimal numbers

- Story: As a user, I generate decimal values with fractions over the full range, so that I test money arithmetic.
- Requirements: R10 (decimal), R6 (float options), R8 (special value opt-out), R9 (float shrinking)
- Acceptance:
  - Given `Gen.Decimal()`, when it samples 100 values for each seed from 0 to 99, then a value with a fraction part, zero, `decimal.MinValue`, and `decimal.MaxValue` all appear. (R10)
  - Given `Gen.Decimal().Between(0.10m, 0.20m)`, when it samples 1000 values, then each value is from 0.10 to 0.20. (R6)
  - Given `Gen.Decimal()` with the option that removes the special values, when it samples 10000 values, then no value is `decimal.MinValue` or `decimal.MaxValue`. (R8)
  - Given `Gen.Decimal()` and the property "the value is below 2.5", when the property fails, then the counterexample is 3. (R9)
- Layers: generator api, generator core
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S13: Time types

- Story: As a user, I generate TimeSpan, DateTimeOffset, DateOnly, and TimeOnly values, so that I test code that uses dates and times.
- Requirements: R11 (time types)
- Acceptance:
  - Given `Gen.TimeSpan()`, `Gen.DateOnly()`, and `Gen.TimeOnly()`, when each samples 100 values for each seed from 0 to 99, then the `MinValue` and the `MaxValue` of its type both appear. (R11)
  - Given `Gen.DateOnly()` with the bounds 2020-01-01 and 2020-12-31, when it samples 1000 values, then each value is in the year 2020. (R11)
  - Given `Gen.DateTimeOffset()`, when it samples 1000 values, then a value with an offset above zero and a value with an offset below zero both appear. (R11)
  - Given each of the four generators and a property that always fails, when the property fails, then the counterexample is the documented origin of that generator. (R11)
- Layers: generator api, generator core
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S14: Rune and Complex

- Story: As a user, I generate Unicode scalar values and complex numbers, so that I test text and numeric code.
- Requirements: R12 (Rune), R13 (Complex)
- Acceptance:
  - Given `Gen.Rune()`, when it samples 10000 values, then no value is in the surrogate range D800 to DFFF, a value above FFFF appears, and a value below 10000 (hex) appears. (R12)
  - Given `Gen.Rune()` and a property that always fails, when the property fails, then the counterexample is the rune `'a'`. (R12)
  - Given `Gen.Complex()`, when it samples 1000 values, then a value with a NaN part appears, and a value with a fraction in each part appears. (R13)
  - Given `Gen.Complex()` with the option that removes the special values, when it samples 10000 values, then the real part and the imaginary part of each value are finite. (R13)
- Layers: generator api
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S15: Dictionaries

- Story: As a user, I generate a dictionary from a key generator and a value generator, so that I test code that uses maps.
- Requirements: R14 (dictionaries)
- Acceptance:
  - Given a dictionary generator of int keys and string values with a minimum count of 2 and a maximum count of 5, when it samples 1000 values, then each count is from 2 to 5. (R14)
  - Given a dictionary generator with an exact count of 3, when it samples 1000 values, then each value has 3 distinct keys. (R14)
  - Given a dictionary generator of int keys and int values and the property "the count is below 2", when the property fails, then the counterexample has 2 entries, with the keys 0 and 1 in any order, and each value is 0. (R14)
- Layers: generator api, collection generators
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S16: Pairs and tuples

- Story: As a user, I generate KeyValuePair, ValueTuple, and Tuple values from element generators, so that I do not write a `Select` by hand.
- Requirements: R15 (pairs and tuples)
- Acceptance:
  - Given a `KeyValuePair` generator from `Gen.Int32()` and `Gen.Boolean()`, when it samples 100 values, then each value is a `KeyValuePair<int, bool>`. (R15)
  - Given a `ValueTuple` generator for each count from 1 to 7 and a `Tuple` generator for each count from 1 to 7, when each samples 10 values, then each value has that count of elements. (R15)
  - Given a `ValueTuple` generator of two `Gen.Int32()` and the property "the first is below 10 or the second is below 20", when the property fails, then the counterexample is (10, 20). (R15)
- Layers: generator api
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S17: Automatic generation of the base types

- Story: As a user, I ask `Gen.Create<T>()` for any base type and get the values of its explicit generator, so that a `[Property]` parameter just works.
- Requirements: R16 (automatic generation), R17 (replaced generators)
- Acceptance:
  - Given each type of R1, R4, R5, R10, R11, R12, and R13, when `Gen.Create<T>()` and the explicit generator with no option sample 100 values with seed 0, then the two lists are equal. (R16)
  - Given `Dictionary<int, string>`, `IDictionary<int, string>`, `IReadOnlyDictionary<int, string>`, `ImmutableDictionary<int, string>`, `KeyValuePair<int, string>`, `(int, string)`, and `Tuple<int, string>`, when `Gen.Create<T>()` samples 100 values, then each value has that type and no generator error occurs. (R16)
  - Given `Gen.Create<double>()`, when it samples 1000 values, then a value with a fraction part appears. (R17)
  - Given `Gen.Create<uint>()`, when it samples 100 values for each seed from 0 to 99, then 0 and `uint.MaxValue` both appear. (R17)
  - Given a `[Property]` method in an xunit v2 test with a `double` parameter and a `Dictionary<int, string>` parameter, when the test runs, then the method receives values and the test passes. (R16)
- Layers: automatic generator, xunit v2 package
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S18: Types of .NET 11

- Story: As a user on .NET 11, I generate Decimal32, Decimal64, Decimal128, BFloat16, and `Complex<T>` values, so that I can test code that uses the new numeric types.
- Requirements: R18 (.NET 11 types)
- Acceptance:
  - Given the net11.0 assembly, when a test reads the members of `Gen`, then `Decimal32`, `Decimal64`, `Decimal128`, and `BFloat16` exist, and a generator for `Complex<T>` exists. (R18)
  - Given the net10.0 assembly, when a test reads the members of `Gen`, then none of those members exist. (R18)
  - Given `Gen.BFloat16()` on net11.0 with no option, when it samples 100 values for each seed from 0 to 99, then NaN, positive infinity, and negative zero all appear, and with the opt-out each value is finite. (R18)
  - Given `Gen.Decimal64()` on net11.0 and the property "the value is below 2.5", when the property fails, then the counterexample is 3. (R18)
  - Given `Gen.Create<Decimal128>()` on net11.0 and `Gen.Decimal128()`, when each samples 100 values with seed 0, then the two lists are equal. (R18)
- Layers: generator api, automatic generator, build
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S19: Same values on both targets, and replay

- Story: As a user, I get the same values on .NET 10 and on .NET 11 and can replay a failure, so that a failure in CI reproduces on my machine.
- Requirements: R20 (same values on both targets), R21 (replay)
- Acceptance:
  - Given each generator of this spec that both targets have, when it samples 100 values with seed 0 and size 50 on net10.0 and on net11.0, then the two lists are equal. (R20)
  - Given a failing property of `Gen.Double()`, `Gen.UInt64()`, and a dictionary generator, when the property runs again with the replay string of its failure, then the counterexample is the same and the run has one test case. (R21)
- Layers: generator core, runner, build
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S20: More collections

- Story: As a user, I ask `Gen.Create<T>()` for the other common collection types, so that my domain types generate without a registration.
- Requirements: R31 (more collections)
- Acceptance:
  - Given `Queue<int>`, `Stack<int>`, `SortedSet<int>`, `LinkedList<int>`, `ImmutableArray<int>`, `ReadOnlyCollection<int>`, `Memory<int>`, and `ReadOnlyMemory<int>`, when `Gen.Create<T>()` samples 100 values, then each value has that type and no generator error occurs. (R31)
  - Given `Gen.Create<Queue<int>>()` and the property "the count is below 3", when the property fails, then the counterexample holds 0, 0, 0. (R31)
- Layers: automatic generator
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S21: Interfaces, base classes, and unions

- Story: As a user, I ask `Gen.Create<T>()` for an interface, an abstract class, or a union, so that polymorphic domain types generate.
- Requirements: R32 (interfaces and base classes), R33 (unions)
- Acceptance:
  - Given an interface `IShape` with the concrete types `Circle` and `Square` in its assembly, when `Gen.Create<IShape>()` samples 100 values, then a `Circle` and a `Square` both appear. (R32)
  - Given an abstract class with no concrete type in its assembly, when `Gen.Create<T>()` runs, then the result is a generator error whose message names the abstract class. (R32)
  - Given `IShape` and a list of candidate types that holds only `Circle`, when the generator samples 100 values, then each value is a `Circle`. (R32)
  - Given a C# union of `Cat`, `Dog`, and `Bird` on net11.0, when `Gen.Create<T>()` samples 100 values, then each of the three cases appears. (R33)
- Layers: automatic generator
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S22: Strings that match a pattern

- Story: As a user, I generate strings that match a regular expression, so that I test parsers with valid input.
- Requirements: R34 (pattern strings)
- Acceptance:
  - Given the pattern `[a-c]{2,4}-\d{3}`, when the generator samples 1000 values, then each value matches the whole pattern. (R34)
  - Given the pattern `[a-c]{2,4}-\d{3}` and a property that always fails, when the property fails, then the counterexample is `aa-000`. (R34)
  - Given the pattern `(?<=a)b`, when the generator runs, then the result is a generator error whose message names the lookbehind. (R34)
- Layers: generator api, string generators
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S23: Uri, Version, and email values

- Story: As a user, I generate real Uri, Version, and email values, so that I do not get one fixed Uri.
- Requirements: R35 (Uri, Version, email)
- Acceptance:
  - Given `Gen.Uri()`, when it samples 100 values, then each value is absolute, each scheme is http or https, both schemes appear, and at least 50 values are different. (R35)
  - Given `Gen.Version()`, when it samples 1000 values, then a value with 2 parts, a value with 3 parts, and a value with 4 parts all appear. (R35)
  - Given the email generator, when it samples 1000 values, then each value has one `@`, a local part that is not empty, and a domain that holds a dot. (R35)
  - Given `Gen.Create<Uri>()` and `Gen.Uri()`, when each samples 100 values with seed 0, then the two lists are equal, and the same holds for `Version`. (R35)
- Layers: generator api, automatic generator
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S24: Realistic data

- Story: As a user, I generate names, addresses, and phone numbers, so that a counterexample is easy to read.
- Requirements: R59 (realistic data)
- Acceptance:
  - Given the generators for a first name, a last name, a full name, a street address, a city, a country, a phone number, and a company name, when each samples 100 values, then no value is empty and at least 10 values of each are different. (R59)
  - Given the first name generator and a property that always fails, when the property fails, then the counterexample is the first entry of the first name list. (R59)
  - Given the full name generator, when it samples 100 values, then each value is a first name of the list, a space, and a last name of the list. (R59)
- Layers: generator api, data
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S25: Recursive generators

- Story: As a user, I generate tree-shaped data with a depth limit, so that I test code that handles JSON or expressions.
- Requirements: R53 (recursive generators), R54 (recursive automatic generation)
- Acceptance:
  - Given a tree type of leaves and nodes, and a recursive generator with a depth limit of 3, when it samples 1000 values, then no value is deeper than 3 and a value of depth 3 appears. (R53)
  - Given that generator and the property "the tree has no node", when the property fails, then the counterexample is one node whose children are leaves. (R53)
  - Given a class `Node` with a property of type `List<Node>`, when `Gen.Create<Node>()` samples 100 values, then the run ends and no value is deeper than the limit. (R54)
- Layers: generator api, automatic generator
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S26: Labels and coverage

- Story: As a user, I tag my test cases and require a share of each tag, so that I know that my generator reaches the cases that matter.
- Requirements: R36 (labels), R37 (cover)
- Acceptance:
  - Given a property of `Gen.Int32().Between(0, 9)` that gives the label "even" to each even value, when it passes with 100 iterations, then the report lists "even" with its share as a percentage. (R36)
  - Given the same property with a failure, when it fails, then the failure report lists "even" with its share. (R36)
  - Given a property that requires a share of 50% for a label that 10% of the test cases have, when the last iteration ends, then the property fails, and the message holds the label, "50%", and the actual share. (R37)
  - Given a property that requires a share of 5% for a label that 50% of the test cases have, when it runs, then it passes. (R37)
- Layers: property api, runner, report
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S27: Exhaustive runs of small domains

- Story: As a user, I get each value of a small input space tried one time, so that a property of a bool or an enum is proved for all inputs.
- Requirements: R55 (exhaustive small domains)
- Acceptance:
  - Given a property of a `bool` and an enum of 3 values with 100 iterations, when it runs, then the test function runs 6 times, with 6 different inputs, and the report says that the run was exhaustive. (R55)
  - Given a property of `Gen.Int32().Between(1, 10)` with 100 iterations, when it runs, then the test function receives each value from 1 to 10 one time. (R55)
  - Given a property of `Gen.Int32().Between(1, 1000)` with 100 iterations, when it runs, then the test function runs 100 times and the report does not say exhaustive. (R55)
  - Given the first property with the option that switches the feature off, when it runs, then the test function runs 100 times. (R55)
- Layers: property api, runner, report
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S28: Health checks

- Story: As a user, I get a named failure when my property wastes its test cases, so that a weak test does not pass in silence.
- Requirements: R56 (health checks)
- Acceptance:
  - Given a generator whose filter keeps 5% of its values, when a property of it runs, then the property fails, and the message names the discard check and how to suppress it. (R56)
  - Given a generator that takes 2 seconds for one value, when a property of it runs, then the property fails, and the message names the slow generator check. (R56)
  - Given a property whose precondition is false for each input, when it runs, then the property fails, and the message names the check for a test function that never runs. (R56)
  - Given each of those properties with its check suppressed by name, when it runs, then that check does not fail the property. (R56)
- Layers: property api, runner, report
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S29: Copyable counterexamples

- Story: As a user, I paste the counterexample of a failure into a unit test, so that I keep the case as a regression test.
- Requirements: R39 (copyable counterexamples)
- Acceptance:
  - Given a failing property with the counterexample 1.5 (a double), "a\"b" (a string), and a list of the ints 1 and 2, when it fails, then the report holds a C# expression for each value, and each expression compiles and equals the value. (R39)
  - Given a failing property whose counterexample is a record `Point(int X, int Y)` with X 1 and Y 2, when it fails, then the report holds `new Point(1, 2)` or an expression that compiles and equals it. (R39)
  - Given a failing property whose counterexample is a `Func<int, int>`, when it fails, then the report holds the text for that value that the report holds today. (R39)
- Layers: report
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S30: Time limits and cancellation

- Story: As a user, I give a property a time limit and receive a cancellation token, so that a hung test case fails with its input.
- Requirements: R40 (timeout), R41 (cancellation)
- Acceptance:
  - Given a property with a time limit of 100 milliseconds whose test function waits 5 seconds for the input 7, when it runs, then the property fails in less than 2 seconds, and the message holds "100" and the input 7. (R40)
  - Given `[Property]` with the time limit option on an xunit v2 test, when the test runs over the limit, then the test fails with the same message. (R40)
  - Given an async property with an int parameter and a `CancellationToken` parameter, when it runs, then the int is generated and the token is not, and the counterexample of a failure lists only the int. (R41)
  - Given that property with a time limit of 100 milliseconds and a test function that waits on the token, when the limit passes, then the token is cancelled. (R41)
- Layers: property api, runner, xunit v2 package
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S31: Stateful properties

- Story: As a user, I define commands against a model and run random sequences of them, so that I test a system with state.
- Requirements: R42 (commands), R43 (stateful property)
- Acceptance:
  - Given a counter with the commands "increment" and "decrement", where "decrement" has the precondition that the model is above 0, when the sequence generator samples 1000 sequences, then no sequence holds a "decrement" at a position where the model is 0. (R42)
  - Given a correct counter, when the stateful property runs 100 sequences, then it passes, and each sequence starts with a new system and a new model. (R43)
  - Given a counter whose "increment" adds 2 when the value is 3, when the stateful property runs, then it fails by a postcondition. (R43)
  - Given a counter whose "decrement" throws an exception that the command does not expect, when the stateful property runs, then it fails. (R43)
- Layers: stateful api, generator core, runner
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S32: Stateful shrinking and report

- Story: As a user, I get the shortest failing sequence of commands in the report, so that I can read the cause.
- Requirements: R44 (stateful shrinking), R45 (stateful report)
- Acceptance:
  - Given the counter whose "increment" adds 2 when the value is 3, when the stateful property fails, then the shrunk sequence is 4 "increment" commands. (R44)
  - Given a failing sequence, when it shrinks, then each precondition holds at its position in each shrunk sequence that runs. (R44)
  - Given that failure, when a test reads the report, then the report lists the 4 commands in order with the inputs of each, and names the fourth command as the command that failed. (R45)
- Layers: stateful api, shrinking, report
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S33: Properties in xunit v3

- Story: As a user of xunit v3, I put `[Property]` on a test, so that I run properties without xunit v2.
- Requirements: R22 (framework packages), R23 (property result), R24 (property forms), R30 (xunit v2 unchanged)
- Acceptance:
  - Given a pack of GalaxyCheck.Xunit.V3, when a test lists its `lib/` folders, then it finds `net10.0` and `net11.0`. (R22)
  - Given an xunit v3 project with a `[Property]` test that holds, when the project runs, then the result holds one passed test for it. (R23)
  - Given an xunit v3 `[Property]` test "the int is below 10", when the project runs, then the result holds one failed test, and its message holds the counterexample 10 and a replay string, in the text that the same test gives in GalaxyCheck.Xunit. (R23)
  - Given xunit v3 `[Property]` tests with a void, a boolean, a `Property`, and a generator return, and the async form of each, when the project runs, then each test that holds passes and each test that is falsified fails. (R24)
  - Given the test project of GalaxyCheck.Xunit, when it runs, then it has 74 passed tests and no failed test. (R30)
- Layers: xunit v3 package, shared framework core, packaging
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S34: Full parity in xunit v3

- Story: As a user of xunit v3, I use each attribute of the xunit v2 package, so that I can move my tests.
- Requirements: R22 (framework packages), R25 (replay and member generators), R26 (sample), R27 (generator snapshot), R28 (data rows), R29 (global configuration)
- Acceptance:
  - Given the xunit v3 package, when a test reads its public types, then `Property`, `Sample`, `GenSnapshot`, `Replay`, and `MemberGen` attributes exist with the options of GalaxyCheck.Xunit. (R22)
  - Given a failing xunit v3 property with `[Replay]` of its replay string, when the project runs, then the test function runs one time and the test fails with the same counterexample. (R25)
  - Given `[Replay("0")]`, when the project runs, then the test fails with the error text that GalaxyCheck.Xunit gives. (R25)
  - Given a parameter with `[MemberGen]` that names a member which returns `Gen.Int32().Between(5, 5)`, when the project runs, then the parameter is 5 in each test case. (R25)
  - Given a `[Sample]` test, when the project runs, then the test fails and its message lists the sampled values. (R26)
  - Given a `[GenSnapshot]` test with a stored snapshot, when the generator changes its values, then the test fails, and with no change it passes. (R27)
  - Given a `[Property]` with `[InlineData(1)]` and `[InlineData(2)]` and one more int parameter, when the project runs, then the result holds 2 tests, the first parameter is 1 or 2, and the second is generated. (R28)
  - Given a global configuration with default iterations of 7, when a `[Property]` with no iterations option runs, then the test function runs 7 times. (R29)
- Layers: xunit v3 package, shared framework core
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S35: Properties in TUnit

- Story: As a user of TUnit, I put `[Property]` on a test, so that I run properties in TUnit.
- Requirements: R22 (framework packages), R23 (property result), R24 (property forms)
- Acceptance:
  - Given a pack of GalaxyCheck.TUnit, when a test lists its `lib/` folders, then it finds `net10.0` and `net11.0`. (R22)
  - Given a TUnit project with a `[Property]` test that holds, when the project runs, then the result holds one passed test for it. (R23)
  - Given a TUnit `[Property]` test "the int is below 10", when the project runs, then the result holds one failed test, and its message holds the counterexample 10 and a replay string, in the text that GalaxyCheck.Xunit gives. (R23)
  - Given TUnit `[Property]` tests with a void, a boolean, a `Property`, and a generator return, and the async form of each, when the project runs, then each test that holds passes and each test that is falsified fails. (R24)
- Layers: tunit package, shared framework core, packaging
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S36: Full parity in TUnit

- Story: As a user of TUnit, I use each attribute of the xunit v2 package, so that I lose no feature.
- Requirements: R22 (framework packages), R25 (replay and member generators), R26 (sample), R27 (generator snapshot), R28 (data rows), R29 (global configuration)
- Acceptance:
  - Given the TUnit package, when a test reads its public types, then `Property`, `Sample`, `GenSnapshot`, `Replay`, and `MemberGen` attributes exist with the options of GalaxyCheck.Xunit. (R22)
  - Given a failing TUnit property with `[Replay]` of its replay string, when the project runs, then the test function runs one time and the test fails with the same counterexample. (R25)
  - Given a parameter with `[MemberGen]` that names a member which returns `Gen.Int32().Between(5, 5)`, when the project runs, then the parameter is 5 in each test case. (R25)
  - Given a `[Sample]` test, when the project runs, then the test fails and its message lists the sampled values. (R26)
  - Given a `[GenSnapshot]` test with a stored snapshot, when the generator changes its values, then the test fails, and with no change it passes. (R27)
  - Given a `[Property]` with `[Arguments(1)]` and `[Arguments(2)]` and one more int parameter, when the project runs, then the result holds 2 tests, the first parameter is 1 or 2, and the second is generated. (R28)
  - Given a global configuration with default iterations of 7, when a `[Property]` with no iterations option runs, then the test function runs 7 times. (R29)
- Layers: tunit package, shared framework core
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S37: Properties in NUnit

- Story: As a user of NUnit, I put `[Property]` on a test, so that I run properties in NUnit.
- Requirements: R22 (framework packages), R23 (property result), R24 (property forms)
- Acceptance:
  - Given a pack of GalaxyCheck.NUnit, when a test lists its `lib/` folders, then it finds `net10.0` and `net11.0`. (R22)
  - Given an NUnit project with a `[Property]` test that holds, when the project runs, then the result holds one passed test for it. (R23)
  - Given an NUnit `[Property]` test "the int is below 10", when the project runs, then the result holds one failed test, and its message holds the counterexample 10 and a replay string, in the text that GalaxyCheck.Xunit gives. (R23)
  - Given NUnit `[Property]` tests with a void, a boolean, a `Property`, and a generator return, and the async form of each, when the project runs, then each test that holds passes and each test that is falsified fails. (R24)
- Layers: nunit package, shared framework core, packaging
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S38: Full parity in NUnit

- Story: As a user of NUnit, I use each attribute of the xunit v2 package, so that I lose no feature.
- Requirements: R22 (framework packages), R25 (replay and member generators), R26 (sample), R27 (generator snapshot), R28 (data rows), R29 (global configuration)
- Acceptance:
  - Given the NUnit package, when a test reads its public types, then `Property`, `Sample`, `GenSnapshot`, `Replay`, and `MemberGen` attributes exist with the options of GalaxyCheck.Xunit. (R22)
  - Given a failing NUnit property with `[Replay]` of its replay string, when the project runs, then the test function runs one time and the test fails with the same counterexample. (R25)
  - Given a parameter with `[MemberGen]` that names a member which returns `Gen.Int32().Between(5, 5)`, when the project runs, then the parameter is 5 in each test case. (R25)
  - Given a `[Sample]` test, when the project runs, then the test fails and its message lists the sampled values. (R26)
  - Given a `[GenSnapshot]` test with a stored snapshot, when the generator changes its values, then the test fails, and with no change it passes. (R27)
  - Given a `[Property]` with `[TestCase(1)]` and `[TestCase(2)]` and one more int parameter, when the project runs, then the result holds 2 tests, the first parameter is 1 or 2, and the second is generated. (R28)
  - Given a global configuration with default iterations of 7, when a `[Property]` with no iterations option runs, then the test function runs 7 times. (R29)
- Layers: nunit package, shared framework core
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S39: Properties in MSTest

- Story: As a user of MSTest, I put `[Property]` on a test, so that I run properties in MSTest.
- Requirements: R22 (framework packages), R23 (property result), R24 (property forms)
- Acceptance:
  - Given a pack of GalaxyCheck.MSTest, when a test lists its `lib/` folders, then it finds `net10.0` and `net11.0`. (R22)
  - Given an MSTest project with a `[Property]` test that holds, when the project runs, then the result holds one passed test for it. (R23)
  - Given an MSTest `[Property]` test "the int is below 10", when the project runs, then the result holds one failed test, and its message holds the counterexample 10 and a replay string, in the text that GalaxyCheck.Xunit gives. (R23)
  - Given MSTest `[Property]` tests with a void, a boolean, a `Property`, and a generator return, and the async form of each, when the project runs, then each test that holds passes and each test that is falsified fails. (R24)
- Layers: mstest package, shared framework core, packaging
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S40: Full parity in MSTest

- Story: As a user of MSTest, I use each attribute of the xunit v2 package, so that I lose no feature.
- Requirements: R22 (framework packages), R25 (replay and member generators), R26 (sample), R27 (generator snapshot), R28 (data rows), R29 (global configuration)
- Acceptance:
  - Given the MSTest package, when a test reads its public types, then `Property`, `Sample`, `GenSnapshot`, `Replay`, and `MemberGen` attributes exist with the options of GalaxyCheck.Xunit. (R22)
  - Given a failing MSTest property with `[Replay]` of its replay string, when the project runs, then the test function runs one time and the test fails with the same counterexample. (R25)
  - Given a parameter with `[MemberGen]` that names a member which returns `Gen.Int32().Between(5, 5)`, when the project runs, then the parameter is 5 in each test case. (R25)
  - Given a `[Sample]` test, when the project runs, then the test fails and its message lists the sampled values. (R26)
  - Given a `[GenSnapshot]` test with a stored snapshot, when the generator changes its values, then the test fails, and with no change it passes. (R27)
  - Given a `[Property]` with `[DataRow(1)]` and `[DataRow(2)]` and one more int parameter, when the project runs, then the result holds 2 tests, the first parameter is 1 or 2, and the second is generated. (R28)
  - Given a global configuration with default iterations of 7, when a `[Property]` with no iterations option runs, then the test function runs 7 times. (R29)
- Layers: mstest package, shared framework core
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S41: Failure memory

- Story: As a user, I get my last failure tried first on the next run, so that I do not paste a replay string by hand.
- Requirements: R38 (failure memory)
- Acceptance:
  - Given a failing `[Property]` test in each of the five frameworks, when the project runs, then a file in the documented folder of the test project holds the replay string under the full name of the test. (R38)
  - Given that file, when the project runs again, then the first test case of that test is the saved counterexample. (R38)
  - Given that file and a test function that now holds, when the project runs, then the test passes and the file no longer holds the entry. (R38)
  - Given that file and a `[Replay]` attribute with another replay string, when the project runs, then the test runs the replay of the attribute. (R38)
  - Given a global configuration that switches the feature off, when a failing test runs, then no file is written. (R38)
- Layers: shared framework core, each framework package, store
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S42: Explicit examples

- Story: As a user, I pin fixed inputs on a property, so that a known regression case runs on each run.
- Requirements: R52 (explicit examples)
- Acceptance:
  - Given a property of two ints with `[Example(1, 2)]` and `[Example(3, 4)]`, in each of the five frameworks, when the project runs, then the first two test cases have the inputs (1, 2) and (3, 4), and generated test cases follow. (R52)
  - Given the property "the first int is not 3" with `[Example(3, 4)]`, when the project runs, then the test fails, the message holds the inputs 3 and 4, and the counterexample is not shrunk. (R52)
  - Given a property of two ints with `[Example(1)]`, when the project runs, then the result is one failed test whose message names the second parameter. (R52)
  - Given a property of an int with `[Example("a")]`, when the project runs, then the result is one failed test whose message names the parameter. (R52)
- Layers: shared framework core, each framework package
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S43: Source generator for NativeAOT and trimming

- Story: As a user who publishes tests with NativeAOT, I get `Gen.Create<T>()` built at compile time, so that my properties run with no reflection.
- Requirements: R57 (source generator), R58 (source generator parity)
- Acceptance:
  - Given a test project with the source generator package and `Gen.Create<Order>()` for a record `Order(int Id, string Name)`, when the project is published with trimming and with NativeAOT, then the publish output holds no trim warning and no AOT warning from a GalaxyCheck package, and the published program runs its property with a pass. (R57)
  - Given `Order`, when the generated code and the reflection path each sample 100 values with seed 0, then the two lists are equal. (R58)
  - Given `Gen.Create<T>()` for a type that the source generator does not support, when the project compiles, then the compiler reports a GalaxyCheck diagnostic that names the type. (R58)
- Layers: source generator, automatic generator, packaging
- Tier: hard
- Status: planned
- Test rounds: 0
- Review rounds: 0

## S44: Reference pages

- Story: As a user, I find each generator with an example in the docs, so that I learn the library without the source.
- Requirements: R60 (reference pages)
- Acceptance:
  - Given the public generator methods of `Gen` on net11.0, when a test reads `docs/`, then each method name has a reference entry that holds one C# code example. (R60)
  - Given a public generator method with no entry, when the test runs, then it fails and its message names the method. (R60)
- Layers: docs, tests
- Tier: standard
- Status: planned
- Test rounds: 0
- Review rounds: 0
