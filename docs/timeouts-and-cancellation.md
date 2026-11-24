# Timeouts and cancellation

A time limit stops a test case that runs too long. When one test case runs over the limit, the property fails.
The message names the limit and the input of that test case.

## Set a time limit

Pass `timeout` to `Check`, `CheckAsync`, `Assert`, or `AssertAsync`:

```csharp
using GalaxyCheck;

try
{
    Property.ForAll(Gen.Int32().Between(0, 100), x =>
    {
        if (x > 50) Thread.Sleep(300);
    }).Assert(seed: 0, timeout: TimeSpan.FromMilliseconds(100));
}
catch (GalaxyCheck.Runners.PropertyFailedException error)
{
    Console.WriteLine(error.Message);
}
```

The message of this program starts with these lines:

```
Falsified after 64 tests (0 shrinks)
Reproduction: AgA/AWQ=
Counterexample: 100
Counterexample as C#:
    100

---- Timed out after 100 ms
```

A test case over the time limit does not shrink, because each shrink could wait for the limit again.

In a test framework, set `TimeoutMilliseconds` on `[Property]`:

```csharp
using GalaxyCheck;

public class FastTests
{
    [Property(TimeoutMilliseconds = 500)]
    public bool SortIsFast(List<int> xs) => xs.Order().Count() == xs.Count;
}
```

The runner runs a synchronous test function on its own thread. A function that does not return cannot hold the
run, but the runner does not stop that thread.

## Cancellation

An async property can take a `CancellationToken`. GalaxyCheck does not generate the token. The token is cancelled
when the time limit passes:

```csharp
using GalaxyCheck;

var property = Property.ForAllAsync(Gen.Int32().Between(1, 10), async (int x, CancellationToken token) =>
{
    await Task.Delay(x, token);
    return true;
});

await property.AssertAsync(timeout: TimeSpan.FromSeconds(1));
Console.WriteLine("The property holds.");
```

In a test framework, a `CancellationToken` parameter of an async `[Property]` method gets the same token. The
token is also cancelled when the test framework cancels the test:

```csharp
using GalaxyCheck;

public class CancellableTests
{
    [Property(TimeoutMilliseconds = 1000)]
    public async Task DelayEnds(byte milliseconds, CancellationToken token)
    {
        await Task.Delay(milliseconds % 20, token);
    }
}
```
