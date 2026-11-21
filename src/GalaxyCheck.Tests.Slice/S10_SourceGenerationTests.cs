using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>Slice S14: the source generator (R57, R58) and the reference pages (R60).</summary>
public class S14_SourceGenerationTests
{
    private static string Sample(string name) => Path.Combine(Repo.Samples, "S14", name);

    private static readonly Lazy<(CliResult Publish, CliResult Run)> TrimmedRun = new(() => PublishAndRun("trim"));

    private static readonly Lazy<(CliResult Publish, CliResult Run)> AotRun = new(() => PublishAndRun("aot"));

    private static readonly Lazy<CliResult> GeneratedParityRun = new(() => Cli.Dotnet($"run --project \"{Sample("ParityGen")}\" -c Release"));

    private static readonly Lazy<CliResult> ReflectionParityRun = new(() => Cli.Dotnet($"run --project \"{Sample("ParityReflection")}\" -c Release"));

    private static readonly Lazy<CliResult> UnsupportedBuild = new(() =>
        Cli.Dotnet($"build \"{Sample("Unsupported")}\" --no-incremental -p:TreatWarningsAsErrors=false"));

    private static (CliResult Publish, CliResult Run) PublishAndRun(string mode)
    {
        var output = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", "s14-" + mode + "-" + Guid.NewGuid().ToString("N"));
        var modeArguments = mode == "aot"
            ? "-p:PublishAot=true"
            : "-p:PublishTrimmed=true -p:SelfContained=true";
        var publish = Cli.Dotnet(
            $"publish \"{Sample("AotApp")}\" -r linux-x64 {modeArguments} -p:TreatWarningsAsErrors=false -o \"{output}\"");
        var executable = Path.Combine(output, "AotApp");
        var run = File.Exists(executable)
            ? Cli.Run(executable, "", output, timeoutSeconds: 120)
            : new CliResult(-1, $"The published program {executable} does not exist");
        return (publish, run);
    }

    private static IReadOnlyList<string> GalaxyCheckIlWarnings(string output) =>
        output.Split('\n')
            .Where(line => line.Contains("warning IL") && line.Contains("GalaxyCheck"))
            .ToList();

    private static void AssertPublishedAndPassed((CliResult Publish, CliResult Run) result)
    {
        result.Publish.ExitCode.Should().Be(0, $"the publish must succeed. Output:\n{result.Publish.Output}");
        GalaxyCheckIlWarnings(result.Publish.Output).Should()
            .BeEmpty($"no trim warning and no AOT warning may come from a GalaxyCheck package. Output:\n{result.Publish.Output}");
        result.Run.ExitCode.Should().Be(0, $"the published program must run its property with a pass. Output:\n{result.Run.Output}");
        result.Run.Output.Should().Contain("PASS", $"the published program must print PASS. Output:\n{result.Run.Output}");
    }

    // R57

    [Fact]
    public void test_001_S10_R57_trimmed_publish_has_no_galaxycheck_warning_and_the_property_passes() =>
        AssertPublishedAndPassed(TrimmedRun.Value);

    [Fact]
    public void test_001_S10_R57_nativeaot_publish_has_no_galaxycheck_warning_and_the_property_passes() =>
        AssertPublishedAndPassed(AotRun.Value);

    // R58

    [Fact]
    public void test_001_S10_R58_generated_code_and_reflection_path_sample_the_same_100_values_for_seed_0()
    {
        var generated = GeneratedParityRun.Value;
        var reflection = ReflectionParityRun.Value;

        generated.ExitCode.Should().Be(0, $"the generated-code sample must run. Output:\n{generated.Output}");
        reflection.ExitCode.Should().Be(0, $"the reflection sample must run. Output:\n{reflection.Output}");

        var generatedValues = ValueLines(generated.Output);
        var reflectionValues = ValueLines(reflection.Output);

        generatedValues.Should().HaveCount(100, $"the sample draws 100 values. Output:\n{generated.Output}");
        reflectionValues.Should().HaveCount(100, $"the sample draws 100 values. Output:\n{reflection.Output}");
        generatedValues.Should().Equal(
            reflectionValues,
            $"the two paths must give the same values for seed 0.\nGenerated:\n{generated.Output}\nReflection:\n{reflection.Output}");
    }

    private static List<string> ValueLines(string output) =>
        output.Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => Regex.IsMatch(line, @"^-?\d+\|\d+\|""")) // Id|Name length|Name as JSON
            .ToList();

    [Fact]
    public void test_001_S10_R58_unsupported_type_gives_a_GCSG001_diagnostic_that_names_the_type()
    {
        var build = UnsupportedBuild.Value;

        var diagnosticLines = build.Output.Split('\n')
            .Where(line => line.Contains("GCSG001") && line.Contains("Unsupported"))
            .ToList();

        diagnosticLines.Should().NotBeEmpty(
            $"the compiler must report GCSG001 with the name of the type 'Unsupported'. Exit code {build.ExitCode}. Output:\n{build.Output}");
    }

    // R60

    /// <summary>The names of the public generator methods of Gen and of Gen.Realistic, as docs names them.</summary>
    private static List<string> PublicGeneratorMethods()
    {
        static IEnumerable<string> Names(Type type, string prefix) =>
            type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && IsGenerator(m.ReturnType))
                .Select(m => prefix + m.Name);

        var genType = typeof(Gen);
        var realistic = genType.GetNestedType("Realistic", BindingFlags.Public | BindingFlags.NonPublic);
        var names = Names(genType, "");
        if (realistic != null && realistic.IsVisible)
        {
            names = names.Concat(Names(realistic, "Realistic."));
        }

        return names.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();
    }

    private static bool IsGenerator(Type returnType) =>
        IsGenInterface(returnType) || returnType.GetInterfaces().Any(IsGenInterface);

    private static bool IsGenInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("IGen`");

    /// <summary>Returns the names of the methods that have no entry with exactly one csharp code block.</summary>
    private static List<string> MissingEntries(IEnumerable<string> methodNames, string docs)
    {
        var sections = Regex.Split(docs.Replace("\r\n", "\n"), @"(?m)^(?=### )");
        var missing = new List<string>();
        foreach (var name in methodNames)
        {
            var headings = name.StartsWith("Realistic.")
                ? new[] { "### Gen." + name, "### Gen." + name.Substring("Realistic.".Length) }
                : new[] { "### Gen." + name };
            var section = sections.FirstOrDefault(s =>
            {
                var firstLine = s.Split('\n')[0].Trim();
                return headings.Any(h => firstLine == h || firstLine.StartsWith(h + "(") || firstLine.StartsWith(h + "<"));
            });
            var blocks = section == null ? 0 : Regex.Matches(section, @"(?m)^```csharp\s*$").Count;
            if (blocks != 1)
            {
                missing.Add(name);
            }
        }

        return missing;
    }

    private static string DocsText()
    {
        var path = Path.Combine(Repo.Root, "docs", "generators.md");
        File.Exists(path).Should().BeTrue($"the reference page {path} must exist");
        return File.ReadAllText(path);
    }

    [Fact]
    public void test_001_S10_R60_each_public_generator_method_has_a_reference_entry_with_one_csharp_example()
    {
        var methods = PublicGeneratorMethods();
        methods.Should().Contain(["UInt64", "StringMatching", "Dictionary", "Rune"], "Gen must offer the spec generators");

        var missing = MissingEntries(methods, DocsText());

        missing.Should().BeEmpty(
            "each public generator method needs a '### Gen.<Name>' heading with one csharp code block in docs/generators.md. Missing: "
            + string.Join(", ", missing));
    }

    [Fact]
    public void test_001_S10_R60_a_method_with_no_entry_fails_the_check_and_its_message_names_the_method()
    {
        const string method = "UInt64";
        var docs = DocsText();
        var methods = PublicGeneratorMethods();
        methods.Should().Contain(method);
        MissingEntries([method], docs).Should().BeEmpty($"docs/generators.md must hold an entry for Gen.{method}");

        // Remove the entry of one method from the real page.
        var sections = Regex.Split(docs.Replace("\r\n", "\n"), @"(?m)^(?=### )");
        var withoutEntry = string.Concat(sections.Where(s => !s.Split('\n')[0].Trim().StartsWith($"### Gen.{method}")));

        var missing = MissingEntries(methods, withoutEntry);

        missing.Should().Contain(method, "the check must fail for a method with no entry");
        var message = "docs/generators.md has no entry for: " + string.Join(", ", missing);
        message.Should().Contain(method, "the failure message must name the method");
    }
}
