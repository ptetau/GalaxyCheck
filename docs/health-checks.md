# Health checks

A health check fails a property that wastes its test cases. A property with a problem then fails with a clear
message, and does not pass with too few real test cases.

| Health check | The property fails when |
| --- | --- |
| `TooManyDiscards` | More than 90% of the generated values are discarded, or the generator discards too many values in a row. |
| `SlowGenerator` | Two values of one run each take longer than one second to generate. One slow value can be a pause of the machine, so one slow value does not fail the run. |
| `TestFunctionNeverRuns` | No test case reaches the test function. |

A tripped check throws `GalaxyCheck.HealthCheckException`. Its `Check` property names the check. The message
names the check and tells you how to switch it off:

```csharp
using GalaxyCheck;

try
{
    Property.ForAll(Gen.Int32().Where(x => x == 12345), x => true).Check(seed: 0);
}
catch (HealthCheckException error)
{
    Console.WriteLine(error.Check);
    Console.WriteLine(error.Message);
}
```

This program writes these lines:

```
TooManyDiscards
Health check TooManyDiscards failed: the generator could not make a value, because it discarded too many values in a row. To switch this check off, pass suppressHealthChecks: new[] { HealthCheck.TooManyDiscards }.
```

## Switch a check off

Pass `suppressHealthChecks` to `Check`, `CheckAsync`, `Assert`, or `AssertAsync`:

```csharp
using GalaxyCheck;

var property = Property.ForAll(Gen.Int32().Between(0, 99), x =>
{
    Property.Precondition(x == 0);
    return true;
});

var result = property.Check(seed: 0, suppressHealthChecks: new[] { HealthCheck.TooManyDiscards });
Console.WriteLine(result.Falsified);
```

In a test framework, put `[SuppressHealthCheck(...)]` on the method or on the class:

```csharp
using GalaxyCheck;

public class SlowTests
{
    [Property]
    [SuppressHealthCheck(HealthCheck.SlowGenerator)]
    public bool StringsHaveALength(string text) => text.Length >= 0;
}
```

Usually, it is better to fix the generator than to switch a check off. For example, replace a strict filter with a
generator that makes only valid values.
