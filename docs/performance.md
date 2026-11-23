# Performance

The current engine of GalaxyCheck is more than 100 times faster than the old engine in each benchmark case. It
also allocates less than 1% of the memory of the old engine for each value.

## What changed

- A generator makes a value directly from a source of choices. The old engine built a tree of example values
  and their shrinks for each value.
- The shrinker works on the recorded choices of a counterexample, and it replays the generator. Thus, a value
  that passes costs no shrink tree.
- A replay string holds the recorded choices of the counterexample.

As a result, the values of a seed and the replay strings differ from the old engine. Read
[Migration from an older GalaxyCheck](migration.md).

## The measured numbers

The benchmark suite in `src/GalaxyCheck.Benchmarks` runs each case on the old engine (commit `f127a67`) and on
the current library. Both run in the same run, on the same machine. A value is one test case of a check, one sampled
value, or one run that finds and shrinks a counterexample.

These numbers come from one run on a Linux machine with 4 cores of an Intel Xeon processor at 2.10 GHz, on
net10.0. Each number is rounded.

| Case | Old values each second | Current values each second | Throughput ratio | Old bytes each value | Current bytes each value | Allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| check int | 34,082 | 15,511,173 | 455 | 10,607 | 1.6 | 0.00015 |
| sample int | 47,774 | 34,038,438 | 712 | 10,566 | 5.0 | 0.00048 |
| sample string | 1,426 | 2,081,696 | 1,460 | 186,516 | 81 | 0.00043 |
| sample list of ints | 6,581 | 4,115,247 | 625 | 53,704 | 86 | 0.0016 |
| sample record | 919 | 849,727 | 924 | 384,019 | 284 | 0.00074 |
| shrink list of ints | 232 | 33,115 | 143 | 3,436,681 | 14,231 | 0.0041 |

The cases are:

- check int: `Property.ForAll(Gen.Int32(), x => true).Check(iterations: 1000)`.
- sample int, sample string, and sample list of ints: `Sample(iterations: 1000)` of `Gen.Int32()`,
  `Gen.String()`, and `Gen.Int32().ListOf()`.
- sample record: `Sample(iterations: 1000)` of `Gen.Create<T>()` for a record with an `int`, a `string`, a
  `DateTime`, and a `Guid`.
- shrink list of ints: `Property.ForAll(Gen.Int32().ListOf(), xs => xs.Count < 5 || xs.Sum() < 100).Check(seed: i)`
  for the seeds 0 to 19. Each check finds and shrinks a counterexample.

## Run the benchmarks

This command runs the suite and writes a JSON report:

```shell
dotnet run -c Release --project src/GalaxyCheck.Benchmarks -- --report report.json
```

The command needs a git clone of the repository, because it builds the old engine from commit `f127a67`. This
command reads a report, and it fails when a case is below 100 times the throughput or above 1% of the
allocation:

```shell
dotnet run -c Release --project src/GalaxyCheck.Benchmarks -- --gate report.json
```

The numbers change from machine to machine. Compare the ratios, not the absolute numbers.
