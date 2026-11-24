# Exhaustive runs

When the inputs of a property have at most as many values as the property has iterations, GalaxyCheck tries each
value one time. The run is then exhaustive: it tests all of the input space, and it does not repeat a value.

An exhaustive run works for `bool`, enums, bounded integers, `Gen.Element`, and tuples of these. It also works
through `Select`, `Where`, and `SelectMany`. When the input space is larger than the iterations, the run makes
the normal random test cases.

```csharp
using GalaxyCheck;

var property = Property.ForAll(Gen.Boolean(), Gen.Enum<DayOfWeek>(), (flag, day) => true);
var result = property.Check();

Console.WriteLine(result.Iterations);
Console.WriteLine(result.Exhaustive);
Console.WriteLine(result.Report);
```

This program writes these lines:

```
14
True
Passed after 14 tests
Exhaustive: the run was exhaustive, and each of the 14 possible inputs ran one time
```

The input space has 2 × 7 = 14 values. Thus, the run has 14 test cases, not 100.

## Switch it off

Pass `exhaustive: false` to `Check` or `Assert`, or set `Exhaustive = false` on `[Property]`. The run then makes
random test cases, also for a small input space:

```csharp
using GalaxyCheck;

public class DayTests
{
    [Property(Exhaustive = false, Iterations = 50)]
    public bool EachDayHasAName(DayOfWeek day) => day.ToString().Length > 0;
}
```
