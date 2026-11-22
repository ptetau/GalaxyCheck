using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S11_ReferenceTests
{
    private static readonly string[] Packages =
    {
        "GalaxyCheck", "GalaxyCheck.Xunit", "GalaxyCheck.Xunit.V3", "GalaxyCheck.TUnit", "GalaxyCheck.NUnit", "GalaxyCheck.MSTest",
    };

    private static readonly Dictionary<string, Lazy<(string? Package, CliResult Run)>> Packs =
        Packages.ToDictionary(
            name => name,
            name => new Lazy<(string?, CliResult)>(() =>
            {
                var folder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);
                var run = Cli.Dotnet($"pack \"{Path.Combine(Repo.Src, name)}\" -c Release -o \"{folder}\"");
                return (Directory.GetFiles(folder, "*.nupkg").FirstOrDefault(), run);
            }));

    private static string ReadLlms()
    {
        var path = Path.Combine(Repo.Root, "docs", "llms.txt");
        File.Exists(path).Should().BeTrue("docs/llms.txt must exist");
        return File.ReadAllText(path);
    }

    private static Type? RealisticType()
    {
        var nested = typeof(Gen).GetNestedType("Realistic");
        return nested ?? typeof(Gen).GetProperty("Realistic", BindingFlags.Public | BindingFlags.Static)?.PropertyType;
    }

    private static IEnumerable<string> PublicStaticMethodNames(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n);

    private static bool IsGeneratorInterface(Type t) =>
        t.IsInterface
        && t.IsPublic | t.IsNestedPublic
        && !(t.IsGenericType ? t.GetGenericTypeDefinition() == typeof(IGen<>) : t.Name == "IGen")
        && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IGen<>));

    private static IEnumerable<string> OptionMethodNames()
    {
        var assembly = typeof(Gen).Assembly;
        var interfaces = assembly.GetExportedTypes().Where(IsGeneratorInterface).ToList();
        var names = interfaces
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name);
        var definitions = interfaces.Select(t => t.IsGenericType ? t.GetGenericTypeDefinition() : t).ToHashSet();
        var extensions = assembly.GetExportedTypes()
            .Where(t => t.IsAbstract && t.IsSealed)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => m.IsDefined(typeof(ExtensionAttribute), false))
            .Where(m =>
            {
                var first = m.GetParameters()[0].ParameterType;
                return definitions.Contains(first.IsGenericType ? first.GetGenericTypeDefinition() : first);
            })
            .Select(m => m.Name);
        return names.Concat(extensions).Distinct().OrderBy(n => n);
    }

    [Fact]
    public void test_001_S11_R65_llms_file_has_at_most_40000_characters()
    {
        ReadLlms().Length.Should().BeLessThanOrEqualTo(40000);
    }

    [Fact]
    public void test_001_S11_R65_llms_file_holds_each_gen_method_with_a_code_example()
    {
        var text = ReadLlms();
        var missing = PublicStaticMethodNames(typeof(Gen))
            .Where(name => !Regex.IsMatch(text, $@"\bGen\.{Regex.Escape(name)}\s*[<(]"))
            .ToList();

        missing.Should().BeEmpty("each public method of Gen needs a line with an example such as Gen.Name(...)");
    }

    [Fact]
    public void test_001_S11_R65_llms_file_holds_each_realistic_method_with_a_code_example()
    {
        var realistic = RealisticType();
        realistic.Should().NotBeNull("Gen.Realistic must exist");
        var text = ReadLlms();
        var missing = PublicStaticMethodNames(realistic!)
            .Where(name => !Regex.IsMatch(text, $@"\bRealistic\.{Regex.Escape(name)}\s*[<(]"))
            .ToList();

        missing.Should().BeEmpty("each public method of Gen.Realistic needs a line with an example such as Gen.Realistic.Name(...)");
    }

    [Fact]
    public void test_001_S11_R65_llms_file_holds_each_generator_option_method_with_a_code_example()
    {
        var text = ReadLlms();
        var names = OptionMethodNames().ToList();
        names.Should().NotBeEmpty();
        var missing = names.Where(name => !Regex.IsMatch(text, $@"\.{Regex.Escape(name)}\s*[<(]")).ToList();

        missing.Should().BeEmpty("each option method of a generator interface needs a line with an example such as .Name(...)");
    }

    [Fact]
    public void test_001_S11_R65_llms_file_holds_each_attribute_with_a_code_example()
    {
        var text = ReadLlms();
        var assembly = Assembly.Load("GalaxyCheck.Xunit");
        var names = assembly.GetExportedTypes()
            .Where(t => typeof(Attribute).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => Regex.Replace(t.Name, "Attribute$", ""))
            .Distinct()
            .ToList();
        names.Should().NotBeEmpty();
        var missing = names.Where(name => !Regex.IsMatch(text, $@"\[\s*(GalaxyCheck\.)?{Regex.Escape(name)}\b")).ToList();

        missing.Should().BeEmpty("each attribute needs a line with an example such as [Name(...)]");
    }

    [Theory]
    [InlineData("GalaxyCheck")]
    [InlineData("GalaxyCheck.Xunit")]
    [InlineData("GalaxyCheck.Xunit.V3")]
    [InlineData("GalaxyCheck.TUnit")]
    [InlineData("GalaxyCheck.NUnit")]
    [InlineData("GalaxyCheck.MSTest")]
    public void test_001_S11_R65_package_holds_llms_file_and_xml_documentation(string project)
    {
        var (package, run) = Packs[project].Value;
        package.Should().NotBeNull($"dotnet pack must write a package. Output:\n{run.Output}");
        using var zip = ZipFile.OpenRead(package!);
        var entries = zip.Entries.Select(e => e.FullName).ToList();

        entries.Should().Contain("llms.txt", $"the package must hold llms.txt at its root. Entries:\n{string.Join("\n", entries)}");
        var dlls = entries.Where(e => e.StartsWith("lib/") && e.EndsWith(".dll")).ToList();
        dlls.Should().NotBeEmpty();
        foreach (var dll in dlls)
        {
            entries.Should().Contain(Path.ChangeExtension(dll, ".xml"), $"an XML documentation file must sit next to {dll}");
        }
    }

    [Fact]
    public void test_001_S11_R65_galaxycheck_xml_documentation_has_a_summary_for_each_public_type_and_member()
    {
        var (package, run) = Packs["GalaxyCheck"].Value;
        package.Should().NotBeNull($"dotnet pack must write a package. Output:\n{run.Output}");
        using var zip = ZipFile.OpenRead(package!);
        var entry = zip.Entries.FirstOrDefault(e => e.FullName == "lib/net11.0/GalaxyCheck.xml");
        entry.Should().NotBeNull("the package must hold lib/net11.0/GalaxyCheck.xml");
        using var stream = entry!.Open();
        var documented = XDocument.Load(stream)
            .Descendants("member")
            .Where(m => !string.IsNullOrWhiteSpace(m.Element("summary")?.Value))
            .Select(m => Normalize((string)m.Attribute("name")!))
            .GroupBy(n => n)
            .ToDictionary(g => g.Key, g => g.Count());

        var missing = new List<string>();
        foreach (var type in typeof(Gen).Assembly.GetExportedTypes())
        {
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                continue;
            }

            Require("T:" + type.FullName!.Replace('+', '.'), 1);
            if (typeof(Delegate).IsAssignableFrom(type))
            {
                continue;
            }

            var isRecord = type.GetMethod("<Clone>$") != null;
            var groups = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => m.MemberType is MemberTypes.Method or MemberTypes.Constructor or MemberTypes.Property or MemberTypes.Field or MemberTypes.Event)
                .Where(m => !m.IsDefined(typeof(CompilerGeneratedAttribute), false))
                .Where(m => m is not MethodBase method || (!method.IsSpecialName || method.IsConstructor))
                .Where(m => m is not FieldInfo field || !field.IsSpecialName)
                .Where(m => m is not ConstructorInfo ctor || ctor.GetParameters().Length > 0)
                .Where(m => m is not MethodInfo info || info.GetBaseDefinition().DeclaringType == info.DeclaringType)
                .Where(m => !(isRecord && m.Name is "Equals" or "Deconstruct"))
                .GroupBy(m => Kind(m) + ":" + type.FullName!.Replace('+', '.') + "." + (m is ConstructorInfo ? "#ctor" : m.Name));
            foreach (var group in groups)
            {
                Require(group.Key, group.Count());
            }
        }

        missing.Should().BeEmpty("each public type and member needs a summary. Missing:\n" + string.Join("\n", missing.Take(60)));

        void Require(string key, int count)
        {
            var normalized = Normalize(key);
            if (!documented.TryGetValue(normalized, out var have) || have < count)
            {
                missing.Add($"{key} (needs {count}, has {(documented.TryGetValue(normalized, out var h) ? h : 0)})");
            }
        }
    }

    private static string Kind(MemberInfo m) => m.MemberType switch
    {
        MemberTypes.Property => "P",
        MemberTypes.Field => "F",
        MemberTypes.Event => "E",
        _ => "M",
    };

    private static string Normalize(string id)
    {
        var open = id.IndexOf('(');
        if (open >= 0)
        {
            id = id[..open];
        }

        return Regex.Replace(id, "`+\\d+", "");
    }
}
