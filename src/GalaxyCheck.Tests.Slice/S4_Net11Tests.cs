using System.Globalization;
using System.Numerics;
using System.Reflection;
using FluentAssertions;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>The tests of the types that only net11.0 has. This project runs on net11.0.</summary>
public class S6_Net11Tests
{
    private static readonly Lazy<CliResult> UnionRun = new(() =>
        Cli.Dotnet($"run --project \"{Path.Combine(Repo.Samples, "S6", "Unions")}\" -f net11.0", Repo.Samples));

    [Fact]
    public void test_001_S4_R18_net11_assembly_has_the_new_members_and_they_work()
    {
        var names = typeof(Gen).GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name).ToHashSet();
        names.Should().Contain(new[] { "Decimal32", "Decimal64", "Decimal128", "BFloat16", "ComplexOf" });

        Gen.Decimal32().Sample(iterations: 10, seed: 0).Should().HaveCount(10);
        Gen.Decimal64().Sample(iterations: 10, seed: 0).Should().HaveCount(10);
        Gen.Decimal128().Sample(iterations: 10, seed: 0).Should().HaveCount(10);
        Gen.BFloat16().Sample(iterations: 10, seed: 0).Should().HaveCount(10);
        Gen.ComplexOf(Gen.Double(), Gen.Double()).Sample(iterations: 10, seed: 0).Should().HaveCount(10);
    }

    [Fact]
    public void test_001_S4_R18_bfloat16_has_special_values_and_an_opt_out()
    {
        Special(Gen.BFloat16());
    }

    [Fact]
    public void test_001_S4_R18_decimal64_shrinks_to_three()
    {
        ShrinksToThree(Gen.Decimal64());
    }

    [Fact]
    public void test_001_S4_R18_create_decimal128_equals_the_explicit_generator()
    {
        var problems = new List<string>();
        S6Support.AssertCreateEqualsExplicit(Gen.Decimal128(), problems);
        S6Support.AssertCreateEqualsExplicit(Gen.Decimal32(), problems);
        S6Support.AssertCreateEqualsExplicit(Gen.Decimal64(), problems);
        S6Support.AssertCreateEqualsExplicit(Gen.BFloat16(), problems);

        problems.Should().BeEmpty("each type must give the same values from Gen.Create<T>() and from its explicit generator: {0}", string.Join("; ", problems));
    }

    [Fact]
    public void test_001_S4_R33_union_gives_each_case()
    {
        var run = UnionRun.Value;

        run.ExitCode.Should().Be(0, "the sample must run: {0}", run.Output);
        run.Output.Should().Contain("CASES Bird,Cat,Dog", "each of the three cases must appear: {0}", run.Output);
    }

    private static void Special<T>(IRealGen<T> gen) where T : IFloatingPointIeee754<T>
    {
        var nan = false;
        var positiveInfinity = false;
        var negativeZero = false;
        for (var seed = 0; seed < 100; seed++)
        {
            foreach (var value in gen.Sample(iterations: 100, seed: seed))
            {
                nan |= T.IsNaN(value);
                positiveInfinity |= T.IsPositiveInfinity(value);
                negativeZero |= T.IsZero(value) && T.IsNegative(value);
            }
        }

        nan.Should().BeTrue("NaN must appear");
        positiveInfinity.Should().BeTrue("positive infinity must appear");
        negativeZero.Should().BeTrue("negative zero must appear");

        for (var seed = 0; seed < 100; seed++)
        {
            var values = gen.WithoutSpecialValues().Sample(iterations: 100, seed: seed);
            values.All(value => T.IsFinite(value)).Should().BeTrue("each value must be finite with the opt-out");
        }
    }

    private static void ShrinksToThree<T>(IRealGen<T> gen) where T : INumber<T>
    {
        var limit = T.Parse("2.5", NumberStyles.Float, CultureInfo.InvariantCulture);
        var three = T.Parse("3", NumberStyles.Float, CultureInfo.InvariantCulture);

        var result = Property.ForAll(gen, x => x < limit).Check(iterations: 100, seed: 0);

        result.Falsified.Should().BeTrue();
        (result.Counterexample!.Value == three).Should().BeTrue("the counterexample must be 3, not {0}", result.Counterexample.Value);
    }
}
