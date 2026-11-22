# Slices

## S1: Targets, CI, and the benchmark suite

- Story: As a maintainer, I build the libraries for net10.0 and net11.0 and measure their speed against commit f127a67, so that the build is supported and each gain is measured.
- Requirements: R19 (targets), R46 (benchmark suite), R47 (baseline in the same run), R61 (CI), R62 (package sources)
- Acceptance:
  - Given a pack of GalaxyCheck and of GalaxyCheck.Xunit, when a test lists the `lib/` folders of each package, then it finds `net10.0` and `net11.0` and no `net8.0`. (R19)
  - Given `.github/workflows/dotnet.yml`, when a test reads it, then each `uses:` line names a major version that GitHub supports, the matrix holds `ubuntu-latest`, `windows-latest`, and `macos-latest`, and the test step runs for net10.0 and net11.0. (R19, R61)
  - Given `nuget.config`, when a test reads the package sources, then the only sources are nuget.org and one folder in the repository, and no source holds `myget.org`. (R62)
  - Given the benchmark command, when it ends, then its report holds the six cases: check int, sample int, sample string, sample list of ints, sample record, and shrink list of ints. (R46)
  - Given the report, when a test reads a case, then the case holds the values for each second and the bytes for each value, for the baseline and for the current library. (R46, R47)
  - Given the report, when a test reads a case, then the case holds the ratio of current to baseline for throughput and for allocation, and the baseline names commit f127a67. (R47)
- Layers: build, packaging, ci, benchmarks
- Tier: standard
- Status: done
- Test rounds: 2
- Review rounds: 2

## S2: A core that is 100 times faster

- Story: As a user, I generate, check, and shrink values 100 times faster with 1% of the memory, so that a large test suite runs quickly.
- Requirements: R48 (throughput), R49 (allocation), R50 (performance gate), R51 (documented API stays)
- Acceptance:
  - Given the case "check int", when the suite runs, then the throughput ratio is at least 100. (R48)
  - Given the case "sample int", when the suite runs, then the throughput ratio is at least 100. (R48)
  - Given the cases "check int" and "sample int", when the suite runs, then the allocation ratio of each is at most 0.01. (R49)
  - Given the case "sample string", when the suite runs, then the throughput ratio is at least 100 and the allocation ratio is at most 0.01. (R48, R49)
  - Given the case "sample list of ints", when the suite runs, then the throughput ratio is at least 100 and the allocation ratio is at most 0.01. (R48, R49)
  - Given the case "sample record", with a record of an int, a string, a DateTime, and a Guid, when the suite runs, then the throughput ratio is at least 100. (R48)
  - Given the case "sample record", when the suite runs, then the allocation ratio is at most 0.01. (R49)
  - Given the case "shrink list of ints", with the property that a list has fewer than 5 items or a sum below 100, when the suite runs, then the throughput ratio is at least 100. (R48)
  - Given the case "shrink list of ints", when the suite runs, then the allocation ratio is at most 0.01. (R49)
  - Given a report in which the case "sample int" has a throughput ratio of 99, when the gate reads it, then the gate fails, and the message holds "sample int" and "99". (R50)
  - Given a report in which the case "sample string" has an allocation ratio of 0.02, when the gate reads it, then the gate fails, and the message holds "sample string" and "0.02". (R50)
  - Given a report in which each throughput ratio is 100 and each allocation ratio is 0.01, when the gate reads it, then the gate passes. (R50)
  - Given each C# code block of README.md, when a test compiles and runs it against the current library, then it compiles with no change and runs. (R51)
- Layers: generator core, runner, collection generators, automatic generator, shrinking, benchmarks, tests, docs
- Tier: hard
- Status: done
- Test rounds: 1
- Review rounds: 2

## S3: Number generators

- Story: As a user, I generate each integer, floating-point, decimal, complex, and Rune value, with special values by default, so that my property meets the values that break code.
- Requirements: R1 (integers), R2 (integer options), R3 (integer shrinking), R4 (BigInteger), R5 (binary floats), R6 (float options), R7 (float special values), R8 (special value opt-out), R9 (float shrinking), R10 (decimal), R12 (Rune), R13 (Complex)
- Acceptance:
  - Given `Gen.SByte()`, `Gen.UInt16()`, `Gen.UInt32()`, `Gen.UInt64()`, `Gen.IntPtr()`, and `Gen.UIntPtr()`, when each samples 100 values for each seed from 0 to 99, then the minimum and the maximum of its type both appear. (R1)
  - Given `Gen.UInt32().Between(10, 20)`, when it samples 1000 values, then each value is from 10 to 20, and 10 and 20 both appear. (R2)
  - Given `Gen.UInt16().GreaterThanEqual(5).LessThanEqual(4)`, when it runs, then the result is a generator error whose message names the bound option. (R2)
  - Given `Gen.UInt64()` and the property "the value is below 1000", when the property fails, then the counterexample is 1000. (R3)
  - Given `Gen.SByte().Between(-50, -10)` and a property that always fails, when the property fails, then the counterexample is -10. (R3)
  - Given `Gen.Int128()` and `Gen.UInt128()`, when each samples 100 values for each seed from 0 to 99, then the minimum and the maximum of its type both appear. (R1)
  - Given `Gen.Int128().Between(-5, 5)`, when it samples 1000 values, then each value is from -5 to 5. (R2)
  - Given `Gen.BigInteger()`, when it samples 100 values for each seed from 0 to 99, then a value below `Int64.MinValue` and a value above `Int64.MaxValue` both appear. (R4)
  - Given `Gen.BigInteger()` and the property "the value is below 10 to the power 30", when the property fails, then the counterexample is 10 to the power 30. (R3, R4)
  - Given `Gen.Half()`, `Gen.Single()`, and `Gen.Double()` with the special values removed, when each samples 1000 values, then a value with a fraction part, a negative value, and a positive value all appear. (R5)
  - Given `Gen.Double()` with the special values removed, when it samples 100 values for each seed from 0 to 99, then a value above 1e300 and a value below -1e300 both appear. (R5)
  - Given `Gen.Double().Between(1.5, 2.5)`, when it samples 1000 values, then each value is from 1.5 to 2.5. (R6)
  - Given `Gen.Double()` and the property "the value is below 3.5", when the property fails, then the counterexample is 4. (R9)
  - Given `Gen.Single()` and a property that always fails, when the property fails, then the counterexample is 0. (R9)
  - Given `Gen.Double()` with no option, when it samples 100 values for each seed from 0 to 99, then at least 90 of the 100 seeds give each of NaN, positive infinity, negative infinity, negative zero, `double.Epsilon`, `double.MinValue`, and `double.MaxValue`. (R7)
  - Given `Gen.Single().Between(-1, 1)`, when it samples 10000 values, then no value is NaN or an infinity, and no value is outside -1 to 1. (R7)
  - Given `Gen.Double()` with the option that removes the special values, when it samples 10000 values, then each value is finite. (R8)
  - Given `Gen.Double()` and the property "the value is not NaN and is below 1e10", when the property fails, then the counterexample is 1e10. (R9)
  - Given `Gen.Decimal()`, when it samples 100 values for each seed from 0 to 99, then a value with a fraction part, zero, `decimal.MinValue`, and `decimal.MaxValue` all appear. (R10)
  - Given `Gen.Decimal().Between(0.10m, 0.20m)`, when it samples 1000 values, then each value is from 0.10 to 0.20. (R6)
  - Given `Gen.Decimal()` with the option that removes the special values, when it samples 10000 values, then no value is `decimal.MinValue` or `decimal.MaxValue`. (R8)
  - Given `Gen.Decimal()` and the property "the value is below 2.5", when the property fails, then the counterexample is 3. (R9)
  - Given `Gen.Rune()`, when it samples 10000 values, then no value is in the surrogate range D800 to DFFF, a value above FFFF appears, and a value below 10000 (hex) appears. (R12)
  - Given `Gen.Rune()` and a property that always fails, when the property fails, then the counterexample is the rune `'a'`. (R12)
  - Given `Gen.Complex()`, when it samples 1000 values, then a value with a NaN part appears, and a value with a fraction in each part appears. (R13)
  - Given `Gen.Complex()` with the option that removes the special values, when it samples 10000 values, then the real part and the imaginary part of each value are finite. (R13)
- Layers: generator api, generator core
- Tier: hard
- Status: done
- Test rounds: 2
- Review rounds: 2

## S4: Time types, containers, and automatic generation

- Story: As a user, I generate time values, dictionaries, and tuples, and `Gen.Create<T>()` works for any base type on both targets, so that a `[Property]` parameter just works.
- Requirements: R11 (time types), R14 (dictionaries), R15 (pairs and tuples), R16 (automatic generation), R17 (replaced generators), R18 (.NET 11 types), R20 (same values on both targets), R21 (replay), R31 (more collections), R32 (interfaces and base classes), R33 (unions)
- Acceptance:
  - Given `Gen.TimeSpan()`, `Gen.DateOnly()`, and `Gen.TimeOnly()`, when each samples 100 values for each seed from 0 to 99, then the `MinValue` and the `MaxValue` of its type both appear. (R11)
  - Given `Gen.DateOnly()` with the bounds 2020-01-01 and 2020-12-31, when it samples 1000 values, then each value is in the year 2020. (R11)
  - Given `Gen.DateTimeOffset()`, when it samples 1000 values, then a value with an offset above zero and a value with an offset below zero both appear. (R11)
  - Given each of the four generators and a property that always fails, when the property fails, then the counterexample is the documented origin of that generator. (R11)
  - Given a dictionary generator of int keys and string values with a minimum count of 2 and a maximum count of 5, when it samples 1000 values, then each count is from 2 to 5. (R14)
  - Given a dictionary generator with an exact count of 3, when it samples 1000 values, then each value has 3 distinct keys. (R14)
  - Given a dictionary generator of int keys and int values and the property "the count is below 2", when the property fails, then the counterexample has 2 entries, with the keys 0 and 1 in any order, and each value is 0. (R14)
  - Given a `KeyValuePair` generator from `Gen.Int32()` and `Gen.Boolean()`, when it samples 100 values, then each value is a `KeyValuePair<int, bool>`. (R15)
  - Given a `ValueTuple` generator for each count from 1 to 7 and a `Tuple` generator for each count from 1 to 7, when each samples 10 values, then each value has that count of elements. (R15)
  - Given a `ValueTuple` generator of two `Gen.Int32()` and the property "the first is below 10 or the second is below 20", when the property fails, then the counterexample is (10, 20). (R15)
  - Given each type of R1, R4, R5, R10, R11, R12, and R13, when `Gen.Create<T>()` and the explicit generator with no option sample 100 values with seed 0, then the two lists are equal. (R16)
  - Given `Dictionary<int, string>`, `IDictionary<int, string>`, `IReadOnlyDictionary<int, string>`, `ImmutableDictionary<int, string>`, `KeyValuePair<int, string>`, `(int, string)`, and `Tuple<int, string>`, when `Gen.Create<T>()` samples 100 values, then each value has that type and no generator error occurs. (R16)
  - Given `Gen.Create<double>()`, when it samples 1000 values, then a value with a fraction part appears. (R17)
  - Given `Gen.Create<uint>()`, when it samples 100 values for each seed from 0 to 99, then 0 and `uint.MaxValue` both appear. (R17)
  - Given a `[Property]` method in an xunit v2 test with a `double` parameter and a `Dictionary<int, string>` parameter, when the test runs, then the method receives values and the test passes. (R16)
  - Given the net11.0 assembly, when a test reads the members of `Gen`, then `Decimal32`, `Decimal64`, `Decimal128`, and `BFloat16` exist, and a generator for `Complex<T>` exists. (R18)
  - Given the net10.0 assembly, when a test reads the members of `Gen`, then none of those members exist. (R18)
  - Given `Gen.BFloat16()` on net11.0 with no option, when it samples 100 values for each seed from 0 to 99, then NaN, positive infinity, and negative zero all appear, and with the opt-out each value is finite. (R18)
  - Given `Gen.Decimal64()` on net11.0 and the property "the value is below 2.5", when the property fails, then the counterexample is 3. (R18)
  - Given `Gen.Create<Decimal128>()` on net11.0 and `Gen.Decimal128()`, when each samples 100 values with seed 0, then the two lists are equal. (R18)
  - Given each generator of this spec that both targets have, when it samples 100 values with seed 0 and size 50 on net10.0 and on net11.0, then the two lists are equal. (R20)
  - Given a failing property of `Gen.Double()`, `Gen.UInt64()`, and a dictionary generator, when the property runs again with the replay string of its failure, then the counterexample is the same and the run has one test case. (R21)
  - Given `Queue<int>`, `Stack<int>`, `SortedSet<int>`, `LinkedList<int>`, `ImmutableArray<int>`, `ReadOnlyCollection<int>`, `Memory<int>`, and `ReadOnlyMemory<int>`, when `Gen.Create<T>()` samples 100 values, then each value has that type and no generator error occurs. (R31)
  - Given `Gen.Create<Queue<int>>()` and the property "the count is below 3", when the property fails, then the counterexample holds 0, 0, 0. (R31)
  - Given an interface `IShape` with the concrete types `Circle` and `Square` in its assembly, when `Gen.Create<IShape>()` samples 100 values, then a `Circle` and a `Square` both appear. (R32)
  - Given an abstract class with no concrete type in its assembly, when `Gen.Create<T>()` runs, then the result is a generator error whose message names the abstract class. (R32)
  - Given `IShape` and a list of candidate types that holds only `Circle`, when the generator samples 100 values, then each value is a `Circle`. (R32)
  - Given a C# union of `Cat`, `Dog`, and `Bird` on net11.0, when `Gen.Create<T>()` samples 100 values, then each of the three cases appears. (R33)
- Layers: generator api, generator core, collection generators, automatic generator, xunit v2 package, build, runner
- Tier: standard
- Status: done
- Test rounds: 2
- Review rounds: 2

## S5: Text, realistic data, and recursive generators

- Story: As a user, I generate strings from a pattern, real Uri and email values, realistic names, and tree-shaped data, so that my inputs look like real data.
- Requirements: R20 (same values on both targets), R34 (pattern strings), R35 (Uri, Version, email), R53 (recursive generators), R54 (recursive automatic generation), R59 (realistic data)
- Acceptance:
  - Given the pattern `[a-c]{2,4}-\d{3}`, when the generator samples 1000 values, then each value matches the whole pattern. (R34)
  - Given the pattern `[a-c]{2,4}-\d{3}` and a property that always fails, when the property fails, then the counterexample is `aa-000`. (R34)
  - Given the pattern `(?<=a)b`, when the generator runs, then the result is a generator error whose message names the lookbehind. (R34)
  - Given `Gen.Uri()`, when it samples 100 values, then each value is absolute, each scheme is http or https, both schemes appear, and at least 50 values are different. (R35)
  - Given `Gen.Version()`, when it samples 1000 values, then a value with 2 parts, a value with 3 parts, and a value with 4 parts all appear. (R35)
  - Given the email generator, when it samples 1000 values, then each value has one `@`, a local part that is not empty, and a domain that holds a dot. (R35)
  - Given `Gen.Create<Uri>()` and `Gen.Uri()`, when each samples 100 values with seed 0, then the two lists are equal, and the same holds for `Version`. (R35)
  - Given the generators for a first name, a last name, a full name, a street address, a city, a country, a phone number, and a company name, when each samples 100 values, then no value is empty and at least 10 values of each are different. (R59)
  - Given the first name generator and a property that always fails, when the property fails, then the counterexample is the first entry of the first name list. (R59)
  - Given the full name generator, when it samples 100 values, then each value is a first name of the list, a space, and a last name of the list. (R59)
  - Given a tree type of leaves and nodes, and a recursive generator with a depth limit of 3, when it samples 1000 values, then no value is deeper than 3 and a value of depth 3 appears. (R53)
  - Given that generator and the property "the tree has no node", when the property fails, then the counterexample is one node whose children are leaves. (R53)
  - Given a class `Node` with a property of type `List<Node>`, when `Gen.Create<Node>()` samples 100 values, then the run ends and no value is deeper than the limit. (R54)
  - Given the pattern, Uri, Version, email, realistic, and recursive generators, when each samples 100 values with seed 0 and size 50 on net10.0 and on net11.0, then the two lists are equal. (R20)
- Layers: generator api, string generators, automatic generator, data
- Tier: hard
- Status: done
- Test rounds: 3
- Review rounds: 3

## S6: Property features

- Story: As a user, I get labels, exhaustive runs, health checks, copyable counterexamples, and time limits, so that a property tells me more and wastes less.
- Requirements: R36 (labels), R37 (cover), R39 (copyable counterexamples), R40 (timeout), R41 (cancellation), R55 (exhaustive small domains), R56 (health checks)
- Acceptance:
  - Given a property of `Gen.Int32().Between(0, 9)` that gives the label "even" to each even value, when it passes with 100 iterations, then the report lists "even" with its share as a percentage. (R36)
  - Given the same property with a failure, when it fails, then the failure report lists "even" with its share. (R36)
  - Given a property that requires a share of 50% for a label that 10% of the test cases have, when the last iteration ends, then the property fails, and the message holds the label, "50%", and the actual share. (R37)
  - Given a property that requires a share of 5% for a label that 50% of the test cases have, when it runs, then it passes. (R37)
  - Given a property of a `bool` and an enum of 3 values with 100 iterations, when it runs, then the test function runs 6 times, with 6 different inputs, and the report says that the run was exhaustive. (R55)
  - Given a property of `Gen.Int32().Between(1, 10)` with 100 iterations, when it runs, then the test function receives each value from 1 to 10 one time. (R55)
  - Given a property of `Gen.Int32().Between(1, 1000)` with 100 iterations, when it runs, then the test function runs 100 times and the report does not say exhaustive. (R55)
  - Given the first property with the option that switches the feature off, when it runs, then the test function runs 100 times. (R55)
  - Given a generator whose filter keeps 5% of its values, when a property of it runs, then the property fails, and the message names the discard check and how to suppress it. (R56)
  - Given a generator that takes 2 seconds for one value, when a property of it runs, then the property fails, and the message names the slow generator check. (R56)
  - Given a property whose precondition is false for each input, when it runs, then the property fails, and the message names the check for a test function that never runs. (R56)
  - Given each of those properties with its check suppressed by name, when it runs, then that check does not fail the property. (R56)
  - Given a failing property with the counterexample 1.5 (a double), "a\"b" (a string), and a list of the ints 1 and 2, when it fails, then the report holds a C# expression for each value, and each expression compiles and equals the value. (R39)
  - Given a failing property whose counterexample is a record `Point(int X, int Y)` with X 1 and Y 2, when it fails, then the report holds `new Point(1, 2)` or an expression that compiles and equals it. (R39)
  - Given a failing property whose counterexample is a `Func<int, int>`, when it fails, then the report holds the text for that value that the report holds today. (R39)
  - Given a property with a time limit of 100 milliseconds whose test function waits 5 seconds for the input 7, when it runs, then the property fails in less than 2 seconds, and the message holds "100" and the input 7. (R40)
  - Given `[Property]` with the time limit option on an xunit v2 test, when the test runs over the limit, then the test fails with the same message. (R40)
  - Given an async property with an int parameter and a `CancellationToken` parameter, when it runs, then the int is generated and the token is not, and the counterexample of a failure lists only the int. (R41)
  - Given that property with a time limit of 100 milliseconds and a test function that waits on the token, when the limit passes, then the token is cancelled. (R41)
- Layers: property api, runner, report, xunit v2 package
- Tier: standard
- Status: done
- Test rounds: 2
- Review rounds: 2

## S7: Stateful testing

- Story: As a user, I run random sequences of commands against a model and get the shortest failing sequence, so that I test a system with state.
- Requirements: R42 (commands), R43 (stateful property), R44 (stateful shrinking), R45 (stateful report)
- Acceptance:
  - Given a counter with the commands "increment" and "decrement", where "decrement" has the precondition that the model is above 0, when the sequence generator samples 1000 sequences, then no sequence holds a "decrement" at a position where the model is 0. (R42)
  - Given a correct counter, when the stateful property runs 100 sequences, then it passes, and each sequence starts with a new system and a new model. (R43)
  - Given a counter whose "increment" adds 2 when the value is 3, when the stateful property runs, then it fails by a postcondition. (R43)
  - Given a counter whose "decrement" throws an exception that the command does not expect, when the stateful property runs, then it fails. (R43)
  - Given the counter whose "increment" adds 2 when the value is 3, when the stateful property fails, then the shrunk sequence is 4 "increment" commands. (R44)
  - Given a failing sequence, when it shrinks, then each precondition holds at its position in each shrunk sequence that runs. (R44)
  - Given that failure, when a test reads the report, then the report lists the 4 commands in order with the inputs of each, and names the fourth command as the command that failed. (R45)
- Layers: stateful api, generator core, runner, shrinking, report
- Tier: hard
- Status: review
- Test rounds: 3
- Review rounds: 3

## S8: The xunit v3 and TUnit packages

- Story: As a user of xunit v3 or TUnit, I use each attribute of the xunit v2 package, so that I run properties in my framework.
- Requirements: R22 (framework packages), R23 (property result), R24 (property forms), R25 (replay and member generators), R26 (sample), R27 (generator snapshot), R28 (data rows), R29 (global configuration), R30 (xunit v2 unchanged)
- Acceptance:
  - Given a pack of GalaxyCheck.Xunit.V3, when a test lists its `lib/` folders, then it finds `net10.0` and `net11.0`. (R22)
  - Given an xunit v3 project with a `[Property]` test that holds, when the project runs, then the result holds one passed test for it. (R23)
  - Given an xunit v3 `[Property]` test "the int is below 10", when the project runs, then the result holds one failed test, and its message holds the counterexample 10 and a replay string, in the text that the same test gives in GalaxyCheck.Xunit. (R23)
  - Given xunit v3 `[Property]` tests with a void, a boolean, a `Property`, and a generator return, and the async form of each, when the project runs, then each test that holds passes and each test that is falsified fails. (R24)
  - Given the test project of GalaxyCheck.Xunit, when it runs, then it has 74 passed tests and no failed test. (R30)
  - Given the xunit v3 package, when a test reads its public types, then `Property`, `Sample`, `GenSnapshot`, `Replay`, and `MemberGen` attributes exist with the options of GalaxyCheck.Xunit. (R22)
  - Given a failing xunit v3 property with `[Replay]` of its replay string, when the project runs, then the test function runs one time and the test fails with the same counterexample. (R25)
  - Given `[Replay("0")]`, when the project runs, then the test fails with the error text that GalaxyCheck.Xunit gives. (R25)
  - Given a parameter with `[MemberGen]` that names a member which returns `Gen.Int32().Between(5, 5)`, when the project runs, then the parameter is 5 in each test case. (R25)
  - Given a `[Sample]` test, when the project runs, then the test fails and its message lists the sampled values. (R26)
  - Given a `[GenSnapshot]` test with a stored snapshot, when the generator changes its values, then the test fails, and with no change it passes. (R27)
  - Given a `[Property]` with `[InlineData(1)]` and `[InlineData(2)]` and one more int parameter, when the project runs, then the result holds 2 tests, the first parameter is 1 or 2, and the second is generated. (R28)
  - Given a global configuration with default iterations of 7, when a `[Property]` with no iterations option runs, then the test function runs 7 times. (R29)
  - Given a pack of GalaxyCheck.TUnit, when a test lists its `lib/` folders, then it finds `net10.0` and `net11.0`. (R22)
  - Given a TUnit project with a `[Property]` test that holds, when the project runs, then the result holds one passed test for it. (R23)
  - Given a TUnit `[Property]` test "the int is below 10", when the project runs, then the result holds one failed test, and its message holds the counterexample 10 and a replay string, in the text that GalaxyCheck.Xunit gives. (R23)
  - Given TUnit `[Property]` tests with a void, a boolean, a `Property`, and a generator return, and the async form of each, when the project runs, then each test that holds passes and each test that is falsified fails. (R24)
  - Given the TUnit package, when a test reads its public types, then `Property`, `Sample`, `GenSnapshot`, `Replay`, and `MemberGen` attributes exist with the options of GalaxyCheck.Xunit. (R22)
  - Given a failing TUnit property with `[Replay]` of its replay string, when the project runs, then the test function runs one time and the test fails with the same counterexample. (R25)
  - Given a parameter with `[MemberGen]` that names a member which returns `Gen.Int32().Between(5, 5)`, when the project runs, then the parameter is 5 in each test case. (R25)
  - Given a `[Sample]` test, when the project runs, then the test fails and its message lists the sampled values. (R26)
  - Given a `[GenSnapshot]` test with a stored snapshot, when the generator changes its values, then the test fails, and with no change it passes. (R27)
  - Given a `[Property]` with `[Arguments(1)]` and `[Arguments(2)]` and one more int parameter, when the project runs, then the result holds 2 tests, the first parameter is 1 or 2, and the second is generated. (R28)
  - Given a global configuration with default iterations of 7, when a `[Property]` with no iterations option runs, then the test function runs 7 times. (R29)
- Layers: xunit v3 package, shared framework core, packaging, tunit package
- Tier: hard
- Status: red
- Test rounds: 0
- Review rounds: 0

## S9: NUnit, MSTest, failure memory, and explicit examples

- Story: As a user of any of the five frameworks, I get my last failure tried first and can pin fixed inputs, so that a known failure runs on each run.
- Requirements: R22 (framework packages), R23 (property result), R24 (property forms), R25 (replay and member generators), R26 (sample), R27 (generator snapshot), R28 (data rows), R29 (global configuration), R38 (failure memory), R52 (explicit examples)
- Acceptance:
  - Given a pack of GalaxyCheck.NUnit, when a test lists its `lib/` folders, then it finds `net10.0` and `net11.0`. (R22)
  - Given an NUnit project with a `[Property]` test that holds, when the project runs, then the result holds one passed test for it. (R23)
  - Given an NUnit `[Property]` test "the int is below 10", when the project runs, then the result holds one failed test, and its message holds the counterexample 10 and a replay string, in the text that GalaxyCheck.Xunit gives. (R23)
  - Given NUnit `[Property]` tests with a void, a boolean, a `Property`, and a generator return, and the async form of each, when the project runs, then each test that holds passes and each test that is falsified fails. (R24)
  - Given the NUnit package, when a test reads its public types, then `Property`, `Sample`, `GenSnapshot`, `Replay`, and `MemberGen` attributes exist with the options of GalaxyCheck.Xunit. (R22)
  - Given a failing NUnit property with `[Replay]` of its replay string, when the project runs, then the test function runs one time and the test fails with the same counterexample. (R25)
  - Given a parameter with `[MemberGen]` that names a member which returns `Gen.Int32().Between(5, 5)`, when the project runs, then the parameter is 5 in each test case. (R25)
  - Given a `[Sample]` test, when the project runs, then the test fails and its message lists the sampled values. (R26)
  - Given a `[GenSnapshot]` test with a stored snapshot, when the generator changes its values, then the test fails, and with no change it passes. (R27)
  - Given a `[Property]` with `[TestCase(1)]` and `[TestCase(2)]` and one more int parameter, when the project runs, then the result holds 2 tests, the first parameter is 1 or 2, and the second is generated. (R28)
  - Given a global configuration with default iterations of 7, when a `[Property]` with no iterations option runs, then the test function runs 7 times. (R29)
  - Given a pack of GalaxyCheck.MSTest, when a test lists its `lib/` folders, then it finds `net10.0` and `net11.0`. (R22)
  - Given an MSTest project with a `[Property]` test that holds, when the project runs, then the result holds one passed test for it. (R23)
  - Given an MSTest `[Property]` test "the int is below 10", when the project runs, then the result holds one failed test, and its message holds the counterexample 10 and a replay string, in the text that GalaxyCheck.Xunit gives. (R23)
  - Given MSTest `[Property]` tests with a void, a boolean, a `Property`, and a generator return, and the async form of each, when the project runs, then each test that holds passes and each test that is falsified fails. (R24)
  - Given the MSTest package, when a test reads its public types, then `Property`, `Sample`, `GenSnapshot`, `Replay`, and `MemberGen` attributes exist with the options of GalaxyCheck.Xunit. (R22)
  - Given a failing MSTest property with `[Replay]` of its replay string, when the project runs, then the test function runs one time and the test fails with the same counterexample. (R25)
  - Given a parameter with `[MemberGen]` that names a member which returns `Gen.Int32().Between(5, 5)`, when the project runs, then the parameter is 5 in each test case. (R25)
  - Given a `[Sample]` test, when the project runs, then the test fails and its message lists the sampled values. (R26)
  - Given a `[GenSnapshot]` test with a stored snapshot, when the generator changes its values, then the test fails, and with no change it passes. (R27)
  - Given a `[Property]` with `[DataRow(1)]` and `[DataRow(2)]` and one more int parameter, when the project runs, then the result holds 2 tests, the first parameter is 1 or 2, and the second is generated. (R28)
  - Given a global configuration with default iterations of 7, when a `[Property]` with no iterations option runs, then the test function runs 7 times. (R29)
  - Given a failing `[Property]` test in each of the five frameworks, when the project runs, then a file in the documented folder of the test project holds the replay string under the full name of the test. (R38)
  - Given that file, when the project runs again, then the first test case of that test is the saved counterexample. (R38)
  - Given that file and a test function that now holds, when the project runs, then the test passes and the file no longer holds the entry. (R38)
  - Given that file and a `[Replay]` attribute with another replay string, when the project runs, then the test runs the replay of the attribute. (R38)
  - Given a global configuration that switches the feature off, when a failing test runs, then no file is written. (R38)
  - Given a property of two ints with `[Example(1, 2)]` and `[Example(3, 4)]`, in each of the five frameworks, when the project runs, then the first two test cases have the inputs (1, 2) and (3, 4), and generated test cases follow. (R52)
  - Given the property "the first int is not 3" with `[Example(3, 4)]`, when the project runs, then the test fails, the message holds the inputs 3 and 4, and the counterexample is not shrunk. (R52)
  - Given a property of two ints with `[Example(1)]`, when the project runs, then the result is one failed test whose message names the second parameter. (R52)
  - Given a property of an int with `[Example("a")]`, when the project runs, then the result is one failed test whose message names the parameter. (R52)
- Layers: nunit package, shared framework core, packaging, mstest package, each framework package, store
- Tier: standard
- Status: red
- Test rounds: 0
- Review rounds: 0

## S10: The source generator and the reference pages

- Story: As a user, I run properties in a NativeAOT test project and find each generator in the docs, so that the library works and is learnable everywhere.
- Requirements: R57 (source generator), R58 (source generator parity), R60 (reference pages)
- Acceptance:
  - Given a test project with the source generator package and `Gen.Create<Order>()` for a record `Order(int Id, string Name)`, when the project is published with trimming and with NativeAOT, then the publish output holds no trim warning and no AOT warning from a GalaxyCheck package, and the published program runs its property with a pass. (R57)
  - Given `Order`, when the generated code and the reflection path each sample 100 values with seed 0, then the two lists are equal. (R58)
  - Given `Gen.Create<T>()` for a type that the source generator does not support, when the project compiles, then the compiler reports a GalaxyCheck diagnostic that names the type. (R58)
  - Given the public generator methods of `Gen` on net11.0, when a test reads `docs/`, then each method name has a reference entry that holds one C# code example. (R60)
  - Given a public generator method with no entry, when the test runs, then it fails and its message names the method. (R60)
- Layers: source generator, automatic generator, packaging, docs, tests
- Tier: hard
- Status: red
- Test rounds: 0
- Review rounds: 0

## S11: Agent experience

- Story: As an LLM agent that writes and runs property tests, I get failures as JSON, errors that state the fix, one compact reference file, and a quiet mode with a fixed seed, so that I fix a test in one step with few tokens.
- Requirements: R63 (machine-readable failures), R64 (errors that state the fix), R65 (agent reference file), R66 (quiet mode with a fixed seed)
- Acceptance:
  - Given the property "the int is below 10" and `Assert`, when it fails, then the message holds one JSON object between the lines `<galaxycheck-failure>` and `</galaxycheck-failure>`, with `counterexample` `["10"]`, `csharp` `["10"]`, a `replay` string that reproduces the failure, and the keys `seed`, `size`, `iterations`, and `shrinks`. (R63)
  - Given the same property as a `[Property]` test in each of the five frameworks, when the project runs, then the JSON object of the failure message also holds `test` with the full name of the test, and `rerun` with one command line, and that command runs only that test. (R63)
  - Given `Gen.UInt16().GreaterThanEqual(5).LessThanEqual(4)`, a list generator with a negative count, `Gen.StringMatching("(?<=a)b")`, and `Gen.Create<T>()` for an abstract class with no concrete type, when each runs, then the last line of the error message starts with `Fix: `. (R64)
  - Given `[Replay("0")]`, a `[MemberGen]` that names no member, and `[Example(1)]` on a property of two ints, in an xunit v2 project, when the project runs, then the last line of the message of each failed test starts with `Fix: `. (R64)
  - Given `docs/llms.txt`, when a test reads it, then it has at most 40,000 characters, and it holds the name of each public generator method of `Gen` and `Gen.Realistic`, each option method of a generator interface, and each attribute, each on a line with a code example. (R65)
  - Given a pack of each of the six library packages, when a test lists the package, then it holds `llms.txt` at its root and an XML documentation file next to each assembly, and the XML file of GalaxyCheck holds a summary for each public type and member. (R65)
  - Given `GALAXYCHECK_AGENT=1` and a property with no seed option, when it runs two times, then the two runs give the same values, and the values are those of seed 0. (R66)
  - Given `GALAXYCHECK_AGENT=1` and an xunit v2 project with one passed and one failed `[Property]` test, when the project runs, then the passed test has no output, and the message of the failed test has at most 40 lines and holds the block of R63. (R66)
  - Given `GALAXYCHECK_SEED=7` and a property with no seed option, when it runs, then its values are those of seed 7, and a property with the seed option 3 gives the values of seed 3. (R66)
- Layers: report, runner, each framework package, docs, packaging
- Tier: standard
- Status: red
- Test rounds: 0
- Review rounds: 0

## S12: The library tests itself

- Story: As a maintainer, I run the tests of the library on the current fast library, so that the test suite is fast and has no old prebuilt copy of the library.
- Requirements: R67 (self-testing)
- Acceptance:
  - Given each project file of `GalaxyCheck.sln`, when a test reads its package references, then none starts with `NebulaCheck` or `GalaxyCheck.Stable`. (R67)
  - Given each `.cs` file of `src/GalaxyCheck.Tests` and `src/GalaxyCheck.Tests.V3`, when a test reads it, then no line holds `NebulaCheck` or `GalaxyCheck.Stable`. (R67)
  - Given `src/GalaxyCheck.Tests` and `src/GalaxyCheck.Tests.V3`, when a test reads each project file, then it has a project reference to `GalaxyCheck.Xunit`, and its `xunit.runner.visualstudio` version equals the version in `src/GalaxyCheck.Xunit.Tests`. (R67)
  - Given the folder `packages-local`, when a test lists it, then no file name starts with `nebulacheck` or `galaxycheck.stable`. (R67)
  - Given `src/GalaxyCheck.Tests` and `src/GalaxyCheck.Tests.V3`, when `dotnet test` runs each on net10.0, then no test fails, `GalaxyCheck.Tests` has at least 400 passed tests, and `GalaxyCheck.Tests.V3` has at least 16 passed tests. (R67)
- Layers: tests, build, packaging
- Tier: standard
- Status: done
- Test rounds: 1
- Review rounds: 1

## S13: The reference suite

- Story: As a maintainer, I run the cases that other property-based testing libraries use on themselves, so that the new core is held to the same laws and to the best known shrinks.
- Requirements: R68 (generator laws), R69 (shrinking challenge), R70 (shrink budget), R71 (distribution), R72 (replay laws), R73 (operator laws), R74 (random programs), R75 (runner equivalence), R76 (exhaustive laws), R77 (bug-finding table)
- Acceptance:
  - Given each public generator of this spec and the seeds 0 to 19, when a property of it fails, then the same seed gives the same values, shrinking ends, no accepted shrink repeats, the shrunk value fails, and a run from its replay makes no further shrink. (R68)
  - Given the challenges reverse, lengthlist, nestedlists, deletion, distinct, coupling, the three difference tests, large union list, and bound5, when each runs on the seeds 0 to 19, then the counterexample is `[0, 1]`, `[900]`, one list of 11 zeros, `([0, 0], 0)`, `[0, 1, -1]` or `[0, 1, 2]`, `[1, 0]`, `[10, 10]`, `[10, 6]`, `[10, 9]`, one list of five distinct ints of the smallest size, and `([-32768], [-1], [], [], [])`. (R69)
  - Given the challenges binheap and calculator, when each runs on the seeds 0 to 19, then the counterexample has no more nodes than `(0, None, (0, (0, None, None), (1, None, None)))` and no more terms than `0 / (0 + 0)`. (R69)
  - Given the property "the int is below 10" and a run whose first failing value is 10, when it shrinks, then the test function runs at most 50 more times. (R70)
  - Given `Gen.Boolean()`, `Gen.Element` of 10 values, and `Gen.Int32().Between(0, 9).WithBias(Gen.Bias.None)`, when each gives 10,000 values, then the chi-squared check at the 0.001 level passes; and given a generator that never gives the value 9, then the same check fails. (R71)
  - Given each integer generator with no option, when it gives 2,000 values on each of the seeds 0 to 19, then its minimum and its maximum each appear. (R71)
  - Given a failing property, when it runs with the replay of its counterexample, then it runs one test case and makes no shrink; with the replay of a property that has another generator, then the error says that the replay is no longer valid; with a replay that is cut short, then the result is an error and not a crash. (R72)
  - Given the seeds 0 to 19, when the functor laws of `Select` and the three monad laws of `SelectMany` are checked on the values and on the shrunk counterexample, then each law holds. (R73)
  - Given 200 random compositions of the generators and operators of the library, when each runs with a time limit of 5 seconds, then none crashes or passes the limit, and each failure shrinks and replays. (R74)
  - Given a property and its async form on the seeds 0 to 19, when each runs, then the iterations and the counterexample are equal; and two runs in two processes give the same text. (R75)
  - Given a domain of 12 values, when an exhaustive run goes through `Select`, through a filter that keeps 6, and through `SelectMany`, then each kept value runs one time; and a domain of 1,000 values with 100 iterations runs 100 random test cases. (R76)
  - Given the eight buggy binary search trees and the correct tree, when the five kinds of properties run on the seeds 0 to 9, then each bug is found by at least one property, and no property fails on the correct tree. (R77)
  - Given `docs/reference-suite.md`, when a test reads it, then it names a source for each requirement from R68 to R77. (D19)
- Layers: tests, generator core, shrinking, runner, docs
- Tier: hard
- Status: done
- Test rounds: 2
- Review rounds: 3

