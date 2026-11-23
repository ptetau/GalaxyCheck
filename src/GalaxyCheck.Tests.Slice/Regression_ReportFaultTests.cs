using System.Text.Json;
using FluentAssertions;
using GalaxyCheck.Runners;
using GalaxyCheck.Stateful;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class Regression_ReportFaultTests
{
    private const string Section = "Counterexample as C#:";

    private static string FailureMessage(Action assert)
    {
        try
        {
            assert();
        }
        catch (PropertyFailedException e)
        {
            return e.Message;
        }

        throw new Xunit.Sdk.XunitException("The property did not fail");
    }

    /// <summary>The readable part of a failure message: the text before the JSON block.</summary>
    private static string ReadableText(string message) =>
        message.Substring(0, message.IndexOf(S11Support.StartMarker, StringComparison.Ordinal));

    /// <summary>The expression lines under the heading "Counterexample as C#:", or an empty list with no heading.</summary>
    private static List<string> CSharpSection(string message)
    {
        var lines = ReadableText(message).Replace("\r\n", "\n").Split('\n').ToList();
        var start = lines.IndexOf(Section);
        if (start < 0) return new List<string>();
        return lines.Skip(start + 1).TakeWhile(l => l.StartsWith("    ", StringComparison.Ordinal)).Select(l => l.Trim()).ToList();
    }

    // Fault 1: a filter with a real pass rate of several percent adapts, and does not trip the discard check.

    [Fact]
    public void test_001_fix_combined_filter_adapts_and_passes_the_discard_check()
    {
        // About a quarter of the full range passes. Each half of the condition passes alone.
        var property = Property.ForAll(Gen.Int32().Where(x => x > 1_000_000 && x % 2 == 0), x => true);

        for (var seed = 0; seed < 20; seed++)
        {
            var act = () => property.Check(seed: seed);

            var result = act.Should().NotThrow($"seed {seed}").Subject;
            result.Falsified.Should().BeFalse($"seed {seed}");
            result.Discards.Should().BeLessThan(9 * result.Iterations, $"seed {seed}: at most 90% of the values are discarded");
        }

        // A filter that can almost never pass still trips the check.
        var never = Property.ForAll(Gen.Int32().Where(x => x == 123456789), x => true);
        var tripped = () => never.Check(seed: 0);
        tripped.Should().Throw<HealthCheckException>().Which.Check.Should().Be(HealthCheck.TooManyDiscards);

        // A filter that keeps a bound of the range still finds it after it rejects the other bound.
        var bound = Property.ForAll(Gen.Int32().Where(x => x == int.MaxValue), x => true);
        for (var seed = 0; seed < 5; seed++)
        {
            var act = () => bound.Check(seed: seed);
            act.Should().NotThrow($"seed {seed}");
        }
    }

    // Fault 2: a coverage failure holds no counterexample and no replay string.

    [Fact]
    public void test_001_fix_coverage_failure_has_no_counterexample_or_replay()
    {
        // 100 of the 1000 values (10%) have the label, and the rule asks for 50%.
        var property = Property.ForAll(Gen.Int32().Between(1, 1000).WithBias(Gen.Bias.None), x => true)
            .Cover("rare", 50, x => x <= 100);

        var result = property.Check(iterations: 100, seed: 1);
        result.Falsified.Should().BeTrue();
        result.Counterexample.Should().BeNull("a coverage failure has no failing test case");
        result.CoverageFailure.Should().NotBeNull();
        result.CoverageFailure!.Label.Should().Be("rare");
        result.CoverageFailure.RequiredPercent.Should().Be(50);
        result.CoverageFailure.ActualPercent.Should().BeLessThan(50);
        result.Report.Should().Contain("rare").And.Contain("50%").And.NotContain("Counterexample").And.NotContain("Reproduction");

        var message = FailureMessage(() => property.Assert(iterations: 100, seed: 1));
        var text = ReadableText(message);
        text.Should().Contain("rare").And.Contain("50%")
            .And.Contain(PercentFormatOf(result.CoverageFailure.ActualPercent));
        text.Should().NotContain("Counterexample").And.NotContain(Section).And.NotContain("Reproduction")
            .And.NotContain("Replay");

        using var json = S11Support.ExtractFailure(message);
        var root = json.RootElement;
        root.GetProperty("kind").GetString().Should().Be("coverage");
        root.GetProperty("counterexample").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("csharp").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("replay").ValueKind.Should().Be(JsonValueKind.Null);
        foreach (var key in new[] { "seed", "size", "iterations", "shrinks" })
        {
            root.TryGetProperty(key, out _).Should().BeTrue($"the key {key} is kept");
        }

        // A generated failure names its kind too.
        var failing = Property.ForAll(Gen.Int32(), x => x < 10);
        using var generated = S11Support.ExtractFailure(FailureMessage(() => failing.Assert(iterations: 1000, seed: 1)));
        generated.RootElement.GetProperty("kind").GetString().Should().Be("generated");
        generated.RootElement.GetProperty("replay").GetString().Should().NotBeNullOrEmpty();
    }

    private static string PercentFormatOf(double percent) =>
        Math.Round(percent, 1).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";

    // Fault 3: the text under "Counterexample as C#" is always valid C#, or the section is left out.

    private sealed class Box
    {
        public int Value;
    }

    private sealed class Inc : Command<Box, int>
    {
        // The system skips the step from 2 to 3, so the third increment fails its postcondition.
        public override void Run(Box system) => system.Value += system.Value == 2 ? 2 : 1;

        public override int NextModel(int model) => model + 1;

        public override bool Postcondition(Box system, int modelBefore, int modelAfter) => system.Value == modelAfter;

        public override string ToString() => "Inc";
    }

    [Fact]
    public void test_001_fix_stateful_and_nested_values_without_csharp_form_have_no_csharp_section()
    {
        var stateful = StatefulProperty.Create(() => new Box(), () => 0, _ => Gen.Constant<Command<Box, int>>(new Inc()));

        var message = FailureMessage(() => stateful.Assert(seed: 1));

        var text = ReadableText(message);
        text.Should().Contain("<- failed");
        text.Should().NotContain(Section, "the command list shows the failing run, and it has no C# form");
        using (var json = S11Support.ExtractFailure(message))
        {
            json.RootElement.GetProperty("csharp").EnumerateArray().Should().OnlyContain(e => e.ValueKind == JsonValueKind.Null);
        }

        // A tuple that holds a function has no C# form, so it never appears under the heading.
        Func<int, int> function = x => x;
        var tuple = Property.ForAll(Gen.ValueTuple(Gen.Int32(), Gen.Element(new[] { function })), t => false);
        var tupleMessage = FailureMessage(() => tuple.Assert(iterations: 10, seed: 1));
        ReadableText(tupleMessage).Should().NotContain(Section);
        using (var json = S11Support.ExtractFailure(tupleMessage))
        {
            json.RootElement.GetProperty("csharp").EnumerateArray().Should().OnlyContain(e => e.ValueKind == JsonValueKind.Null);
        }

        // Two parameters, one with a C# form and one without: the section is left out, not filled with other text.
        var pair = Property.ForAll(Gen.Int32(), Gen.Element(new[] { function }), (x, f) => false);
        ReadableText(FailureMessage(() => pair.Assert(iterations: 10, seed: 1))).Should().NotContain(Section);

        // A value with a C# form keeps the section, and each line under it is C# that makes the value.
        var plain = Property.ForAll(Gen.ValueTuple(Gen.Int32(), Gen.Int32()), t => t.Item1 < 10);
        var expressions = CSharpSection(FailureMessage(() => plain.Assert(iterations: 1000, seed: 1)));
        expressions.Should().ContainSingle();
        var value = CSharpScript.EvaluateAsync<object?>(expressions[0], ScriptOptions.Default).GetAwaiter().GetResult();
        value.Should().Be((10, 0));
    }
}
