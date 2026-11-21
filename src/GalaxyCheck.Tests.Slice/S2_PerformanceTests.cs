using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S2_PerformanceTests
{
    private record Run(CliResult Cli, JsonDocument? Report);

    // The benchmark command is slow. It runs one time for this class.
    private static readonly Lazy<Run> Benchmark = new(() =>
    {
        var folder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "report.json");
        var cli = Cli.Dotnet($"run -c Release --project \"{Path.Combine(Repo.Src, "GalaxyCheck.Benchmarks")}\" -- --report \"{path}\"");
        JsonDocument? report = null;
        if (File.Exists(path))
        {
            try { report = JsonDocument.Parse(File.ReadAllText(path)); } catch (JsonException) { }
        }

        return new Run(cli, report);
    });

    private static double Ratio(string caseName, string property)
    {
        var run = Benchmark.Value;
        var because = $"the benchmark command must write a report. Output:\n{run.Cli.Output}";
        run.Report.Should().NotBeNull(because);
        var found = run.Report!.RootElement.GetProperty("cases").EnumerateArray()
            .Where(c => c.GetProperty("name").GetString() == caseName).ToList();
        found.Should().HaveCount(1, $"the report must hold the case \"{caseName}\". Output:\n{run.Cli.Output}");
        return found[0].GetProperty(property).GetDouble();
    }

    private static void Throughput(string caseName) =>
        Ratio(caseName, "throughputRatio").Should().BeGreaterThanOrEqualTo(100, $"the case \"{caseName}\" needs a throughput ratio of 100. Output:\n{Benchmark.Value.Cli.Output}");

    private static void Allocation(string caseName) =>
        Ratio(caseName, "allocationRatio").Should().BeLessThanOrEqualTo(0.01, $"the case \"{caseName}\" needs an allocation ratio of 0.01. Output:\n{Benchmark.Value.Cli.Output}");

    [Fact]
    public void test_001_S2_R48_check_int_throughput_ratio_is_at_least_100() => Throughput("check int");

    [Fact]
    public void test_001_S2_R48_sample_int_throughput_ratio_is_at_least_100() => Throughput("sample int");

    [Fact]
    public void test_001_S2_R49_check_int_allocation_ratio_is_at_most_0_01() => Allocation("check int");

    [Fact]
    public void test_001_S2_R49_sample_int_allocation_ratio_is_at_most_0_01() => Allocation("sample int");

    [Fact]
    public void test_001_S2_R48_sample_string_throughput_ratio_is_at_least_100() => Throughput("sample string");

    [Fact]
    public void test_001_S2_R49_sample_string_allocation_ratio_is_at_most_0_01() => Allocation("sample string");

    [Fact]
    public void test_001_S2_R48_sample_list_of_ints_throughput_ratio_is_at_least_100() => Throughput("sample list of ints");

    [Fact]
    public void test_001_S2_R49_sample_list_of_ints_allocation_ratio_is_at_most_0_01() => Allocation("sample list of ints");

    [Fact]
    public void test_001_S2_R48_sample_record_throughput_ratio_is_at_least_100() => Throughput("sample record");

    [Fact]
    public void test_001_S2_R49_sample_record_allocation_ratio_is_at_most_0_01() => Allocation("sample record");

    [Fact]
    public void test_001_S2_R48_shrink_list_of_ints_throughput_ratio_is_at_least_100() => Throughput("shrink list of ints");

    [Fact]
    public void test_001_S2_R49_shrink_list_of_ints_allocation_ratio_is_at_most_0_01() => Allocation("shrink list of ints");

    // ---- The gate (R50) ----

    private static readonly string[] CaseNames =
        ["check int", "sample int", "sample string", "sample list of ints", "sample record", "shrink list of ints"];

    private static CliResult RunGate(Dictionary<string, (double Throughput, double Allocation)> overrides)
    {
        var cases = CaseNames.Select(name =>
        {
            var (t, a) = overrides.TryGetValue(name, out var o) ? o : (100.0, 0.01);
            return new
            {
                name,
                baseline = new { valuesPerSecond = 1.0, bytesPerValue = 1.0 },
                current = new { valuesPerSecond = t, bytesPerValue = a },
                throughputRatio = t,
                allocationRatio = a,
            };
        }).ToList();
        var folder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "report.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new { baseline = "f127a67", cases }));
        return Cli.Dotnet($"run -c Release --project \"{Path.Combine(Repo.Src, "GalaxyCheck.Benchmarks")}\" -- --gate \"{path}\"");
    }

    [Fact]
    public void test_001_S2_R50_gate_fails_on_throughput_ratio_99_and_names_the_case()
    {
        var gate = RunGate(new() { ["sample int"] = (99, 0.01) });
        gate.ExitCode.Should().Be(1, $"the gate must fail. Output:\n{gate.Output}");
        gate.Output.Should().Contain("sample int").And.Contain("99", $"the message names the case and the ratio. Output:\n{gate.Output}");
    }

    [Fact]
    public void test_001_S2_R50_gate_fails_on_allocation_ratio_0_02_and_names_the_case()
    {
        var gate = RunGate(new() { ["sample string"] = (100, 0.02) });
        gate.ExitCode.Should().Be(1, $"the gate must fail. Output:\n{gate.Output}");
        gate.Output.Should().Contain("sample string").And.Contain("0.02", $"the message names the case and the ratio. Output:\n{gate.Output}");
    }

    [Fact]
    public void test_001_S2_R50_gate_passes_when_each_ratio_is_on_target()
    {
        var gate = RunGate(new());
        gate.ExitCode.Should().Be(0, $"the gate must pass. Output:\n{gate.Output}");
    }

    // ---- README (R51) ----

    private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
        string.Join("\n", diagnostics.Select(d => d.ToString()));

    // The number of annotated methods that a block holds: 1 or more when it has such an attribute, else 0.
    private static int type0(string block) =>
        Regex.IsMatch(block, @"\[(Property|Sample|Replay)[\](]") ? 1 : 0;

    // Closes a generic extension method over the type argument of the property, where there is one.
    private static MethodInfo? Close(MethodInfo method, object property)
    {
        if (!method.IsGenericMethodDefinition)
        {
            return method;
        }

        var first = method.GetParameters()[method.IsStatic ? 0 : 0].ParameterType;
        var types = new[] { property.GetType() }.Concat(property.GetType().GetInterfaces())
            .Where(t => t.IsGenericType && first.IsGenericType && t.GetGenericTypeDefinition() == first.GetGenericTypeDefinition())
            .ToList();
        try { return types.Count > 0 && method.GetGenericArguments().Length == 1 ? method.MakeGenericMethod(types[0].GetGenericArguments()) : null; }
        catch (ArgumentException) { return null; }
    }

    // Runs the returned property with the check method of the library. A generator error or a replay error throws.
    private static void CheckProperty(object property, string? replay)
    {
        // The check methods are generic extension methods, so the runtime binder closes them over the property type.
        dynamic dynamicProperty = property;
        var isAsync = property.GetType().GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericArguments()[0].Name.StartsWith("AsyncTest"));
        if (isAsync)
        {
            ((System.Threading.Tasks.Task)GalaxyCheck.Extensions.CheckAsync(dynamicProperty, replay: replay)).GetAwaiter().GetResult();
        }
        else
        {
            GalaxyCheck.Extensions.Check(dynamicProperty, replay: replay);
        }
    }

    [Fact]
    public void test_001_S2_R51_each_readme_csharp_block_compiles_unchanged_and_runs()
    {
        var readme = File.ReadAllText(Path.Combine(Repo.Root, "README.md"));
        var blocks = Regex.Matches(readme, @"```csharp[ \t]*\r?\n(.*?)\r?\n```", RegexOptions.Singleline)
            .Select(m => m.Groups[1].Value).ToList();
        blocks.Should().NotBeEmpty("README.md must hold csharp code blocks");

        // Make sure that the libraries are loaded, then reference everything that is loaded.
        _ = typeof(Gen).Assembly;
        _ = typeof(PropertyAttribute).Assembly;
        _ = typeof(Xunit.FactAttribute).Assembly;
        var trusted = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "").Split(Path.PathSeparator);
        var paths = trusted.Concat(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a.Location.Length > 0).Select(a => a.Location))
            .Where(p => p.Length > 0).Distinct().ToList();
        var references = paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();

        var failures = new List<string>();
        for (var i = 0; i < blocks.Count; i++)
        {
            var source = "using GalaxyCheck;\n" + blocks[i];
            var compilation = CSharpCompilation.Create(
                $"ReadmeBlock{i}",
                [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            using var stream = new MemoryStream();
            var emit = compilation.Emit(stream);
            var errors = emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Count > 0)
            {
                failures.Add($"Block {i + 1} does not compile:\n{Describe(errors)}\n{blocks[i]}");
                continue;
            }

            try
            {
                var assembly = Assembly.Load(stream.ToArray());
                var ran = 0;
                foreach (var type in assembly.GetTypes())
                {
                    object? instance = null;
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        var attributes = method.GetCustomAttributesData().Select(a => a.AttributeType.Name).ToList();
                        if (!attributes.Any(n => n is "PropertyAttribute" or "SampleAttribute" or "ReplayAttribute")
                            || method.GetParameters().Length != 0)
                        {
                            continue;
                        }

                        var replay = method.GetCustomAttributesData()
                            .Where(a => a.AttributeType.Name == "ReplayAttribute")
                            .Select(a => a.ConstructorArguments[0].Value as string).FirstOrDefault();
                        if (!method.IsStatic)
                        {
                            instance ??= Activator.CreateInstance(type);
                        }

                        var property = method.Invoke(method.IsStatic ? null : instance, null);
                        property.Should().NotBeNull($"{type.Name}.{method.Name} must return a property");
                        CheckProperty(property!, replay);
                        ran++;
                    }
                }

                ran.Should().BeGreaterThanOrEqualTo(type0(blocks[i]), "each block with a [Property], [Sample], or [Replay] method must run it");
            }
            catch (Exception e)
            {
                failures.Add($"Block {i + 1} does not run: {(e is TargetInvocationException { InnerException: { } inner } ? inner : e)}\n{blocks[i]}");
            }
        }

        failures.Should().BeEmpty(string.Join("\n\n", failures));
    }
}
