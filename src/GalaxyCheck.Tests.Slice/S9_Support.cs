using FluentAssertions;
using System.Text.Json;

namespace GalaxyCheck.Tests.Slice;

internal sealed record S13Phase(
    IReadOnlyList<TrxResult> Results,
    CliResult Run,
    Dictionary<string, string>? Failures,
    IReadOnlyDictionary<string, string[]> Logs)
{
    public string Output => Run.Output;

    public TrxResult One(string method)
    {
        var found = Results.Where(r => r.TestName.Contains(method)).ToList();
        found.Should().HaveCount(1, $"the project must report exactly one test result for {method}. Output:\n{Output}");
        return found[0];
    }

    public string? Key(string method) => Failures?.Keys.FirstOrDefault(k => k.Contains(method));

    public string[] Log(string method) => Logs.TryGetValue(method, out var lines) ? lines : [];
}

internal sealed record S13Memory(S13Phase First, S13Phase Second, S13Phase Third, S13Phase Fourth);

internal static class S13Support
{
    private static readonly Dictionary<string, Lazy<S13Memory>> MemoryRuns =
        new[] { "xunit2", "xunit3", "nunit", "mstest", "tunit" }.ToDictionary(fw => fw, fw => new Lazy<S13Memory>(() => RunMemory(fw)));

    private static readonly Dictionary<string, Lazy<S13Phase>> PlainRuns =
        new[] { "xunit2", "xunit3", "nunit", "mstest", "tunit" }.ToDictionary(fw => fw, fw => new Lazy<S13Phase>(() => RunPlain(fw)));

    public static S13Memory Memory(string fw) => MemoryRuns[fw].Value;

    public static S13Phase Plain(string fw) => PlainRuns[fw].Value;

    public static string Folder(string fw, string kind)
    {
        var name = fw switch { "xunit2" => "Xunit2", "xunit3" => "Xunit3", "nunit" => "NUnit", "mstest" => "MSTest", _ => "TUnit" };
        return Path.Combine(Repo.Samples, "S13", name + kind);
    }

    public static string FailureFile(string folder) => Path.Combine(folder, ".galaxycheck", "failures.json");

    private static void ClearLogs(string folder)
    {
        var logs = Path.Combine(folder, "logs");
        if (Directory.Exists(logs)) Directory.Delete(logs, recursive: true);
    }

    private static void Clean(string folder)
    {
        ClearLogs(folder);
        var memory = Path.Combine(folder, ".galaxycheck");
        if (Directory.Exists(memory)) Directory.Delete(memory, recursive: true);
        var hold = Path.Combine(folder, "hold.txt");
        if (File.Exists(hold)) File.Delete(hold);
    }

    private static Dictionary<string, string>? ReadFailures(string folder)
    {
        var path = FailureFile(folder);
        if (!File.Exists(path)) return null;
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");
    }

    private static IReadOnlyDictionary<string, string[]> ReadLogs(string folder)
    {
        var logs = Path.Combine(folder, "logs");
        if (!Directory.Exists(logs)) return new Dictionary<string, string[]>();
        return Directory.GetFiles(logs, "*.txt").ToDictionary(
            p => Path.GetFileNameWithoutExtension(p),
            p => File.ReadAllLines(p));
    }

    private static S13Phase Phase(string folder, string fw)
    {
        var (results, run) = Trx.RunProject(folder, fw);
        return new S13Phase(results, run, ReadFailures(folder), ReadLogs(folder));
    }

    private static void WriteReplayConstant(string folder, string value)
    {
        var literal = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        File.WriteAllText(
            Path.Combine(folder, "FixedReplay.cs"),
            "namespace S13Sample;\n\npublic static class FixedReplay\n{\n    public const string Value = \"" + literal + "\";\n}\n");
    }

    private static S13Memory RunMemory(string fw)
    {
        var folder = Folder(fw, "Memory");
        Clean(folder);
        WriteReplayConstant(folder, "placeholder");

        // Run 1: the failing tests save their replay strings.
        var first = Phase(folder, fw);
        var other = first.Failures?.FirstOrDefault(p => p.Key.Contains("MemOther")).Value;
        var switchable = first.Failures?.FirstOrDefault(p => p.Key.Contains("MemSwitchable")).Value;
        WriteReplayConstant(folder, other ?? "placeholder");

        // Run 2: the saved counterexample comes first.
        ClearLogs(folder);
        var second = Phase(folder, fw);

        // Run 3: the test function holds now.
        ClearLogs(folder);
        File.WriteAllText(Path.Combine(folder, "hold.txt"), "hold");
        var third = Phase(folder, fw);
        File.Delete(Path.Combine(folder, "hold.txt"));

        // Run 4: a saved replay (counterexample 10) and a [Replay] attribute (counterexample 50) for one test.
        ClearLogs(folder);
        var overrideKey = first.Key("MemSwitchable")?.Replace("MemSwitchable", "MemReplayOverride") ?? "S13Sample.MemoryTests.MemReplayOverride";
        Directory.CreateDirectory(Path.Combine(folder, ".galaxycheck"));
        File.WriteAllText(
            FailureFile(folder),
            JsonSerializer.Serialize(new Dictionary<string, string> { [overrideKey] = switchable ?? "missing" }));
        var fourth = Phase(folder, fw);
        return new S13Memory(first, second, third, fourth);
    }

    private static S13Phase RunPlain(string fw)
    {
        var folder = Folder(fw, "Plain");
        Clean(folder);
        return Phase(folder, fw);
    }
}
