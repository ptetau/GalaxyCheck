# Generators for the time types

GalaxyCheck has a generator for each time type of .NET. Each generator makes values over the full range of its
type, and each generator has an inclusive lower bound and an inclusive upper bound.

| Type | Generator | A failing value shrinks to |
| --- | --- | --- |
| `TimeSpan` | `Gen.TimeSpan()` | `TimeSpan.Zero` |
| `DateTimeOffset` | `Gen.DateTimeOffset()` | `2000-01-01T00:00:00+00:00` |
| `DateOnly` | `Gen.DateOnly()` | `2000-01-01` |
| `TimeOnly` | `Gen.TimeOnly()` | `00:00` |
| `DateTime` | `Gen.DateTime()` | the lower bound |

When the bounds do not hold the origin, a failing value shrinks to the bound that is nearest to the origin.

`Gen.DateTimeOffset()` makes offsets from -14 hours to +14 hours, not only zero.

## Options

`Gen.TimeSpan()`, `Gen.DateTimeOffset()`, `Gen.DateOnly()`, `Gen.TimeOnly()`, and `Gen.DateTime()` have these
options:

- `From(min)`: the inclusive minimum.
- `Until(max)`: the inclusive maximum.
- `Within(x, y)`: both bounds, inclusive.

```csharp
using GalaxyCheck;

var officeHours = Gen.TimeOnly().Within(new TimeOnly(9, 0), new TimeOnly(17, 0));
var thisCentury = Gen.DateOnly().From(new DateOnly(2000, 1, 1)).Until(new DateOnly(2099, 12, 31));
var shortDelays = Gen.TimeSpan().Within(TimeSpan.Zero, TimeSpan.FromMinutes(5));
var afterEpoch = Gen.DateTimeOffset().From(DateTimeOffset.UnixEpoch);

Property.ForAll(officeHours, t => t.Hour >= 9 && t <= new TimeOnly(17, 0)).Assert();
Property.ForAll(thisCentury, d => d.Year is >= 2000 and <= 2099).Assert();
Property.ForAll(shortDelays, d => d <= TimeSpan.FromMinutes(5)).Assert();
Property.ForAll(afterEpoch, t => t >= DateTimeOffset.UnixEpoch).Assert();

var firstFailure = Gen.DateOnly().Minimum(seed: 0, pred: d => d.Year > 2010);
Console.WriteLine(firstFailure.ToString("yyyy-MM-dd"));
```

The program writes `2011-01-01`: the date after 2010 that is nearest to the origin.
