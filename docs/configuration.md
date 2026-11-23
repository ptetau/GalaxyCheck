# Configuration

A test assembly sets its defaults in one class that implements `GalaxyCheck.Configuration.IConfigureGlobal`.
Each framework package finds the class and calls `Configure` before the first test. The same class works in
xunit v2, xunit v3, TUnit, NUnit, and MSTest.

```csharp
using GalaxyCheck.Configuration;

public class TestConfiguration : IConfigureGlobal
{
    public void Configure(IGlobalConfiguration instance)
    {
        instance.Properties.DefaultIterations = 200;
        instance.Properties.DefaultShrinkLimit = 1000;
        instance.FailureMemory.Enabled = true;
    }
}
```

## The settings

| Setting | Default | Effect |
| --- | --- | --- |
| `Properties.DefaultIterations` | 100 | The count of test cases of a property with no `Iterations` option. |
| `Properties.DefaultShrinkLimit` | 100 | The maximum count of shrinks of a property with no `ShrinkLimit` option. |
| `Properties.Seed` | null | The seed of each property with no `Seed` option. |
| `GenSnapshots.DefaultIterations` | 3 | The count of snapshots of a `[GenSnapshot]` test with no `Iterations` option. |
| `GenSnapshots.DefaultSize` | 100 | The size of a `[GenSnapshot]` test with no `Size` option. |
| `GenSnapshots.AssertSnapshotMatches` | null | The function that compares a snapshot with the stored snapshot. |
| `FailureMemory.Enabled` | true | Saves each failure and tries it first in the next run. Read [Failure memory](failure-memory.md). |

## Generator snapshots

A `[GenSnapshot]` test samples its parameters and compares the values with a stored snapshot. Use it to find a
change to the values of a generator. The MSTest package has no `[GenSnapshot]`.

GalaxyCheck has no store for snapshots. Set `AssertSnapshotMatches` to a function that stores a snapshot the
first time, and throws when a later snapshot differs:

```csharp
using GalaxyCheck;
using GalaxyCheck.Configuration;

public class SnapshotConfiguration : IConfigureGlobal
{
    public void Configure(IGlobalConfiguration instance)
    {
        instance.GenSnapshots.AssertSnapshotMatches = snapshot =>
        {
            var file = $"{snapshot.TestClassName}.{snapshot.TestMethodName}.{snapshot.Seed}.snap";
            var text = System.Text.Json.JsonSerializer.Serialize(snapshot.Output);
            if (!File.Exists(file))
            {
                File.WriteAllText(file, text);
            }
            else if (File.ReadAllText(file) != text)
            {
                throw new Exception($"The snapshot {file} differs.");
            }

            return Task.CompletedTask;
        };
    }
}

public class SnapshotTests
{
    [GenSnapshot(Iterations = 5)]
    public string Greeting(int x) => $"Hello {x}";
}
```

When `AssertSnapshotMatches` is not set, a `[GenSnapshot]` test fails with the message
"Configuration value AssertSnapshotMatches needs to be set".
