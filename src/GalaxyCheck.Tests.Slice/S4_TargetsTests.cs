using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>Runs the sample S6/Targets on net10.0 and on net11.0.</summary>
public class S6_TargetsTests
{
    private static readonly string Project = Path.Combine(Repo.Samples, "S6", "Targets");

    private static readonly Lazy<CliResult> Net10 = new(() => Cli.Dotnet($"run --project \"{Project}\" -f net10.0", Repo.Samples));

    private static readonly Lazy<CliResult> Net11 = new(() => Cli.Dotnet($"run --project \"{Project}\" -f net11.0", Repo.Samples));

    [Fact]
    public void test_001_S4_R18_net10_assembly_has_none_of_the_new_members()
    {
        var run = Net10.Value;

        run.ExitCode.Should().Be(0, "the sample must run: {0}", run.Output);
        Lines(run, "RUNTIME").Should().ContainSingle().Which.Should().Be("RUNTIME 10", "the sample must run on net10.0: {0}", run.Output);
        Lines(run, "MEMBERS").Should().ContainSingle().Which.Should().Be("MEMBERS ", "net10.0 must have none of the members: {0}", run.Output);
        Lines(run, "GEN").Should().NotBeEmpty("the sample must print its values: {0}", run.Output);
    }

    [Fact]
    public void test_001_S4_R20_same_values_on_net10_and_net11()
    {
        var net10 = Net10.Value;
        var net11 = Net11.Value;

        net10.ExitCode.Should().Be(0, "the sample must run on net10.0: {0}", net10.Output);
        net11.ExitCode.Should().Be(0, "the sample must run on net11.0: {0}", net11.Output);
        Lines(net11, "MEMBERS").Should().ContainSingle().Which.Should().Be("MEMBERS Decimal32,Decimal64,Decimal128,BFloat16,ComplexOf");

        var first = Lines(net10, "GEN");
        var second = Lines(net11, "GEN");
        first.Should().HaveCountGreaterThanOrEqualTo(19, "each generator must print one line: {0}", net10.Output);
        first.Should().NotContain(line => line.StartsWith("GEN-ERROR"), "{0}", net10.Output);
        second.Should().NotContain(line => line.StartsWith("GEN-ERROR"), "{0}", net11.Output);
        first.Should().Equal(second, "the values must be the same on both targets");
    }

    [Fact]
    public void test_001_S5_R20_same_text_values_on_net10_and_net11()
    {
        var net10 = Net10.Value;
        var net11 = Net11.Value;

        net10.ExitCode.Should().Be(0, "the sample must run on net10.0: {0}", net10.Output);
        net11.ExitCode.Should().Be(0, "the sample must run on net11.0: {0}", net11.Output);

        var first = Lines(net10, "TEXT");
        var second = Lines(net11, "TEXT");
        first.Should().HaveCountGreaterThanOrEqualTo(13, "each generator must print one line: {0}", net10.Output);
        first.Should().NotContain(line => line.StartsWith("TEXT-ERROR"), "{0}", net10.Output);
        second.Should().NotContain(line => line.StartsWith("TEXT-ERROR"), "{0}", net11.Output);
        first.Should().Equal(second, "the values must be the same on both targets");
    }

    private static List<string> Lines(CliResult run, string prefix) =>
        run.Output.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l == prefix || l.StartsWith(prefix + " ") || l.StartsWith(prefix + "-ERROR")).ToList();
}
