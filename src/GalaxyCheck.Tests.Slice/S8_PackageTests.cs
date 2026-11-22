using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S10_PackageTests
{
    private static readonly Lazy<(CliResult Run, string Folder)> Pack = new(() =>
    {
        var folder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", "s10-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var run = Cli.Dotnet($"pack \"{Path.Combine(Repo.Src, "GalaxyCheck.Xunit.V3")}\" -c Release -o \"{folder}\"");
        return (run, folder);
    });

    private static readonly Lazy<(IReadOnlyList<TrxResult> Results, CliResult Run, string Project)> V2Tests = new(() =>
    {
        var project = FindXunitV2TestProject();
        var folder = Path.GetDirectoryName(project)!;
        var (results, run) = Trx.RunProject(folder, "xunit2");
        return (results, run, project);
    });

    /// <summary>The test project of GalaxyCheck.Xunit: a test project outside this folder that references GalaxyCheck.Xunit.</summary>
    private static string FindXunitV2TestProject()
    {
        var candidates = Directory.EnumerateFiles(Repo.Src, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}GalaxyCheck.Tests.Slice{Path.DirectorySeparatorChar}"))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(p => Path.GetFileNameWithoutExtension(p).Contains("Test", StringComparison.Ordinal))
            .Where(p => !Regex.IsMatch(Path.GetFileNameWithoutExtension(p), "V3|TUnit|NUnit|MSTest|Benchmark|CodeAnalysis"))
            .Where(p => Regex.IsMatch(File.ReadAllText(p), @"GalaxyCheck\.Xunit\.csproj"))
            .OrderBy(p => p)
            .ToList();
        candidates.Should().NotBeEmpty("a test project of GalaxyCheck.Xunit must exist");
        return candidates.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Contains("Xunit", StringComparison.Ordinal)) ?? candidates[0];
    }

    [Fact]
    public void test_001_S8_R22_pack_holds_lib_folders_net10_and_net11()
    {
        var (run, folder) = Pack.Value;
        var packages = Directory.GetFiles(folder, "GalaxyCheck.Xunit.V3.*.nupkg");
        packages.Should().HaveCount(1, "dotnet pack must write one package. Output: " + run.Output);

        using var zip = ZipFile.OpenRead(packages[0]);
        var libFolders = zip.Entries
            .Select(e => e.FullName.Replace('\\', '/'))
            .Where(n => n.StartsWith("lib/", StringComparison.Ordinal))
            .Select(n => n.Split('/')[1])
            .Distinct()
            .ToList();
        libFolders.Should().Contain(new[] { "net10.0", "net11.0" }, "the package must hold both targets. Output: " + run.Output);
    }

    [Fact]
    public void test_001_S8_R22_package_public_types_have_the_options_of_xunit_v2()
    {
        // The xunit v3 assembly is checked by the sample test PublicTypes.TypesExist (run in S10_RunTests).
        // This test checks that the same list of options holds for GalaxyCheck.Xunit, the reference.
        var assembly = Assembly.Load("GalaxyCheck.Xunit");
        var problems = new List<string>();

        void Options(string type, params string[] names)
        {
            var t = assembly.GetType("GalaxyCheck." + type + "Attribute");
            if (t == null) { problems.Add("missing " + type + "Attribute"); return; }
            foreach (var name in names)
            {
                if (t.GetProperty(name) == null) problems.Add(type + "Attribute has no option " + name);
            }
        }

        Options("Property", "Iterations", "ShrinkLimit", "Seed", "Size", "TimeoutMilliseconds", "Exhaustive");
        Options("Sample");
        Options("GenSnapshot", "Iterations", "Size");
        Options("Replay");
        Options("MemberGen");
        problems.Should().BeEmpty();

        var run = S10_RunTests.Main.Value;
        var types = S10_RunTests.Find(run, "TypesExist");
        types.Should().ContainSingle("the xunit v3 sample must run the types test. Output: " + run.Cli.Output);
        types[0].Outcome.Should().Be("Passed", types[0].Message + " Output: " + run.Cli.Output);
    }

    [Fact]
    public void test_001_S8_R30_xunit_v2_test_project_has_74_passed_and_no_failed()
    {
        var (results, run, project) = V2Tests.Value;
        var passed = results.Count(r => r.Outcome == "Passed");
        var failed = results.Count(r => r.Outcome == "Failed");
        failed.Should().Be(0, $"the test project {project} must have no failed test. Output: {run.Output}");
        passed.Should().Be(74, $"the test project {project} must have 74 passed tests. Output: {run.Output}");
    }
}
