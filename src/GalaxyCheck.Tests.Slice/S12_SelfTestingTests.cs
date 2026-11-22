using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S12_SelfTestingTests
{
    private static readonly string[] OldNames = { "NebulaCheck", "GalaxyCheck.Stable" };

    private static string Project(string name) => Path.Combine(Repo.Src, name, name + ".csproj");

    private static IEnumerable<string> SolutionProjects()
    {
        var sln = File.ReadAllText(Path.Combine(Repo.Root, "GalaxyCheck.sln"));
        return Regex.Matches(sln, "\"([^\"]+\\.csproj)\"")
            .Select(m => Path.GetFullPath(Path.Combine(Repo.Root, m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar))))
            .Distinct()
            .ToList();
    }

    private static IEnumerable<XElement> PackageReferences(string projectFile) =>
        XElement.Load(projectFile).Descendants().Where(e => e.Name.LocalName == "PackageReference");

    private static string? Version(XElement reference) =>
        (string?)reference.Attribute("Version") ?? reference.Elements().FirstOrDefault(e => e.Name.LocalName == "Version")?.Value;

    private static string? RunnerVersion(string projectFile) =>
        PackageReferences(projectFile)
            .Where(r => string.Equals((string?)r.Attribute("Include") ?? (string?)r.Attribute("Update"), "xunit.runner.visualstudio", StringComparison.OrdinalIgnoreCase))
            .Select(Version)
            .FirstOrDefault();

    private static IEnumerable<string> SourceFiles(string projectName)
    {
        var folder = Path.Combine(Repo.Src, projectName);
        var skipped = new[] { Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar };
        return Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(f => !skipped.Any(s => f.Substring(folder.Length).Contains(s)));
    }

    private static readonly Dictionary<string, Lazy<(CliResult Run, IReadOnlyList<TrxResult> Results)>> Runs =
        new[] { "GalaxyCheck.Tests", "GalaxyCheck.Tests.V3" }.ToDictionary(
            name => name,
            name => new Lazy<(CliResult, IReadOnlyList<TrxResult>)>(() =>
            {
                var results = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", "s12-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(results);
                var folder = Path.Combine(Repo.Src, name);
                var run = Cli.Dotnet($"test \"{folder}\" -f net10.0 --logger \"trx;LogFileName=s12.trx\" --results-directory \"{results}\"");
                var trx = Directory.GetFiles(results, "*.trx", SearchOption.AllDirectories).SelectMany(Trx.Read).ToList();
                return (run, trx);
            }));

    [Fact]
    public void test_001_S12_R67_no_project_refers_to_an_old_package()
    {
        var projects = SolutionProjects().ToList();
        projects.Should().NotBeEmpty("GalaxyCheck.sln lists projects");
        foreach (var project in projects)
        {
            File.Exists(project).Should().BeTrue($"{project} is listed in GalaxyCheck.sln");
            var bad = PackageReferences(project)
                .Select(r => (string?)r.Attribute("Include") ?? (string?)r.Attribute("Update") ?? "")
                .Where(n => OldNames.Any(o => n.StartsWith(o, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            bad.Should().BeEmpty($"{project} must not reference an old package");
        }
    }

    [Theory]
    [InlineData("GalaxyCheck.Tests")]
    [InlineData("GalaxyCheck.Tests.V3")]
    public void test_001_S12_R67_no_test_file_uses_an_old_namespace(string projectName)
    {
        var files = SourceFiles(projectName).ToList();
        files.Should().NotBeEmpty();
        var offenders = files
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, line, n: i + 1)))
            .Where(x => OldNames.Any(o => x.line.Contains(o)))
            .Select(x => $"{x.f}:{x.n}: {x.line.Trim()}")
            .ToList();
        offenders.Should().BeEmpty();
    }

    [Theory]
    [InlineData("GalaxyCheck.Tests")]
    [InlineData("GalaxyCheck.Tests.V3")]
    public void test_001_S12_R67_project_references_xunit_and_runner_version_matches(string projectName)
    {
        var project = XElement.Load(Project(projectName));
        var references = project.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => ((string?)e.Attribute("Include") ?? "").Replace('\\', '/'))
            .ToList();
        references.Should().Contain(r => r.EndsWith("GalaxyCheck.Xunit/GalaxyCheck.Xunit.csproj"));

        var expected = RunnerVersion(Project("GalaxyCheck.Xunit.Tests"));
        expected.Should().NotBeNullOrEmpty("GalaxyCheck.Xunit.Tests has the runner package");
        RunnerVersion(Project(projectName)).Should().Be(expected);
    }

    [Fact]
    public void test_001_S12_R67_packages_local_holds_no_old_package()
    {
        var folder = Path.Combine(Repo.Root, "packages-local");
        var names = Directory.Exists(folder)
            ? Directory.GetFileSystemEntries(folder, "*", SearchOption.AllDirectories).Select(Path.GetFileName).ToList()
            : new List<string?>();
        names.Where(n => n!.StartsWith("nebulacheck", StringComparison.OrdinalIgnoreCase)
                      || n.StartsWith("galaxycheck.stable", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty();
    }

    [Fact]
    public void test_001_S12_R67_GalaxyCheck_Tests_passes_with_at_least_400_tests()
    {
        var (run, results) = Runs["GalaxyCheck.Tests"].Value;
        results.Should().NotBeEmpty($"the run wrote a trx file. Output:\n{run.Output}");
        results.Where(r => r.Outcome == "Failed").Select(r => r.TestName).Should().BeEmpty($"no test fails. Output:\n{run.Output}");
        results.Count(r => r.Outcome == "Passed").Should().BeGreaterThanOrEqualTo(400, $"Output:\n{run.Output}");
    }

    [Fact]
    public void test_001_S12_R67_GalaxyCheck_Tests_V3_passes_with_at_least_16_tests()
    {
        var (run, results) = Runs["GalaxyCheck.Tests.V3"].Value;
        results.Should().NotBeEmpty($"the run wrote a trx file. Output:\n{run.Output}");
        results.Where(r => r.Outcome == "Failed").Select(r => r.TestName).Should().BeEmpty($"no test fails. Output:\n{run.Output}");
        results.Count(r => r.Outcome == "Passed").Should().BeGreaterThanOrEqualTo(16, $"Output:\n{run.Output}");
    }
}
