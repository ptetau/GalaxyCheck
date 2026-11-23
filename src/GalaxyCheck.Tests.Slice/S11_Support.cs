using System.Text.Json;
using System.Xml.Linq;

namespace GalaxyCheck.Tests.Slice;

/// <summary>One test result of a trx file, with the error message and the output kept apart.</summary>
public record S11TrxResult(string TestName, string Outcome, string Message, string StdOut);

public static class S11Support
{
    public const string StartMarker = "<galaxycheck-failure>";
    public const string EndMarker = "</galaxycheck-failure>";

    /// <summary>Reads the JSON object between the two marker lines of a failure message.</summary>
    public static JsonDocument ExtractFailure(string message)
    {
        var lines = message.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).ToList();
        var start = lines.IndexOf(StartMarker);
        var end = lines.IndexOf(EndMarker);
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("The message holds no failure block. Message:\n" + message);
        }

        return JsonDocument.Parse(string.Join("\n", lines.Skip(start + 1).Take(end - start - 1)));
    }

    public static string LastLine(string message) =>
        message.Replace("\r\n", "\n").TrimEnd().Split('\n').Last().TrimEnd();

    /// <summary>Runs "dotnet test" on a sample project, with environment settings such as "GALAXYCHECK_AGENT=1".</summary>
    public static (IReadOnlyList<S11TrxResult> Results, CliResult Run) RunWithEnvironment(string projectFolder, string environment)
    {
        var memory = Path.Combine(projectFolder, ".galaxycheck");
        if (Directory.Exists(memory))
        {
            Directory.Delete(memory, recursive: true);
        }

        var resultsFolder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(resultsFolder);
        var run = Cli.Run(
            "env",
            $"{environment} dotnet test \"{projectFolder}\" --logger \"trx;LogFileName=results.trx\" --results-directory \"{resultsFolder}\"");
        var results = Directory.GetFiles(resultsFolder, "*.trx", SearchOption.AllDirectories)
            .SelectMany(path => XElement.Load(path).Descendants().Where(e => e.Name.LocalName == "UnitTestResult"))
            .Select(e => new S11TrxResult(
                (string?)e.Attribute("testName") ?? "",
                (string?)e.Attribute("outcome") ?? "",
                string.Join("\n", e.Descendants().Where(d => d.Name.LocalName == "Message").Select(d => d.Value)),
                string.Join("\n", e.Descendants().Where(d => d.Name.LocalName == "StdOut").Select(d => d.Value))))
            .ToList();
        return (results, run);
    }
}
