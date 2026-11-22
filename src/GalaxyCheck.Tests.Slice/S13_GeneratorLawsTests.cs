using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using GalaxyCheck;
using GalaxyCheck.Gens;
using GalaxyCheck.Runners.Check;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>Shared helpers of the S13 reference suite.</summary>
internal static class S13
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static readonly int[] Seeds = Enumerable.Range(0, 20).ToArray();

    /// <summary>
    /// Gives an exact text for a value. Two values are the same when their texts are equal.
    /// NaN gives "NaN", so NaN equals NaN; -0 gives "-0", so it differs from 0.
    /// </summary>
    public static string Show(object? value)
    {
        switch (value)
        {
            case null: return "null";
            case string s: return "\"" + s + "\"";
            case char c: return "'" + c + "'";
            case bool b: return b ? "true" : "false";
            case double d: return d.ToString("R", Inv);
            case float f: return f.ToString("R", Inv);
            case Half h: return h.ToString("R", Inv);
            case decimal m: return m.ToString(Inv);
            case Complex c: return "(" + c.Real.ToString("R", Inv) + "," + c.Imaginary.ToString("R", Inv) + ")";
            case Rune r: return "U+" + r.Value.ToString("X", Inv);
            case DateTimeOffset o: return o.ToString("O", Inv);
            case DateOnly d: return d.ToString("O", Inv);
            case TimeOnly t: return t.ToString("O", Inv);
            case TimeSpan t: return t.ToString("c", Inv);
            case Uri u: return u.OriginalString;
            case Version v: return v.ToString();
            case ITuple t:
                return "(" + string.Join(", ", Enumerable.Range(0, t.Length).Select(i => Show(t[i]))) + ")";
            case IFormattable f: return f.ToString(null, Inv);
        }

        var type = value.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            return "[" + Show(type.GetProperty("Key")!.GetValue(value)) + ": " + Show(type.GetProperty("Value")!.GetValue(value)) + "]";
        }

        if (value is IEnumerable e)
        {
            return "[" + string.Join(", ", e.Cast<object?>().Select(Show)) + "]";
        }

        return value.ToString() ?? "";
    }

    public static List<string> ShowAll<T>(IEnumerable<T> values) => values.Select(v => Show(v)).ToList();

    /// <summary>Runs the function on the thread pool and throws a TimeoutException when it runs over the limit.</summary>
    public static T Within<T>(TimeSpan limit, Func<T> function)
    {
        var task = Task.Run(function);
        if (Task.WhenAny(task, Task.Delay(limit)).GetAwaiter().GetResult() != task)
        {
            throw new TimeoutException($"the run took more than {limit.TotalSeconds} seconds");
        }

        return task.GetAwaiter().GetResult();
    }

    /// <summary>Removes the consecutive repeats of a sequence, for example a re-run of the same failing value.</summary>
    public static List<string> Collapse(IEnumerable<string> values)
    {
        var result = new List<string>();
        foreach (var value in values)
        {
            if (result.Count == 0 || result[^1] != value)
            {
                result.Add(value);
            }
        }

        return result;
    }

    public static bool IsEven<T>(T x) where T : IBinaryInteger<T> => T.IsEvenInteger(x);

    public static bool CharSumEven(string s) => s.Sum(c => (int)c) % 2 == 0;

    public static string Problems(List<string> problems) =>
        problems.Count + " problem(s):\n" + string.Join("\n", problems.Take(40));
}

/// <summary>Records each call of a test function: the value and whether the property held.</summary>
internal sealed class S13Recorder<T>
{
    private readonly Func<T, bool> holds;
    private readonly List<(string Shown, bool Held)> calls = new();

    public S13Recorder(Func<T, bool> holds) => this.holds = holds;

    public bool Invoke(T value)
    {
        var shown = S13.Show(value);
        var held = holds(value);
        lock (calls)
        {
            calls.Add((shown, held));
        }

        return held;
    }

    public IReadOnlyList<(string Shown, bool Held)> Calls
    {
        get { lock (calls) return calls.ToList(); }
    }

    public List<string> Failing => Calls.Where(c => !c.Held).Select(c => c.Shown).ToList();
}

public class S13_GeneratorLawsTests
{
    private static readonly DateTimeOffset Y2K = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static IGen<string> Tree() =>
        Gen.Recursive<string>(
            Gen.Int32().Between(0, 9).Select(i => i.ToString(CultureInfo.InvariantCulture)),
            g => g.ListOf().WithCountBetween(1, 3).Select(xs => "(" + string.Join(" ", xs) + ")"),
            4);

    /// <summary>Each row: a public generator, and a property that fails for some but not all of its values.</summary>
    private static readonly Dictionary<string, Func<int, List<string>>> Cases = new()
    {
        ["Int32"] = s => Laws(Gen.Int32(), S13.IsEven, s),
        ["Byte"] = s => Laws(Gen.Byte(), S13.IsEven, s),
        ["Int16"] = s => Laws(Gen.Int16(), S13.IsEven, s),
        ["Int64"] = s => Laws(Gen.Int64(), S13.IsEven, s),
        ["SByte"] = s => Laws(Gen.SByte(), S13.IsEven, s),
        ["UInt16"] = s => Laws(Gen.UInt16(), S13.IsEven, s),
        ["UInt32"] = s => Laws(Gen.UInt32(), S13.IsEven, s),
        ["UInt64"] = s => Laws(Gen.UInt64(), S13.IsEven, s),
        ["IntPtr"] = s => Laws(Gen.IntPtr(), S13.IsEven, s),
        ["UIntPtr"] = s => Laws(Gen.UIntPtr(), S13.IsEven, s),
        ["Int128"] = s => Laws(Gen.Int128(), S13.IsEven, s),
        ["UInt128"] = s => Laws(Gen.UInt128(), S13.IsEven, s),
        ["BigInteger"] = s => Laws(Gen.BigInteger(), S13.IsEven, s),
        ["Int32 between"] = s => Laws(Gen.Int32().Between(-500, 500), S13.IsEven, s),
        ["Half"] = s => Laws(Gen.Half(), x => !(x > (Half)1.5f), s),
        ["Single"] = s => Laws(Gen.Single(), x => !(x > 1.5f), s),
        ["Double"] = s => Laws(Gen.Double(), x => !(x > 1.5), s),
        ["Double without special values"] = s => Laws(Gen.Double().WithoutSpecialValues(), x => x <= 1.5, s),
        ["Decimal"] = s => Laws(Gen.Decimal(), x => x <= 1.5m, s),
        ["Complex"] = s => Laws(Gen.Complex(), c => !(c.Real > 1.5), s),
        ["TimeSpan"] = s => Laws(Gen.TimeSpan(), x => x <= TimeSpan.Zero, s),
        ["DateTimeOffset"] = s => Laws(Gen.DateTimeOffset(), x => x <= Y2K, s),
        ["DateOnly"] = s => Laws(Gen.DateOnly(), x => x <= new DateOnly(2000, 1, 1), s),
        ["TimeOnly"] = s => Laws(Gen.TimeOnly(), x => x.Hour < 12, s),
        ["Rune"] = s => Laws(Gen.Rune(), r => r.Value % 2 == 0, s),
        ["Boolean"] = s => Laws(Gen.Boolean(), b => b, s),
        ["String"] = s => Laws(Gen.String(), S13.CharSumEven, s),
        ["StringMatching"] = s => Laws(Gen.StringMatching("[a-z]{0,12}"), S13.CharSumEven, s),
        ["Uri"] = s => Laws(Gen.Uri(), u => S13.CharSumEven(u.OriginalString), s),
        ["Version"] = s => Laws(Gen.Version(), v => S13.CharSumEven(v.ToString()), s),
        ["Email"] = s => Laws(Gen.Email(), S13.CharSumEven, s),
        ["Realistic.FirstName"] = s => Laws(Gen.Realistic.FirstName(), S13.CharSumEven, s),
        ["Realistic.LastName"] = s => Laws(Gen.Realistic.LastName(), S13.CharSumEven, s),
        ["Realistic.FullName"] = s => Laws(Gen.Realistic.FullName(), S13.CharSumEven, s),
        ["Realistic.StreetAddress"] = s => Laws(Gen.Realistic.StreetAddress(), S13.CharSumEven, s),
        ["Realistic.City"] = s => Laws(Gen.Realistic.City(), S13.CharSumEven, s),
        ["Realistic.Country"] = s => Laws(Gen.Realistic.Country(), S13.CharSumEven, s),
        ["Realistic.PhoneNumber"] = s => Laws(Gen.Realistic.PhoneNumber(), S13.CharSumEven, s),
        ["Realistic.CompanyName"] = s => Laws(Gen.Realistic.CompanyName(), S13.CharSumEven, s),
        ["List"] = s => Laws(Gen.Int32().ListOf(), xs => xs.Count % 2 == 0, s),
        ["Dictionary"] = s => Laws(Gen.Dictionary(Gen.Int32(), Gen.Boolean()), d => d.Count % 2 == 0, s),
        ["KeyValuePair"] = s => Laws(Gen.KeyValuePair(Gen.Int32(), Gen.Boolean()), kv => kv.Value || S13.IsEven(kv.Key), s),
        ["ValueTuple 1"] = s => Laws(Gen.ValueTuple(Gen.Int32()), t => S13.IsEven(t.Item1), s),
        ["ValueTuple 2"] = s => Laws(Gen.ValueTuple(Gen.Int32(), Gen.Boolean()), t => t.Item2 || S13.IsEven(t.Item1), s),
        ["ValueTuple 7"] = s => Laws(
            Gen.ValueTuple(Gen.Int32(), Gen.Int32(), Gen.Int32(), Gen.Int32(), Gen.Int32(), Gen.Int32(), Gen.Int32()),
            t => S13.IsEven(unchecked(t.Item1 + t.Item2 + t.Item3 + t.Item4 + t.Item5 + t.Item6 + t.Item7)), s),
        ["Tuple 1"] = s => Laws(Gen.Tuple(Gen.Int32()), t => S13.IsEven(t.Item1), s),
        ["Tuple 2"] = s => Laws(Gen.Tuple(Gen.Int32(), Gen.Boolean()), t => t.Item2 || S13.IsEven(t.Item1), s),
        ["Tuple 7"] = s => Laws(
            Gen.Tuple(Gen.Int32(), Gen.Int32(), Gen.Int32(), Gen.Int32(), Gen.Int32(), Gen.Int32(), Gen.Int32()),
            t => S13.IsEven(unchecked(t.Item1 + t.Item2 + t.Item3 + t.Item4 + t.Item5 + t.Item6 + t.Item7)), s),
        ["Recursive"] = s => Laws(Tree(), t => !t.StartsWith('('), s),
    };

    public static IEnumerable<object[]> CaseNames() => Cases.Keys.Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void test_001_S13_R68_generator_laws_hold_on_seeds_0_to_19(string generator)
    {
        var problems = new List<string>();
        foreach (var seed in S13.Seeds)
        {
            try
            {
                problems.AddRange(Cases[generator](seed).Select(p => $"{generator}, seed {seed}: {p}"));
            }
            catch (Exception e)
            {
                problems.Add($"{generator}, seed {seed}: {e.GetType().Name}: {e.Message}");
            }
        }

        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    /// <summary>Checks the five laws of R68 for one generator and one seed.</summary>
    private static List<string> Laws<T>(IGen<T> gen, Func<T, bool> holds, int seed)
    {
        var problems = new List<string>();
        var limit = TimeSpan.FromSeconds(30);

        // Law 1: the same seed and size give the same values.
        var first = S13.ShowAll(gen.Sample(iterations: 20, seed: seed, size: 50));
        var second = S13.ShowAll(gen.Sample(iterations: 20, seed: seed, size: 50));
        if (!first.SequenceEqual(second))
        {
            problems.Add($"the same seed and size gave [{string.Join(", ", first)}] and then [{string.Join(", ", second)}]");
        }

        // Law 2: a failing property ends its shrinking.
        var recorder = new S13Recorder<T>(holds);
        var property = Property.ForAll(gen, (Func<T, bool>)recorder.Invoke);
        CheckResult<T> result;
        try
        {
            result = S13.Within(limit, () => property.Check(iterations: 100, seed: seed));
        }
        catch (TimeoutException e)
        {
            problems.Add("shrinking did not end: " + e.Message);
            return problems;
        }

        if (!result.Falsified || result.Counterexample is null)
        {
            problems.Add("the property did not fail in 100 iterations, so the laws could not be checked");
            return problems;
        }

        var shrunk = S13.Show(result.Counterexample.Value);

        // Law 3: no value appears twice in the sequence of accepted shrinks.
        var accepted = S13.Collapse(recorder.Failing);
        var repeated = accepted.GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (repeated.Count > 0)
        {
            problems.Add($"an accepted shrink repeats: {string.Join(", ", repeated)} in [{string.Join(" -> ", accepted)}]");
        }

        if (accepted.Count == 0 || accepted[^1] != shrunk)
        {
            problems.Add($"the last failing input {(accepted.Count == 0 ? "(none)" : accepted[^1])} is not the counterexample {shrunk}");
        }

        // Law 4: the shrunk value still fails.
        if (holds(result.Counterexample.Value))
        {
            problems.Add($"the shrunk value {shrunk} does not fail");
        }

        // Law 5: a run from the replay of the shrunk value makes no further shrink.
        var replayRecorder = new S13Recorder<T>(holds);
        var replayProperty = Property.ForAll(gen, (Func<T, bool>)replayRecorder.Invoke);
        CheckResult<T> replayed;
        try
        {
            replayed = S13.Within(limit, () => replayProperty.Check(replay: result.Counterexample.Replay));
        }
        catch (TimeoutException e)
        {
            problems.Add("the replay did not end: " + e.Message);
            return problems;
        }

        if (!replayed.Falsified || replayed.Counterexample is null)
        {
            problems.Add("the replay of the shrunk value did not fail");
        }
        else
        {
            if (replayed.Shrinks != 0)
            {
                problems.Add($"the replay made {replayed.Shrinks} more shrink(s)");
            }

            var replayedValue = S13.Show(replayed.Counterexample.Value);
            if (replayedValue != shrunk)
            {
                problems.Add($"the replay gave {replayedValue}, not {shrunk}");
            }
        }

        return problems;
    }
}

public class S13_ReferenceDocTests
{
    private static readonly Regex Source = new(
        @"https?://|hypothesis|quickcheck|jqwik|fast-check|hedgehog|cscheck|fscheck|shrinking.challenge|how to specify it|smartcheck|proptest|test\.check|scalacheck|propcheck|americium|elm-test",
        RegexOptions.IgnoreCase);

    [Fact]
    public void test_001_S13_R68_reference_doc_names_a_source_for_each_requirement()
    {
        var path = Path.Combine(Repo.Root, "docs", "reference-suite.md");
        Assert.True(File.Exists(path), $"the file {path} must exist");
        var lines = File.ReadAllLines(path);

        // A section is a heading line and the lines up to the next heading.
        var sections = new List<List<string>>();
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith('#') || sections.Count == 0)
            {
                sections.Add(new List<string>());
            }

            sections[^1].Add(line);
        }

        var missing = new List<string>();
        for (var n = 68; n <= 77; n++)
        {
            var id = new Regex($@"\bR{n}\b");
            var onLine = lines.Any(l => id.IsMatch(l) && Source.IsMatch(l));
            var inSection = sections.Any(s => id.IsMatch(s[0]) && s.Any(l => Source.IsMatch(l)));
            if (!onLine && !inSection)
            {
                missing.Add($"R{n}");
            }
        }

        Assert.True(missing.Count == 0, $"docs/reference-suite.md must name a source (a library, a paper, or a URL) on the line or under the heading of each requirement. Missing: {string.Join(", ", missing)}");
    }
}
