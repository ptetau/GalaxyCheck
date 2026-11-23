# Failure memory

When a property fails in a test framework, GalaxyCheck saves the replay string of the failure. The next run of
that test tries the saved failure first. Thus, a failure that you saw once stays until you fix it, also when the
next run has another seed.

Failure memory works in xunit v2, xunit v3, TUnit, NUnit, and MSTest. It is on by default. The core methods
`Check` and `Assert` do not use it.

## The file

GalaxyCheck saves each failure in the file `.galaxycheck/failures.json` in the folder of the test project. The
file is a JSON object. Each key is the full name of a test, and each value is a replay string:

```json
{
  "MyTests.AboutShrinking.MyProperty": "ArHH1ZcECgIJAA=="
}
```

The rules of the file are:

- When a property fails, GalaxyCheck writes its replay string under the key of the test.
- The next run of the test runs the saved replay first. When the saved replay still fails, the test fails on
  that counterexample.
- When the saved replay passes, GalaxyCheck removes the entry and runs the normal test cases.
- A `[Replay]` attribute on the test takes precedence over the file.

You can commit the file to share failures with your team, or add `.galaxycheck/` to `.gitignore`.

## How a key works

A key is `Namespace.Class.Method`. A data row adds its values after the method, and a class with constructor
arguments adds them after the class. For example: `MyTests.Fixture(5).Method(1, "a")`.

Only a value with a stable text goes into a key: `null`, a `bool`, a number, a `char`, a `string`, an enum, or a
type. Any other value goes into the key as its type name, with the index of its row when the framework gives
one. For example: `MyTests.Class.Method(MyTests.Database#1)`. Thus, the key of a test does not change between
runs.

## Switch it off

Set `FailureMemory.Enabled` to `false` in the global configuration of the test assembly:

```csharp
using GalaxyCheck.Configuration;

public class NoFailureMemory : IConfigureGlobal
{
    public void Configure(IGlobalConfiguration instance)
    {
        instance.FailureMemory.Enabled = false;
    }
}
```

To forget one failure, remove its entry from the file, or remove the file.

## When GalaxyCheck finds no project folder

The build records the folder of the test project in the test assembly. When the build records no folder,
GalaxyCheck looks for the project file above the test assembly. When GalaxyCheck finds no project folder, it
saves nothing, and the first failure message holds a note with a `Fix:` line. A project that sets
`GenerateAssemblyInfo` to `false` records no folder.
