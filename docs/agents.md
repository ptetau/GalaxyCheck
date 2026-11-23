# Use by LLM agents

GalaxyCheck gives an LLM agent four aids. A person can use each aid too:

- A failure block of JSON in each failure message.
- Error messages that state the fix.
- One compact reference file.
- A quiet mode with a fixed seed.

## The failure block

Each failure message of a property holds one JSON object between the lines `<galaxycheck-failure>` and
`</galaxycheck-failure>`. The message of `Assert` holds the block, and so does the message of a failed
`[Property]` test in each of the five test frameworks. The JSON object is on one line.

A failed xunit v2 test gives this block:

```
<galaxycheck-failure>
{"counterexample":["(Item1 = 10, Item2 = 15)"],"csharp":["(10, 15)"],"replay":"ArHH1ZcECgIJAA==","seed":1123378097,"size":10,"iterations":11,"shrinks":1,"test":"AboutShrinking.MyProperty","rerun":"dotnet test --filter FullyQualifiedName=AboutShrinking.MyProperty"}
</galaxycheck-failure>
```

| Field | Value |
| --- | --- |
| `counterexample` | The text of each counterexample value, one string for each parameter. |
| `csharp` | The C# expression of each value, or `null` for a value with no expression. |
| `replay` | The replay string. Put it in `[Replay("...")]` or pass it as `replay:`. |
| `seed` | The seed of the failing test case. |
| `size` | The size of the failing test case. |
| `iterations` | The count of test cases that ran. |
| `shrinks` | The count of shrinks. |
| `test` | The full name of the test. Only in a test framework. |
| `rerun` | One command line that runs only that test. Only in a test framework. |
| `example` | The `[Example(...)]` attribute of a failed example. Only for an example. |

A failed `[Example]` has no replay string. Its block holds `"replay":null`, `"seed":null`, and `"size":null`.

The `rerun` command is one bash command. Run it in the folder of the test project. For xunit, NUnit, and MSTest,
it is a `dotnet test --filter` command. For TUnit, it is a `dotnet run` command with `--treenode-filter`. MSTest
and TUnit cannot select one data row, so the command runs each row of that method.

When a test calls `.Assert()` on an inner property, the block belongs to the test. The inner failure stays in the
text between the lines `[inner failure, not of this test]` and `[end of inner failure]`.

## Errors that state the fix

The message of each generator error ends with a line that starts with `Fix:`. The same holds for each error of
`[Replay]`, `[MemberGen]`, and `[Example]`. The `Fix:` line gives a corrected call, or the option to change:

```
Error during generation: 'min' cannot be greater than 'max'
Fix: give a minimum that is not above the maximum, for example .GreaterThanEqual(4).LessThanEqual(5), or swap the two bounds.
```

## The reference file

The file [llms.txt](llms.txt) lists each public generator, each option, each property option, and each attribute,
with a one-line example for each. The file has at most 40,000 characters, so an agent can read all of the file.
Each GalaxyCheck package holds `llms.txt` at its root. Each package also holds the XML documentation file of each
assembly.

## The quiet mode

Set the environment variable `GALAXYCHECK_AGENT=1` for a test run:

```shell
GALAXYCHECK_AGENT=1 dotnet test
```

In the quiet mode:

- A property with no seed option uses seed 0. Thus, each run gives the same test cases.
- A passed property writes no output.
- The failure message of a `[Property]` or `[Sample]` test has at most 40 lines, and it still holds the failure
  block. A test framework can add a line or two.

## A fixed seed

Set `GALAXYCHECK_SEED` to give each property with no seed option the same seed:

```shell
GALAXYCHECK_SEED=7 dotnet test
```

A seed option of the property wins over both environment variables. A value of `GALAXYCHECK_SEED` that is not an
integer fails the run, with a `Fix:` line.

```csharp
using GalaxyCheck;

Environment.SetEnvironmentVariable("GALAXYCHECK_SEED", "7");

var first = Property.ForAll(Gen.Int32(), x => x < 1000).Check();
var second = Property.ForAll(Gen.Int32(), x => x < 1000).Check();

Console.WriteLine(first.Counterexample!.Replay == second.Counterexample!.Replay);
```

The program writes `True`.
