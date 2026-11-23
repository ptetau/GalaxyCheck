# Replay and counterexamples

Each failure message holds a replay string and a C# expression of the counterexample. The replay string runs the
failing test case again. The C# expression lets you copy the counterexample into a unit test.

## Replay strings

A replay string holds the exact choices of the counterexample, not only the seed. A run with the replay string
runs one test case and makes no shrink. Thus, a debugger goes directly to the counterexample.

In the core library, pass the string as `replay`:

```csharp
using GalaxyCheck;

var property = Property.ForAll(Gen.Int32(), x => x < 10);

var first = property.Check(seed: 0);
var again = property.Check(replay: first.Counterexample!.Replay);

Console.WriteLine(first.Counterexample.Replay);
Console.WriteLine($"{again.Iterations} test case, counterexample {again.Counterexample!.Value}");
```

This program writes these lines:

```
AgAMAgoA
1 test case, counterexample 10
```

In a test framework, put `[Replay("...")]` on the property:

```csharp
using GalaxyCheck;

public class ReplayTests
{
    [Property]
    [Replay("AgAMAgoA")]
    public bool IntsAreSmall(int x) => x < 10;
}
```

A replay string belongs to one property. When the generators of the property change, the replay string can stop
working. A replay string that does not decode gives an error, and the last line of the error starts with `Fix:`.

## Copyable counterexamples

The failure message has a section `Counterexample as C#:`. It holds a C# expression for each value. The
expression compiles, and it equals the value, for these types:

- The base types, strings, and enums.
- Collections.
- A type with a public constructor or settable properties.

The section holds only valid C#. When a value has no expression, for example a function, the commands of a
stateful test, or a tuple that holds such a value, the message has no section, and the `Counterexample` line
shows the values. In the JSON block, the expression of such a value is `null`.

```csharp
using GalaxyCheck;

try
{
    Property.ForAll(Gen.Create<Order>(), order => order.Id < 100).Assert(seed: 0);
}
catch (GalaxyCheck.Runners.PropertyFailedException error)
{
    Console.WriteLine(error.Message);
}

public record Order(int Id, string Name);
```

The message of this program starts with these lines:

```
Falsified after 22 tests (1 shrink)
Reproduction: AgAVA2QAAA==
Counterexample: { Id = 100, Name = "" }
Counterexample as C#:
    new global::Order(100, "")
```

`CSharpExpression.TryFormat(value, out expression)` gives the same expression for any value. It returns false when
the value has no expression:

```csharp
using GalaxyCheck;

if (CSharpExpression.TryFormat(new List<int> { 1, 2 }, out var expression))
{
    Console.WriteLine(expression);
}
```

The program writes `new global::System.Collections.Generic.List<int> { 1, 2 }`.

The expression escapes each string and each char. Thus, a value cannot put code into the expression.

## Failure memory

Each framework package also saves the replay string of a failure, and the next run tries it first. Read
[Failure memory](failure-memory.md).
