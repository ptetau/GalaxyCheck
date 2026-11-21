# Domain: base-types

This design document holds no code.

## Entities and value objects

- Generator (value object): a recipe that turns a seed and a size into a value and its smaller values. It has no identity: two generators with the same recipe are the same.
- NumericGenerator (value object): a Generator of one number type, with a lower bound, an upper bound, a shrink origin, and, for an integer, a bias.
- SpecialValues (value object): the set of special values of a floating-point or decimal type, and a switch that removes them.
- ContainerGenerator (value object): a Generator that is built from element generators: a dictionary, a pair, a tuple, or a collection, with count limits where the container has a count.
- PatternGenerator (value object): a Generator of strings that is built from a regular expression.
- DataList (value object): a fixed English list of realistic values, for example first names.
- RecursiveGenerator (value object): a Generator of a recursive type, with a depth limit.
- AutomaticGenerator (value object): the rule that picks the Generator of a type, by reflection or by generated code. It holds the registered types and the candidate types of an interface.
- Property (value object): a Generator of test cases, with iterations, a time limit, labels, cover rules, examples, and suppressed health checks.
- PropertyRun (entity): one run of a Property. The test name and the seed identify it.
- Report (value object): the text of a PropertyRun result: the counterexample, its C# expressions, the replay string, the label shares, and the note for an exhaustive run.
- ReplayString (value object): the encoded seed, size, and shrink path of one counterexample.
- SavedFailure (entity): the ReplayString that a failed PropertyRun left in the failure file. The full name of the test identifies it.
- Command (value object): a precondition on a model, an action on a system, a model change, and a postcondition.
- StatefulRun (entity): one sequence of Commands against a new system and a new model. Its PropertyRun and its position identify it.
- FrameworkAdapter (value object): the part of a framework package that finds the attributes of a test, starts a PropertyRun, and turns its result into a test result of that framework.
- GlobalConfiguration (value object): the defaults of an assembly: iterations, generators, snapshot settings, and the failure memory switch.
- BenchmarkCase (value object): one measured case, with the throughput and the allocation of the baseline and of the current library, and the two ratios.
- PerformanceGate (value object): the rule that compares each BenchmarkCase with the targets.
- Package (value object): one NuGet package of the solution, with its target assemblies.
- AgentMode (value object): the settings that the environment gives for an agent run: quiet output and a fixed seed.
- FailureRecord (value object): the JSON form of a failed PropertyRun.
- ReferenceEntry (value object): the docs entry of one public generator method, with one example.

## Relationships

- An AutomaticGenerator picks one Generator for each type: the explicit generator with no option.
- A ContainerGenerator has one or more element Generators.
- A Property has one Generator of inputs and many PropertyRuns.
- A PropertyRun has one Report, and at most one SavedFailure.
- A stateful Property has many StatefulRuns, and a StatefulRun has many Commands.
- A FrameworkAdapter starts one PropertyRun for each test, or for each data row of a test.
- A PerformanceGate reads many BenchmarkCases.

## Invariants

- I1 (bounds): A value that is a number is never outside the bounds of its generator.
- I2 (shrink target): A failing value shrinks to the simplest value that still fails. For an integer, that is the value nearest to the origin.
- I3 (determinism): A generator with the same seed and size gives the same values, on each target that has the type.
- I4 (one source of values): The AutomaticGenerator and the explicit generator with no option give the same values.
- I5 (distinct keys): The keys of a generated dictionary are distinct.
- I6 (preconditions): Each precondition holds at its position, in a generated sequence and in each shrunk sequence that runs.
- I7 (same text): A FrameworkAdapter gives the result and the message text that the xunit v2 adapter gives for the same property.
- I8 (depth): No recursive value is deeper than its limit.
- I9 (valid scalar): A generated Rune is never a surrogate.

## State machines

### Machine: PropertyRun

- States: ready, replaying, examples, exhaustive, generating, shrinking, passed, falsified, errored
- Start: ready
- End: passed, falsified, errored
- T1: ready -> replaying on start [a replay string is given or saved] / run only the replayed test case
- T2: ready -> examples on start [no replay string] / run each example in order
- T3: replaying -> falsified on the replayed test case fails / report the same counterexample
- T4: replaying -> passed on the replayed test case holds / remove the saved failure
- T5: examples -> falsified on an example fails / report the inputs of the example with no shrink
- T6: examples -> exhaustive on each example holds [the inputs have at most as many values as iterations, and the feature is on] / try each value one time
- T7: examples -> generating on each example holds [the inputs have more values than iterations, or the feature is off] / run random test cases
- T8: exhaustive -> passed on each value holds / report that the run was exhaustive
- T9: exhaustive -> shrinking on a value fails / keep the failing value
- T10: generating -> shrinking on a test case fails / keep the failing value
- T11: generating -> passed on the last iteration [each cover rule is met] / report the label shares
- T12: generating -> falsified on the last iteration [a cover rule is not met] / report the label, the required share, and the actual share
- T13: generating -> falsified on the time limit passes / cancel the token, report the limit and the input
- T14: generating -> errored on a health check trips [the check is not suppressed] / report the name of the check
- T15: shrinking -> falsified on no simpler value fails / report the counterexample, its C# expressions, and the replay string
- X1: ready on start refused: an invalid replay string gives the replay error text
- X2: examples on an example with the wrong number or type of inputs refused: an error that names the parameter
- X3: ready on start refused: a generator error that names the option, the type, or the construct

### Machine: SavedFailure

- States: absent, saved
- Start: absent
- End: none
- T1: absent -> saved on a property is falsified [the failure memory is on] / write the replay string under the full name of the test
- T2: saved -> absent on the saved replay holds / remove the entry
- T3: saved -> saved on the saved replay fails / keep the entry

### Machine: StatefulRun

- States: fresh, running, passed, failed
- Start: fresh
- End: passed, failed
- T1: fresh -> running on the first command / make a new system and a new model
- T2: running -> running on a command [the postcondition holds] / change the model
- T3: running -> failed on a postcondition fails, or an action throws an exception that the command does not expect / keep the sequence for shrinking
- T4: running -> passed on the last command holds / end the sequence
- X1: running on a command whose precondition is false refused: the sequence generator and the shrinker never place the command at that position

## Bounded contexts and boundary

- Inside: the generator library (generators, shrinking, the automatic generator, properties, the runner, the report, stateful testing), the five framework packages, the source generator, the benchmark suite and its gate, the reference pages, the CI workflow, and the package sources.
- Outside: the test frameworks themselves (xunit v2, xunit v3, TUnit, NUnit, MSTest), the .NET runtime and compiler, the Roslyn analyzer package, the Visual Studio extension, and the code that a user tests.
- Actors: a user, who writes generators and properties in C#; a test framework, which finds a test and calls the FrameworkAdapter; the C# compiler, which runs the source generator; a maintainer, who runs the benchmark command; GitHub Actions, which runs the CI workflow; nuget.org, which serves packages.

## Requirement map

- R1: NumericGenerator, I1
- R2: NumericGenerator, I1, PropertyRun.X3
- R3: NumericGenerator, I2, PropertyRun.T10, PropertyRun.T15
- R4: NumericGenerator, I1, I2
- R5: NumericGenerator
- R6: NumericGenerator, I1
- R7: SpecialValues, I1
- R8: SpecialValues
- R9: NumericGenerator, SpecialValues, I2
- R10: NumericGenerator, SpecialValues
- R11: NumericGenerator, I1, I2
- R12: Generator, I9, I2
- R13: ContainerGenerator, SpecialValues
- R14: ContainerGenerator, I5, I2
- R15: ContainerGenerator, I2
- R16: AutomaticGenerator, I4, FrameworkAdapter
- R17: AutomaticGenerator, I4
- R18: NumericGenerator, SpecialValues, AutomaticGenerator, Package
- R19: Package, boundary: GitHub Actions
- R20: Generator, I3
- R21: ReplayString, PropertyRun.T1, PropertyRun.T3
- R22: FrameworkAdapter, Package, GlobalConfiguration
- R23: FrameworkAdapter, Report, I7, PropertyRun.T11, PropertyRun.T15
- R24: FrameworkAdapter, Property, I7
- R25: FrameworkAdapter, ReplayString, I7, PropertyRun.T1, PropertyRun.T3, PropertyRun.X1
- R26: FrameworkAdapter, Report, I7
- R27: FrameworkAdapter, GlobalConfiguration, I7
- R28: FrameworkAdapter, AutomaticGenerator
- R29: GlobalConfiguration, FrameworkAdapter
- R30: FrameworkAdapter, I7
- R31: AutomaticGenerator, ContainerGenerator, I2
- R32: AutomaticGenerator, PropertyRun.X3
- R33: AutomaticGenerator
- R34: PatternGenerator, I2, PropertyRun.X3
- R35: Generator, AutomaticGenerator, I4
- R36: Property, Report, PropertyRun.T11
- R37: Property, Report, PropertyRun.T11, PropertyRun.T12
- R38: SavedFailure, GlobalConfiguration, FrameworkAdapter, SavedFailure.T1, SavedFailure.T2, SavedFailure.T3, PropertyRun.T1, PropertyRun.T4
- R39: Report, PropertyRun.T15
- R40: Property, Report, PropertyRun.T13
- R41: Property, PropertyRun.T13
- R42: Command, I6, StatefulRun.X1
- R43: StatefulRun, Command, StatefulRun.T1, StatefulRun.T2, StatefulRun.T3, StatefulRun.T4
- R44: StatefulRun, I6, I2, StatefulRun.X1
- R45: Report, StatefulRun, StatefulRun.T3
- R46: BenchmarkCase
- R47: BenchmarkCase
- R48: BenchmarkCase, Generator, PropertyRun
- R49: BenchmarkCase, Generator, PropertyRun
- R50: PerformanceGate, BenchmarkCase
- R51: Generator, Property, boundary: a user
- R52: Property, FrameworkAdapter, PropertyRun.T2, PropertyRun.T5, PropertyRun.T7, PropertyRun.X2
- R53: RecursiveGenerator, I8, I2
- R54: AutomaticGenerator, RecursiveGenerator, I8
- R55: Property, Report, PropertyRun.T6, PropertyRun.T8, PropertyRun.T9
- R56: Property, Report, PropertyRun.T14
- R57: AutomaticGenerator, Package, boundary: the C# compiler
- R58: AutomaticGenerator, I4, boundary: the C# compiler
- R59: DataList, Generator, I2
- R60: ReferenceEntry
- R61: boundary: GitHub Actions, Package
- R62: Package, boundary: nuget.org
- R63: FailureRecord, Report, FrameworkAdapter, PropertyRun.T15
- R64: Generator, FrameworkAdapter, PropertyRun.X1, PropertyRun.X2, PropertyRun.X3
- R65: ReferenceEntry, Package
- R66: AgentMode, Report, PropertyRun.T11
