# Labels and cover

Labels show the distribution of the test cases. A cover rule sets a minimum share of test cases with a label.
Use them to make sure that a property tests the cases that you care about.

## Labels

`property.Label(label, when)` gives the label to each test case for which `when` is true. The report of the run
lists each label with its share of the test cases. `CheckResult<T>.Labels` holds each share as a percentage.

```csharp
using GalaxyCheck;

var result = Property.ForAll(Gen.Int32().Between(0, 9), x => x * 2 % 2 == 0)
    .Label("even", x => x % 2 == 0)
    .Label("zero", x => x == 0)
    .Check();

Console.WriteLine(result.Report);
Console.WriteLine(result.Labels["even"]);
```

This program writes these lines:

```
Passed after 10 tests
Exhaustive: the run was exhaustive, and each of the 10 possible inputs ran one time
Labels:
    50% even
    10% zero
50
```

The report of a failed property lists the labels too.

## Cover

`property.Cover(label, minPercent, when)` gives the label, and it sets the minimum share `minPercent`. When the
share is lower after the last iteration, the property fails. The message names the label, the necessary share,
and the actual share:

```csharp
using GalaxyCheck;

try
{
    Property.ForAll(Gen.Int32().Between(0, 9), x => true)
        .Cover("zero", 50, x => x == 0)
        .Assert();
}
catch (GalaxyCheck.Runners.PropertyFailedException error)
{
    Console.WriteLine(error.Message.Split('\n')[2]);
}
```

This program writes this line:

```
---- Cover not met: label "zero" requires 50% of the test cases, but only 10% (1 of 10) had it.
```

`Label` and `Cover` work on `Property<T>` and on `AsyncProperty<T>`.
