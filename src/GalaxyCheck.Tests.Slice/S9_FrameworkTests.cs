using System.IO.Compression;
using System.Text.RegularExpressions;
using FluentAssertions;
using GalaxyCheck.Runners;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>One framework package of slice S12 and the sample projects that run it.</summary>
public sealed record S12_Framework(string Name, string Prefix, string Trx, string Package, string RowAttribute)
{
    public string Folder(string kind) => Path.Combine(Repo.Samples, "S12", Name + kind);

    public string ProbePath(string probe) =>
        Path.Combine(Path.GetTempPath(), "galaxycheck-s12-probe", Prefix + "-" + probe + ".txt");

    public IReadOnlyList<string> ProbeLines(string probe) =>
        File.Exists(ProbePath(probe))
            ? File.ReadAllLines(ProbePath(probe)).Where(l => l.Length > 0).ToList()
            : new List<string>();

    public void ClearProbes()
    {
        foreach (var file in Directory.Exists(Path.GetDirectoryName(ProbePath("x")))
                     ? Directory.GetFiles(Path.GetDirectoryName(ProbePath("x"))!, Prefix + "-*.txt")
                     : Array.Empty<string>())
        {
            File.Delete(file);
        }
    }

    public static readonly S12_Framework NUnit =
        new("NUnit", "nunit", "nunit", "GalaxyCheck.NUnit", "TestCase");

    public static readonly S12_Framework MSTest =
        new("MSTest", "mstest", "mstest", "GalaxyCheck.MSTest", "DataRow");
}

public sealed record S12_Run(IReadOnlyList<TrxResult> Results, CliResult Run)
{
    public string Output => Run.Output;

    /// <summary>The results of the test method with this name (a data row test has one result for each row).</summary>
    public IReadOnlyList<TrxResult> Find(string method) =>
        Results.Where(r => Regex.IsMatch(r.TestName, $@"(^|[.\s]){Regex.Escape(method)}(\(|\s|$)")).ToList();

    public TrxResult Single(string method)
    {
        var found = Find(method);
        found.Should().HaveCount(1, $"the run must hold one result for {method}.\n{Output}");
        return found[0];
    }
}

public static class S12_Support
{
    private static readonly Dictionary<string, Lazy<S12_Run>> MainRuns = new()
    {
        ["nunit"] = new(() => RunMain(S12_Framework.NUnit)),
        ["mstest"] = new(() => RunMain(S12_Framework.MSTest)),
    };

    private static readonly Dictionary<string, Lazy<S12_Run>> ConfigRuns = new()
    {
        ["nunit"] = new(() => RunConfig(S12_Framework.NUnit)),
        ["mstest"] = new(() => RunConfig(S12_Framework.MSTest)),
    };

    private static readonly Dictionary<string, Lazy<(string Output, IReadOnlyList<string> Entries)>> Packs = new()
    {
        ["nunit"] = new(() => Pack(S12_Framework.NUnit)),
        ["mstest"] = new(() => Pack(S12_Framework.MSTest)),
    };

    private static S12_Run Run(S12_Framework fw, string folder)
    {
        var (results, run) = Trx.RunProject(folder, fw.Trx);
        return new S12_Run(results, run);
    }

    private static S12_Run RunMain(S12_Framework fw)
    {
        fw.ClearProbes();
        return Run(fw, fw.Folder("Main"));
    }

    private static S12_Run RunConfig(S12_Framework fw)
    {
        fw.ClearProbes();
        return Run(fw, fw.Folder("Config"));
    }

    private static (string Output, IReadOnlyList<string> Entries) Pack(S12_Framework fw)
    {
        var folder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", "pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var result = Cli.Dotnet($"pack \"{Path.Combine(Repo.Src, fw.Package)}\" -c Release -o \"{folder}\"");
        var entries = new List<string>();
        foreach (var file in Directory.GetFiles(folder, "*.nupkg").Where(f => !f.EndsWith(".symbols.nupkg")))
        {
            using var zip = ZipFile.OpenRead(file);
            entries.AddRange(zip.Entries.Select(e => e.FullName));
        }

        return (result.Output, entries);
    }

    /// <summary>Copies a sample project to a sibling folder, so that the original keeps its placeholders.</summary>
    private static string CopyWithReplacement(string source, string file, string placeholder, string value)
    {
        var target = source + ".run";
        if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        Directory.CreateDirectory(target);
        foreach (var path in Directory.GetFiles(source))
        {
            File.Copy(path, Path.Combine(target, Path.GetFileName(path)));
        }

        WriteReplacement(target, file, source, placeholder, value);
        return target;
    }

    private static void WriteReplacement(string target, string file, string source, string placeholder, string value) =>
        File.WriteAllText(
            Path.Combine(target, file),
            File.ReadAllText(Path.Combine(source, file)).Replace(placeholder, value));

    // R22: the package.

    public static void PackHasLibFolders(S12_Framework fw)
    {
        var (output, entries) = Packs[fw.Prefix].Value;
        entries.Should().Contain(e => e.StartsWith("lib/net10.0/"), $"the package must hold lib/net10.0.\n{output}");
        entries.Should().Contain(e => e.StartsWith("lib/net11.0/"), $"the package must hold lib/net11.0.\n{output}");
    }

    public static void PublicTypesExist(S12_Framework fw)
    {
        var run = MainRuns[fw.Prefix].Value;
        var result = run.Single("PublicTypes");
        result.Outcome.Should().Be("Passed", $"{result.Message}\n{run.Output}");
    }

    // R23: the property result.

    public static void PropertyThatHoldsPasses(S12_Framework fw)
    {
        var run = MainRuns[fw.Prefix].Value;
        var result = run.Single("IntHolds");
        result.Outcome.Should().Be("Passed", $"{result.Message}\n{run.Output}");
    }

    public static void FalsifiedPropertyFailsWithCounterexampleAndReplay(S12_Framework fw)
    {
        // The property of the sample, with the seed of its attribute, run through the library.
        var property = Property.ForAll(Gen.Int32(), x => x < 10);
        var check = property.Check(seed: 1234);
        check.Falsified.Should().BeTrue("the property 'the int is below 10' is falsified");
        var replay = check.Counterexample!.Replay;
        string expectedText;
        try
        {
            property.Assert(seed: 1234);
            expectedText = "";
        }
        catch (PropertyFailedException exception)
        {
            expectedText = exception.Message;
        }

        var run = MainRuns[fw.Prefix].Value;
        var result = run.Single("IntIsBelow10");
        result.Outcome.Should().Be("Failed", $"{result.Message}\n{run.Output}");
        var message = Normalize(result.Message);
        message.Should().Contain("10", $"the message must hold the counterexample.\n{run.Output}");
        message.Should().Contain(replay, $"the message must hold the replay string.\n{run.Output}");
        expectedText.Should().NotBeEmpty("the library must fail this property");
        message.Should().Contain(Normalize(expectedText), $"the message must have the text of GalaxyCheck.Xunit.\n{run.Output}");
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Trim();

    // R24: the property forms.

    public static void EachFormHoldsOrFails(S12_Framework fw)
    {
        var run = MainRuns[fw.Prefix].Value;
        var forms = new[] { "Void", "Bool", "Property", "Gen", "AsyncVoid", "AsyncBool", "AsyncProperty", "AsyncGen" };
        var problems = new List<string>();
        foreach (var form in forms)
        {
            foreach (var (suffix, expected) in new[] { ("Holds", "Passed"), ("Falsified", "Failed") })
            {
                var name = $"{form}_{suffix}";
                var found = run.Find(name);
                if (found.Count != 1)
                {
                    problems.Add($"{name}: {found.Count} results");
                }
                else if (found[0].Outcome != expected)
                {
                    problems.Add($"{name}: {found[0].Outcome}, expected {expected}. {found[0].Message}");
                }
            }
        }

        problems.Should().BeEmpty($"each form must give its result.\n{run.Output}");
    }

    // R25: replay and member generators.

    public static void ReplayRunsOneExample(S12_Framework fw)
    {
        var property = Property.ForAll(Gen.Int32(), x => x < 10);
        var check = property.Check(seed: 1234);
        check.Falsified.Should().BeTrue("the property 'the int is below 10' is falsified");
        var replay = check.Counterexample!.Replay;
        var counterexample = check.Counterexample.Value!.ToString();

        var folder = CopyWithReplacement(fw.Folder("Replay"), "ReplayTests.cs", "@@REPLAY@@", replay);
        fw.ClearProbes();
        var run = Run(fw, folder);
        var result = run.Single("Replayed");
        result.Outcome.Should().Be("Failed", $"{result.Message}\n{run.Output}");
        Normalize(result.Message).Should().Contain(counterexample!, $"the test must fail with the same counterexample.\n{run.Output}");
        fw.ProbeLines("replay").Should().Equal(new[] { counterexample }, $"the test function must run one time, with the counterexample.\n{run.Output}");
    }

    public static void MemberGenGivesFive(S12_Framework fw)
    {
        var run = MainRuns[fw.Prefix].Value;
        var result = run.Single("MemberGenIsFive");
        result.Outcome.Should().Be("Passed", $"{result.Message}\n{run.Output}");
        var lines = fw.ProbeLines("member");
        lines.Should().HaveCount(5, $"the test function runs once for each iteration.\n{run.Output}");
        lines.Should().OnlyContain(l => l == "5", $"the parameter must be 5 in each test case.\n{run.Output}");
    }

    // R26: sample.

    public static void SampleFailsAndListsValues(S12_Framework fw)
    {
        var run = MainRuns[fw.Prefix].Value;
        var result = run.Single("Sampled");
        result.Outcome.Should().Be("Failed", $"{result.Message}\n{run.Output}");
        result.Message.Should().Contain("7777", $"the message must list the sampled values.\n{run.Output}");
    }

    // R27: generator snapshot.

    public static void SnapshotFailsOnlyWhenValuesChange(S12_Framework fw)
    {
        var source = fw.Folder("Snapshot");
        var folder = CopyWithReplacement(source, "SnapshotTests.cs", "@@GEN@@", "Gen.Int32().Between(0, 100)");

        // The first run stores the snapshot. The second run has the same values.
        Run(fw, folder);
        var same = Run(fw, folder);
        var unchanged = same.Single("Snapshot");
        unchanged.Outcome.Should().Be("Passed", $"a stored snapshot with no change must pass.\n{unchanged.Message}\n{same.Output}");

        WriteReplacement(folder, "SnapshotTests.cs", source, "@@GEN@@", "Gen.Int32().Between(500, 600)");
        var changed = Run(fw, folder);
        var result = changed.Single("Snapshot");
        result.Outcome.Should().Be("Failed", $"a generator with other values must fail the snapshot.\n{changed.Output}");
    }

    // R28: data rows.

    public static void DataRowsRunOnePropertyForEachRow(S12_Framework fw)
    {
        var run = MainRuns[fw.Prefix].Value;
        var rows = run.Find("Rows");
        rows.Should().HaveCount(2, $"the result must hold one test for each row.\n{run.Output}");
        rows.Should().OnlyContain(r => r.Outcome == "Passed", $"each row must pass.\n{string.Join("\n", rows.Select(r => r.Message))}\n{run.Output}");
        var pairs = fw.ProbeLines("rows").Select(l => l.Split(',')).ToList();
        pairs.Should().NotBeEmpty($"the test function must run.\n{run.Output}");
        pairs.Select(p => p[0]).Distinct().Should().BeEquivalentTo(new[] { "1", "2" }, $"the first parameter is 1 or 2.\n{run.Output}");
        foreach (var row in new[] { "1", "2" })
        {
            pairs.Where(p => p[0] == row).Select(p => p[1]).Distinct().Count()
                .Should().BeGreaterThan(1, $"the second parameter is generated for row {row}.\n{run.Output}");
        }
    }

    // R29: global configuration.

    public static void GlobalConfigurationSetsIterations(S12_Framework fw)
    {
        var run = ConfigRuns[fw.Prefix].Value;
        var result = run.Single("DefaultIterations");
        result.Outcome.Should().Be("Passed", $"{result.Message}\n{run.Output}");
        fw.ProbeLines("config").Should().HaveCount(7, $"the test function must run 7 times.\n{run.Output}");
    }
}

public class S12_NUnitTests
{
    private static readonly S12_Framework Fw = S12_Framework.NUnit;

    [Fact]
    public void test_001_S9_R22_nunit_pack_has_net10_and_net11_lib_folders() => S12_Support.PackHasLibFolders(Fw);

    [Fact]
    public void test_001_S9_R23_nunit_property_that_holds_is_one_passed_test() => S12_Support.PropertyThatHoldsPasses(Fw);

    [Fact]
    public void test_001_S9_R23_nunit_falsified_property_fails_with_counterexample_and_replay() =>
        S12_Support.FalsifiedPropertyFailsWithCounterexampleAndReplay(Fw);

    [Fact]
    public void test_001_S9_R24_nunit_each_form_passes_when_it_holds_and_fails_when_falsified() => S12_Support.EachFormHoldsOrFails(Fw);

    [Fact]
    public void test_001_S9_R22_nunit_public_types_have_the_options_of_xunit() => S12_Support.PublicTypesExist(Fw);

    [Fact]
    public void test_001_S9_R25_nunit_replay_runs_one_time_and_fails_with_same_counterexample() => S12_Support.ReplayRunsOneExample(Fw);

    [Fact]
    public void test_001_S9_R25_nunit_member_gen_gives_five_in_each_test_case() => S12_Support.MemberGenGivesFive(Fw);

    [Fact]
    public void test_001_S9_R26_nunit_sample_fails_and_lists_sampled_values() => S12_Support.SampleFailsAndListsValues(Fw);

    [Fact]
    public void test_001_S9_R27_nunit_snapshot_fails_only_when_values_change() => S12_Support.SnapshotFailsOnlyWhenValuesChange(Fw);

    [Fact]
    public void test_001_S9_R28_nunit_data_rows_run_one_property_for_each_row() => S12_Support.DataRowsRunOnePropertyForEachRow(Fw);

    [Fact]
    public void test_001_S9_R29_nunit_global_configuration_sets_default_iterations() => S12_Support.GlobalConfigurationSetsIterations(Fw);
}

public class S12_MSTestTests
{
    private static readonly S12_Framework Fw = S12_Framework.MSTest;

    [Fact]
    public void test_001_S9_R22_mstest_pack_has_net10_and_net11_lib_folders() => S12_Support.PackHasLibFolders(Fw);

    [Fact]
    public void test_001_S9_R23_mstest_property_that_holds_is_one_passed_test() => S12_Support.PropertyThatHoldsPasses(Fw);

    [Fact]
    public void test_001_S9_R23_mstest_falsified_property_fails_with_counterexample_and_replay() =>
        S12_Support.FalsifiedPropertyFailsWithCounterexampleAndReplay(Fw);

    [Fact]
    public void test_001_S9_R24_mstest_each_form_passes_when_it_holds_and_fails_when_falsified() => S12_Support.EachFormHoldsOrFails(Fw);

    [Fact]
    public void test_001_S9_R22_mstest_public_types_have_the_options_of_xunit() => S12_Support.PublicTypesExist(Fw);

    [Fact]
    public void test_001_S9_R25_mstest_replay_runs_one_time_and_fails_with_same_counterexample() => S12_Support.ReplayRunsOneExample(Fw);

    [Fact]
    public void test_001_S9_R25_mstest_member_gen_gives_five_in_each_test_case() => S12_Support.MemberGenGivesFive(Fw);

    [Fact]
    public void test_001_S9_R26_mstest_sample_fails_and_lists_sampled_values() => S12_Support.SampleFailsAndListsValues(Fw);

    [Fact]
    public void test_001_S9_R27_mstest_snapshot_fails_only_when_values_change() => S12_Support.SnapshotFailsOnlyWhenValuesChange(Fw);

    [Fact]
    public void test_001_S9_R28_mstest_data_rows_run_one_property_for_each_row() => S12_Support.DataRowsRunOnePropertyForEachRow(Fw);

    [Fact]
    public void test_001_S9_R29_mstest_global_configuration_sets_default_iterations() => S12_Support.GlobalConfigurationSetsIterations(Fw);
}
