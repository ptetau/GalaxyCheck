# Explicit examples

`[Example(...)]` gives fixed inputs to a property. Each example runs before the generated test cases. Use an
example for a known edge case, or for a bug that a property found before.

The attribute works in xunit v2, xunit v3, TUnit, NUnit, and MSTest.

```csharp
using GalaxyCheck;

public class DivisionTests
{
    [Property]
    [Example(0, 1)]
    [Example(int.MinValue, -1)]
    public bool DivisionIsTheInverseOfMultiplication(int x, int y)
    {
        Property.Precondition(y != 0);
        return (long)x * y / y == x;
    }
}
```

The rules of an example are:

- An example gives one value for each parameter, in the order of the parameters.
- A failing example fails the test. The message holds the inputs of the example and the attribute.
- An example does not shrink, and it has no replay string.
- An example with the wrong number of values, or a value of the wrong type, is one failed test. The message names
  the parameter, and its last line starts with `Fix:`.

This test fails on its example, because `Math.Abs(int.MinValue)` throws an `OverflowException`:

```csharp
using GalaxyCheck;

public class AbsoluteValueTests
{
    [Property]
    [Example(int.MinValue)]
    public bool AbsoluteValueIsNotNegative(int x) => Math.Abs(x) >= 0;
}
```

The failure message starts with these lines:

```
Falsified after 1 test (0 shrinks)
Reproduction:
    [Example(int.MinValue)]
Counterexample: -2147483648
Counterexample as C#:
    int.MinValue

---- Negating the minimum value of a twos complement number is invalid.
```

The JSON block of the message holds `"kind":"example"`, `"replay":null`, and `"example":"[Example(int.MinValue)]"`. Read
[Use by LLM agents](agents.md).

In TUnit, put `[Example]` beside `[Test, PropertyTest]`. In MSTest, the method returns `void` or `Task`.
