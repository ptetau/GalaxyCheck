using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace GalaxyCheck.Tests.Slice;

/// <summary>The paths of the repository.</summary>
public static class Repo
{
    public static string Root { get; } = FindRoot();

    public static string Src => Path.Combine(Root, "src");

    public static string Samples => Path.Combine(Root, "src", "GalaxyCheck.Tests.Slice", "samples");

    private static string FindRoot([CallerFilePath] string path = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(path)!);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "GalaxyCheck.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found");
    }
}

/// <summary>The result of a command. A failure message of a test must hold <see cref="Output"/>.</summary>
public record CliResult(int ExitCode, string Output);

public static class Cli
{
    /// <summary>Runs a command and returns its exit code and its output (stdout and stderr).</summary>
    public static CliResult Run(string fileName, string arguments, string? workingDirectory = null, int timeoutSeconds = 900)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory ?? Repo.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        // A child "dotnet test" must not inherit the test host settings of this process.
        foreach (var key in startInfo.Environment.Keys.Where(k => k.StartsWith("VSTEST_") || k.StartsWith("MSBUILD")).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        var output = new StringBuilder();
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(timeoutSeconds * 1000))
        {
            process.Kill(entireProcessTree: true);
            return new CliResult(-1, output + $"\nTimed out after {timeoutSeconds} seconds");
        }

        process.WaitForExit();
        return new CliResult(process.ExitCode, output.ToString());
    }

    /// <summary>Runs "dotnet" with the arguments.</summary>
    public static CliResult Dotnet(string arguments, string? workingDirectory = null, int timeoutSeconds = 900) =>
        Run("dotnet", arguments, workingDirectory, timeoutSeconds);
}

/// <summary>One test result of a trx file.</summary>
public record TrxResult(string TestName, string Outcome, string Message);

public static class Trx
{
    /// <summary>Reads each UnitTestResult of a trx file.</summary>
    public static IReadOnlyList<TrxResult> Read(string path) =>
        XElement.Load(path)
            .Descendants()
            .Where(e => e.Name.LocalName == "UnitTestResult")
            .Select(e => new TrxResult(
                (string?)e.Attribute("testName") ?? "",
                (string?)e.Attribute("outcome") ?? "",
                string.Join("\n", e.Descendants().Where(d => d.Name.LocalName is "Message" or "StdOut").Select(d => d.Value))))
            .ToList();

    /// <summary>
    /// Runs a sample test project and returns its results and the output of the run.
    /// The framework is "xunit2", "xunit3", "nunit", "mstest", or "tunit".
    /// </summary>
    public static (IReadOnlyList<TrxResult> Results, CliResult Run) RunProject(string projectFolder, string framework)
    {
        var resultsFolder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(resultsFolder);
        var run = framework == "tunit"
            ? Cli.Dotnet($"run --project \"{projectFolder}\" -- --report-trx --results-directory \"{resultsFolder}\"")
            : Cli.Dotnet($"test \"{projectFolder}\" --logger \"trx;LogFileName=results.trx\" --results-directory \"{resultsFolder}\"");
        var results = Directory.GetFiles(resultsFolder, "*.trx", SearchOption.AllDirectories).SelectMany(Read).ToList();
        return (results, run);
    }
}
