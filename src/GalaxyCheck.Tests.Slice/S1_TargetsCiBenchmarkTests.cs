using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>Slice S1: packaging targets, the CI workflow, the package sources.</summary>
public class S1_PackagingTests
{
    private record PackRun(CliResult Cli, IReadOnlyList<string> LibFolders);

    private static readonly Dictionary<string, Lazy<PackRun>> Packs = new()
    {
        ["GalaxyCheck"] = new(() => Pack("GalaxyCheck")),
        ["GalaxyCheck.Xunit"] = new(() => Pack("GalaxyCheck.Xunit")),
    };

    private static PackRun Pack(string project)
    {
        var folder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice-s1-pack", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var result = Cli.Dotnet($"pack \"{Path.Combine(Repo.Src, project)}\" -c Release -o \"{folder}\"");
        var package = Directory.GetFiles(folder, project + ".*.nupkg")
            .FirstOrDefault(f => !f.EndsWith(".symbols.nupkg") && !f.EndsWith(".snupkg"));
        if (package == null)
        {
            return new PackRun(result, Array.Empty<string>());
        }

        using var zip = ZipFile.OpenRead(package);
        var libs = zip.Entries
            .Select(e => e.FullName.Replace('\\', '/'))
            .Where(n => n.StartsWith("lib/"))
            .Select(n => n.Split('/')[1])
            .Distinct()
            .ToList();
        return new PackRun(result, libs);
    }

    private static void AssertTargets(string project)
    {
        var run = Packs[project].Value;
        var because = $"the pack of {project} must write a nupkg. Output of dotnet pack:\n{run.Cli.Output}";
        run.Cli.ExitCode.Should().Be(0, because);
        run.LibFolders.Should().Contain("net10.0", because);
        run.LibFolders.Should().Contain("net11.0", because);
        run.LibFolders.Should().NotContain("net8.0", because);
    }

    [Fact]
    public void test_001_S1_R19_galaxycheck_package_has_net10_and_net11_and_no_net8() =>
        AssertTargets("GalaxyCheck");

    [Fact]
    public void test_001_S1_R19_galaxycheck_xunit_package_has_net10_and_net11_and_no_net8() =>
        AssertTargets("GalaxyCheck.Xunit");

    private static string Workflow()
    {
        var path = Path.Combine(Repo.Root, ".github", "workflows", "dotnet.yml");
        File.Exists(path).Should().BeTrue("the CI workflow must exist at " + path);
        return File.ReadAllText(path);
    }

    [Fact]
    public void test_001_S1_R61_each_uses_line_names_a_supported_major_version()
    {
        var uses = Regex.Matches(Workflow(), @"^\s*-?\s*uses:\s*(?<ref>\S+)", RegexOptions.Multiline)
            .Select(m => m.Groups["ref"].Value.Trim('"', '\''))
            .ToList();
        uses.Should().NotBeEmpty("the workflow must use at least one action");

        // GitHub no longer supports the Node 12 and Node 16 actions: the first-party actions need v4 or later.
        var minimum = new Dictionary<string, int>
        {
            ["actions/checkout"] = 4,
            ["actions/setup-dotnet"] = 4,
            ["actions/cache"] = 4,
            ["actions/upload-artifact"] = 4,
            ["actions/download-artifact"] = 4,
        };

        foreach (var reference in uses)
        {
            var match = Regex.Match(reference, @"^(?<name>[^@]+)@v(?<major>\d+)(\.\d+)*$");
            match.Success.Should().BeTrue($"'{reference}' must name a major version as @v<N>");
            var name = match.Groups["name"].Value.ToLowerInvariant();
            var major = int.Parse(match.Groups["major"].Value);
            if (minimum.TryGetValue(name, out var min))
            {
                major.Should().BeGreaterThanOrEqualTo(min, $"'{reference}' names a version that GitHub no longer supports");
            }
        }
    }

    [Fact]
    public void test_001_S1_R61_matrix_holds_the_three_operating_systems()
    {
        var text = Workflow();
        text.Should().Contain("ubuntu-latest");
        text.Should().Contain("windows-latest");
        text.Should().Contain("macos-latest");
        text.Should().Contain("matrix", "the operating systems must come from a matrix");
    }

    [Fact]
    public void test_001_S1_R19_test_step_runs_for_net10_and_net11()
    {
        var text = Workflow();
        Regex.IsMatch(text, @"dotnet\s+test").Should().BeTrue("the workflow must have a test step");
        text.Should().Contain("net10.0");
        text.Should().Contain("net11.0");
    }

    [Fact]
    public void test_001_S1_R19_each_test_project_of_the_solution_targets_net10_and_net11()
    {
        var solution = File.ReadAllText(Path.Combine(Repo.Root, "GalaxyCheck.sln"));
        var projects = Regex.Matches(solution, @"""([^""]+\.csproj)""")
            .Select(m => Path.GetFullPath(Path.Combine(Repo.Root, m.Groups[1].Value.Replace('\\', '/'))))
            .Where(p => !p.Contains("GalaxyCheck.Tests.Slice"))
            .ToList();
        var inherited = Path.Combine(Repo.Root, "Directory.Build.props");
        var testProjects = 0;
        foreach (var project in projects)
        {
            var document = XDocument.Load(project);
            var isTest = document.Descendants().Any(e =>
                (e.Name.LocalName == "PackageReference" && ((string?)e.Attribute("Include") ?? "") == "Microsoft.NET.Test.Sdk")
                || (e.Name.LocalName == "IsTestProject" && e.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)));
            if (!isTest)
            {
                continue;
            }

            testProjects++;
            var frameworks = document.Descendants()
                .Where(e => e.Name.LocalName is "TargetFramework" or "TargetFrameworks")
                .Select(e => e.Value).ToList();
            if (frameworks.Count == 0 && File.Exists(inherited))
            {
                frameworks = XDocument.Load(inherited).Descendants()
                    .Where(e => e.Name.LocalName is "TargetFramework" or "TargetFrameworks")
                    .Select(e => e.Value).ToList();
            }

            var all = string.Join(";", frameworks);
            all.Should().Contain("net10.0", $"the test project {Path.GetFileName(project)} must target net10.0");
            all.Should().Contain("net11.0", $"the test project {Path.GetFileName(project)} must target net11.0, or its net11.0 leg runs no test");
        }

        testProjects.Should().BeGreaterThan(0, "GalaxyCheck.sln must hold a test project");
    }

    [Fact]
    public void test_001_S1_R19_test_step_names_both_targets_in_a_matrix_or_a_framework_argument()
    {
        var text = Workflow();
        // The two targets must reach the test step: through a matrix value or a -f / --framework argument.
        var matrixHoldsBoth = Regex.IsMatch(text, @"matrix:(.|\n)*net10\.0(.|\n)*net11\.0")
            || Regex.IsMatch(text, @"matrix:(.|\n)*net11\.0(.|\n)*net10\.0");
        var testLines = text.Split('\n').Where(l => Regex.IsMatch(l, @"dotnet\s+test")).ToList();
        testLines.Should().NotBeEmpty();
        var argumentUsesFramework = testLines.Any(l => Regex.IsMatch(l, @"(\s-f\s|--framework)"));
        (matrixHoldsBoth && argumentUsesFramework || testLines.Count(l => l.Contains("net10.0") || l.Contains("net11.0")) >= 2)
            .Should().BeTrue("the test step must run once for each of net10.0 and net11.0");
    }

    [Fact]
    public void test_001_S1_R62_the_only_sources_are_nuget_org_and_one_repository_folder()
    {
        var path = Path.Combine(Repo.Root, "nuget.config");
        File.Exists(path).Should().BeTrue("nuget.config must exist at " + path);
        var raw = File.ReadAllText(path);
        raw.ToLowerInvariant().Should().NotContain("myget.org", "no source may hold myget.org");

        var sources = XDocument.Parse(raw)
            .Descendants("packageSources")
            .SelectMany(s => s.Elements())
            .Where(e => e.Name.LocalName == "add")
            .Select(e => ((string?)e.Attribute("value") ?? "").Trim())
            .ToList();

        var packageSources = XDocument.Parse(raw).Descendants("packageSources").Single().Elements().ToList();
        var clearIndex = packageSources.FindIndex(e => e.Name.LocalName == "clear");
        var firstAddIndex = packageSources.FindIndex(e => e.Name.LocalName == "add");
        clearIndex.Should().BeGreaterThanOrEqualTo(0, "packageSources must hold a <clear /> element, or the restore also reads the sources of the machine");
        clearIndex.Should().BeLessThan(firstAddIndex, "the <clear /> element must come before the add elements");

        sources.Should().HaveCount(2, "the sources are nuget.org and one folder");
        var remote = sources.Where(s => s.StartsWith("http://") || s.StartsWith("https://")).ToList();
        var local = sources.Except(remote).ToList();
        remote.Should().ContainSingle();
        new Uri(remote[0]).Host.Should().Be("api.nuget.org", "the remote source is nuget.org");
        local.Should().ContainSingle();
        local[0].Replace('\\', '/').Trim('.', '/').TrimEnd('/').Should().Be("packages-local");
        Path.GetFullPath(Path.Combine(Repo.Root, local[0])).Should().StartWith(Repo.Root, "the folder is in the repository");
    }
}

/// <summary>Slice S1: the benchmark command and its report.</summary>
public class S1_BenchmarkReportTests
{
    private static readonly string[] CaseNames =
    {
        "check int", "sample int", "sample string", "sample list of ints", "sample record", "shrink list of ints",
    };

    private record Bench(CliResult Cli, string ReportPath);

    private static readonly Lazy<Bench> Run = new(() =>
    {
        var folder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice-s1-bench", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var report = Path.Combine(folder, "report.json");
        var result = Cli.Dotnet(
            $"run -c Release --project \"{Path.Combine(Repo.Src, "GalaxyCheck.Benchmarks")}\" -- --report \"{report}\"",
            timeoutSeconds: 1800);
        return new Bench(result, report);
    });

    private static JsonElement Report()
    {
        var bench = Run.Value;
        var because = $"the benchmark command must write its report. Output of the command:\n{bench.Cli.Output}";
        bench.Cli.ExitCode.Should().Be(0, because);
        File.Exists(bench.ReportPath).Should().BeTrue(because);
        return JsonDocument.Parse(File.ReadAllText(bench.ReportPath)).RootElement.Clone();
    }

    private static JsonElement Case(JsonElement report, string name)
    {
        var because = $"the report must hold the case '{name}'. Output of the command:\n{Run.Value.Cli.Output}";
        report.TryGetProperty("cases", out var cases).Should().BeTrue(because);
        var match = cases.EnumerateArray()
            .Where(c => c.TryGetProperty("name", out var n) && n.GetString() == name)
            .ToList();
        match.Should().HaveCount(1, because);
        return match[0];
    }

    private static double Number(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var part in path)
        {
            current.TryGetProperty(part, out current).Should().BeTrue(
                $"the report must hold '{string.Join(".", path)}'. Output of the command:\n{Run.Value.Cli.Output}");
        }

        current.ValueKind.Should().Be(JsonValueKind.Number, string.Join(".", path) + " must be a number");
        return current.GetDouble();
    }

    [Fact]
    public void test_001_S1_R46_report_holds_the_six_cases()
    {
        var report = Report();
        var names = report.GetProperty("cases").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();
        names.Should().BeEquivalentTo(CaseNames, $"Output of the command:\n{Run.Value.Cli.Output}");
        names.Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData("check int")]
    [InlineData("sample int")]
    [InlineData("sample string")]
    [InlineData("sample list of ints")]
    [InlineData("sample record")]
    [InlineData("shrink list of ints")]
    public void test_001_S1_R46_case_holds_values_per_second_and_bytes_per_value_for_both_libraries(string name)
    {
        var c = Case(Report(), name);
        foreach (var side in new[] { "baseline", "current" })
        {
            Number(c, side, "valuesPerSecond").Should().BeGreaterThan(0, $"{name} {side} valuesPerSecond");
            Number(c, side, "bytesPerValue").Should().BeGreaterThanOrEqualTo(0, $"{name} {side} bytesPerValue");
        }
    }

    [Fact]
    public void test_001_S1_R47_report_names_the_baseline_commit()
    {
        var report = Report();
        report.TryGetProperty("baseline", out var baseline).Should().BeTrue($"Output of the command:\n{Run.Value.Cli.Output}");
        baseline.GetString().Should().Be("f127a67");
    }

    [Theory]
    [InlineData("check int")]
    [InlineData("sample int")]
    [InlineData("sample string")]
    [InlineData("sample list of ints")]
    [InlineData("sample record")]
    [InlineData("shrink list of ints")]
    public void test_001_S1_R47_case_holds_the_ratios_of_current_to_baseline(string name)
    {
        var c = Case(Report(), name);

        var throughput = Number(c, "throughputRatio");
        var expectedThroughput = Number(c, "current", "valuesPerSecond") / Number(c, "baseline", "valuesPerSecond");
        throughput.Should().BeApproximately(expectedThroughput, Math.Abs(expectedThroughput) * 0.01 + 1e-9,
            $"{name}: throughputRatio is current over baseline of valuesPerSecond");

        var allocation = Number(c, "allocationRatio");
        var baselineBytes = Number(c, "baseline", "bytesPerValue");
        baselineBytes.Should().BeGreaterThan(0, $"{name}: the baseline must allocate, so that the ratio is defined");
        var expectedAllocation = Number(c, "current", "bytesPerValue") / baselineBytes;
        allocation.Should().BeApproximately(expectedAllocation, Math.Abs(expectedAllocation) * 0.01 + 1e-9,
            $"{name}: allocationRatio is current over baseline of bytesPerValue");
    }
}
