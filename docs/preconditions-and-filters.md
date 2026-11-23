# Preconditions and filters

A property sometimes holds only for some inputs. GalaxyCheck has two ways to discard the other inputs:

- A filter on the generator: `gen.Where(condition)`. The generator discards a value that fails the condition,
  and it makes a new value.
- A precondition in the test function: `Property.Precondition(condition)`. A false condition discards the test
  case. The discarded test case does not count as an iteration.

## Preconditions

A precondition is short when the condition joins two or more parameters:

```csharp
using GalaxyCheck;

var property = Property.ForAll(Gen.Int32(), Gen.Int32(), (x, y) =>
{
    Property.Precondition(x != y);
    return x.GetHashCode() != y.GetHashCode();
});

var result = property.Check(seed: 0);
Console.WriteLine($"{result.Iterations} test cases, {result.Discards} discards");
```

`Property.Precondition` also works in a `[Property]` method of each test framework:

```csharp
using GalaxyCheck;

public class DivisionTests
{
    [Property]
    public bool RemainderIsSmallerThanTheDivisor(int x, int y)
    {
        Property.Precondition(y > 0);
        return Math.Abs(x % y) < y;
    }
}
```

## Adaptive filters

A filter that discards a value tries again at other sizes. The filter learns the sizes at which values pass, and
the next retries start near those sizes. Thus, a filter that passes only for large values, or only for small
values, finds a value fast. A precondition uses the same search.

```csharp
using GalaxyCheck;

var bigNumbers = Gen.Int32().Where(x => x > 1_000_000);

var result = Property.ForAll(bigNumbers, x => x > 1_000_000).Check(seed: 0);
Console.WriteLine($"{result.Iterations} test cases, {result.Discards} discards");
```

The program writes `100 test cases, 206 discards`. A small size gives small values, so the filter discards
values until it finds the sizes that pass. A shrunk value obeys the filter too.

## Too many discards

When more than 90% of the generated values are discarded, the property fails with the health check
`TooManyDiscards`. Read [Health checks](health-checks.md). A filter that keeps few values is slow, so build the
value directly when you can. For example, `Gen.Int32().Between(0, 100).Select(x => x * 2)` makes even values with
no discard.
