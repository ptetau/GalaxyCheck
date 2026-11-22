using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>The public members and constructors of one public type of an assembly, read from the metadata.</summary>
internal record S11TypeMeta(HashSet<string> Members, List<int> ConstructorParameterCounts);

internal static class S11Metadata
{
    public static Dictionary<string, S11TypeMeta> Read(Stream stream)
    {
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();
        var result = new Dictionary<string, S11TypeMeta>();
        foreach (var handle in md.TypeDefinitions)
        {
            var type = md.GetTypeDefinition(handle);
            if ((type.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.Public)
            {
                continue;
            }

            var members = new HashSet<string>();
            foreach (var p in type.GetProperties())
            {
                members.Add(md.GetString(md.GetPropertyDefinition(p).Name));
            }

            foreach (var f in type.GetFields())
            {
                var field = md.GetFieldDefinition(f);
                if ((field.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public)
                {
                    members.Add(md.GetString(field.Name));
                }
            }

            var constructors = new List<int>();
            foreach (var m in type.GetMethods())
            {
                var method = md.GetMethodDefinition(m);
                if (md.GetString(method.Name) == ".ctor" && (method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public)
                {
                    constructors.Add(method.GetParameters().Count(h => md.GetParameter(h).SequenceNumber > 0));
                }
            }

            var ns = md.GetString(type.Namespace);
            var name = md.GetString(type.Name);
            result[ns.Length == 0 ? name : ns + "." + name] = new S11TypeMeta(members, constructors);
        }

        return result;
    }
}

public class S11_PackageTests
{
    private static readonly Lazy<(CliResult Run, string? Package)> TUnitPack = new(() =>
    {
        var folder = Path.Combine(Path.GetTempPath(), "galaxycheck-slice", "S11-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var run = Cli.Dotnet($"pack src/GalaxyCheck.TUnit -c Release -o \"{folder}\"", Repo.Root);
        var package = Directory.GetFiles(folder, "GalaxyCheck.TUnit.*.nupkg").FirstOrDefault();
        return (run, package);
    });

    private static readonly string[] AttributeNames =
    {
        "GalaxyCheck.PropertyAttribute",
        "GalaxyCheck.SampleAttribute",
        "GalaxyCheck.GenSnapshotAttribute",
        "GalaxyCheck.ReplayAttribute",
        "GalaxyCheck.MemberGenAttribute",
    };

    private static Dictionary<string, S11TypeMeta> TUnitTypes()
    {
        var (run, package) = TUnitPack.Value;
        package.Should().NotBeNull(because: run.Output);
        using var zip = ZipFile.OpenRead(package!);
        var entry = zip.GetEntry("lib/net10.0/GalaxyCheck.TUnit.dll");
        entry.Should().NotBeNull(because: "the package lib folders: " + string.Join(", ", zip.Entries.Select(e => e.FullName)));
        using var memory = new MemoryStream();
        using (var stream = entry!.Open())
        {
            stream.CopyTo(memory);
        }

        memory.Position = 0;
        return S11Metadata.Read(memory);
    }

    private static Dictionary<string, S11TypeMeta> XunitTypes()
    {
        var path = Assembly.Load("GalaxyCheck.Xunit").Location;
        using var stream = File.OpenRead(path);
        return S11Metadata.Read(stream);
    }

    [Fact]
    public void test_001_S8_R22_tunit_pack_holds_net10_and_net11_and_no_other_target()
    {
        var (run, package) = TUnitPack.Value;
        package.Should().NotBeNull(because: run.Output);
        using var zip = ZipFile.OpenRead(package!);
        var targets = zip.Entries
            .Select(e => e.FullName.Split('/'))
            .Where(parts => parts.Length > 2 && parts[0] == "lib")
            .Select(parts => parts[1])
            .Distinct()
            .ToList();
        targets.Should().Contain("net10.0", because: run.Output);
        targets.Should().Contain("net11.0", because: run.Output);
    }

    [Fact]
    public void test_001_S8_R22_tunit_public_attributes_exist_with_the_options_of_xunit()
    {
        var tunit = TUnitTypes();
        var xunit = XunitTypes();
        var expectedOptions = new Dictionary<string, string[]>
        {
            ["GalaxyCheck.PropertyAttribute"] = new[] { "Iterations", "ShrinkLimit", "Seed", "Size", "TimeoutMilliseconds", "Exhaustive" },
            ["GalaxyCheck.GenSnapshotAttribute"] = new[] { "Iterations", "Size" },
        };

        foreach (var name in AttributeNames)
        {
            tunit.Should().ContainKey(name);
            xunit.Should().ContainKey(name, because: "GalaxyCheck.Xunit is the reference");

            var missing = xunit[name].Members.Where(m => !tunit[name].Members.Contains(m)).ToList();
            missing.Should().BeEmpty(because: $"{name} of GalaxyCheck.TUnit must have each option of GalaxyCheck.Xunit");

            if (expectedOptions.TryGetValue(name, out var options))
            {
                tunit[name].Members.Should().Contain(options, because: name);
            }
        }

        // The attributes with a constructor argument: Replay(replay) and MemberGen(memberName).
        tunit["GalaxyCheck.ReplayAttribute"].ConstructorParameterCounts.Should().Contain(1);
        tunit["GalaxyCheck.MemberGenAttribute"].ConstructorParameterCounts.Should().Contain(1);
    }

    [Fact]
    public void test_001_S8_R22_tunit_has_property_test_attribute_and_property_arguments_attribute()
    {
        var tunit = TUnitTypes();
        tunit.Should().ContainKey("GalaxyCheck.PropertyAttribute");
        tunit.Should().ContainKey("GalaxyCheck.PropertyTestAttribute");
        tunit.Should().ContainKey("GalaxyCheck.PropertyArgumentsAttribute");

        var options = new[] { "Iterations", "ShrinkLimit", "Seed", "Size", "TimeoutMilliseconds", "Exhaustive" };
        tunit["GalaxyCheck.PropertyTestAttribute"].Members.Should().Contain(options);
        tunit["GalaxyCheck.PropertyTestAttribute"].Members.Should().Contain(tunit["GalaxyCheck.PropertyAttribute"].Members.Where(m => options.Contains(m)));

        // params object?[] values: one constructor parameter.
        tunit["GalaxyCheck.PropertyArgumentsAttribute"].ConstructorParameterCounts.Should().Contain(1);
    }
}
