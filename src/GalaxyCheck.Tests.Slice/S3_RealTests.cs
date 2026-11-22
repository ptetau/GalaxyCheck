using System.Numerics;
using System.Text;
using FluentAssertions;
using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S4_RealTests
{
    private static bool HasFraction(double x) => double.IsFinite(x) && x != Math.Floor(x);

    private static List<double> AsDoubles<T>(IEnumerable<T> values, Func<T, double> convert) => values.Select(convert).ToList();

    private static void AssertFractionNegativePositive(List<double> values, string name)
    {
        values.Should().Contain(x => HasFraction(x), $"{name} must produce a fraction part");
        values.Should().Contain(x => x < 0, $"{name} must produce a negative value");
        values.Should().Contain(x => x > 0, $"{name} must produce a positive value");
    }

    private static object? Counterexample<T>(IGen<T> gen, Func<T, bool> holds, int seed = 1)
    {
        var result = Property.ForAll(gen, holds).Check(iterations: 1000, seed: seed);
        result.Falsified.Should().BeTrue("the property must fail");
        return result.Counterexample!.Value;
    }

    // R5

    [Fact]
    public void test_001_S3_R5_half_has_fraction_negative_positive()
    {
        var values = Gen.Half().WithoutSpecialValues().Sample(iterations: 1000, seed: 1);
        AssertFractionNegativePositive(AsDoubles(values, x => (double)x), "Half");
    }

    [Fact]
    public void test_001_S3_R5_single_has_fraction_negative_positive()
    {
        var values = Gen.Single().WithoutSpecialValues().Sample(iterations: 1000, seed: 1);
        AssertFractionNegativePositive(AsDoubles(values, x => (double)x), "Single");
    }

    [Fact]
    public void test_001_S3_R5_double_has_fraction_negative_positive()
    {
        var values = Gen.Double().WithoutSpecialValues().Sample(iterations: 1000, seed: 1);
        AssertFractionNegativePositive(values, "Double");
    }

    [Fact]
    public void test_001_S3_R5_double_reaches_full_finite_range()
    {
        var gen = Gen.Double().WithoutSpecialValues();
        var all = Enumerable.Range(0, 100).SelectMany(seed => gen.Sample(iterations: 100, seed: seed)).ToList();
        all.Should().Contain(x => x > 1e300, "a value above 1e300 must appear");
        all.Should().Contain(x => x < -1e300, "a value below -1e300 must appear");
    }

    // R6

    [Fact]
    public void test_001_S3_R6_double_between_stays_in_bounds()
    {
        var values = Gen.Double().Between(1.5, 2.5).Sample(iterations: 1000, seed: 1);
        values.Should().HaveCount(1000);
        values.Should().OnlyContain(x => x >= 1.5 && x <= 2.5);
    }

    [Fact]
    public void test_001_S3_R6_decimal_between_stays_in_bounds()
    {
        var values = Gen.Decimal().Between(0.10m, 0.20m).Sample(iterations: 1000, seed: 1);
        values.Should().HaveCount(1000);
        values.Should().OnlyContain(x => x >= 0.10m && x <= 0.20m);
    }

    // R9

    [Fact]
    public void test_001_S3_R9_double_below_3_5_shrinks_to_4()
    {
        Counterexample(Gen.Double(), x => x < 3.5).Should().Be(4.0);
    }

    [Fact]
    public void test_001_S3_R9_single_always_failing_shrinks_to_zero()
    {
        Counterexample(Gen.Single(), _ => false).Should().Be(0f);
    }

    [Fact]
    public void test_001_S3_R9_double_not_nan_below_1e10_shrinks_to_1e10()
    {
        Counterexample(Gen.Double(), x => !double.IsNaN(x) && x < 1e10).Should().Be(1e10);
    }

    [Fact]
    public void test_001_S3_R9_decimal_below_2_5_shrinks_to_3()
    {
        Counterexample(Gen.Decimal(), x => x < 2.5m).Should().Be(3m);
    }

    // R7

    [Fact]
    public void test_001_S3_R7_double_special_values_appear_for_most_seeds()
    {
        var gen = Gen.Double();
        var samples = Enumerable.Range(0, 100).Select(seed => gen.Sample(iterations: 100, seed: seed)).ToList();

        void AtLeast90(string name, Func<double, bool> isSpecial)
        {
            var seeds = samples.Count(values => values.Any(isSpecial));
            seeds.Should().BeGreaterThanOrEqualTo(90, $"{name} must appear in 100 values for at least 90 of 100 seeds");
        }

        AtLeast90("NaN", double.IsNaN);
        AtLeast90("positive infinity", double.IsPositiveInfinity);
        AtLeast90("negative infinity", double.IsNegativeInfinity);
        AtLeast90("negative zero", x => x == 0 && double.IsNegative(x));
        AtLeast90("double.Epsilon", x => x == double.Epsilon);
        AtLeast90("double.MinValue", x => x == double.MinValue);
        AtLeast90("double.MaxValue", x => x == double.MaxValue);
    }

    [Fact]
    public void test_001_S3_R7_single_between_never_gives_nan_or_infinity_or_outside_bounds()
    {
        var values = Gen.Single().Between(-1f, 1f).Sample(iterations: 10000, seed: 1);
        values.Should().HaveCount(10000);
        values.Should().OnlyContain(x => !float.IsNaN(x) && !float.IsInfinity(x) && x >= -1f && x <= 1f);
    }

    // R8

    [Fact]
    public void test_001_S3_R8_double_without_special_values_is_finite()
    {
        var values = Gen.Double().WithoutSpecialValues().Sample(iterations: 10000, seed: 1);
        values.Should().HaveCount(10000);
        values.Should().OnlyContain(x => double.IsFinite(x));
    }

    [Fact]
    public void test_001_S3_R8_decimal_without_special_values_has_no_min_or_max()
    {
        var values = Gen.Decimal().WithoutSpecialValues().Sample(iterations: 10000, seed: 1);
        values.Should().HaveCount(10000);
        values.Should().NotContain(decimal.MinValue);
        values.Should().NotContain(decimal.MaxValue);
    }

    // R10

    [Fact]
    public void test_001_S3_R10_decimal_has_fraction_zero_min_and_max()
    {
        var gen = Gen.Decimal();
        var all = Enumerable.Range(0, 100).SelectMany(seed => gen.Sample(iterations: 100, seed: seed)).ToList();
        all.Should().Contain(x => x != decimal.Truncate(x), "a value with a fraction part must appear");
        all.Should().Contain(0m);
        all.Should().Contain(decimal.MinValue);
        all.Should().Contain(decimal.MaxValue);
    }

    // R12

    [Fact]
    public void test_001_S3_R12_rune_is_valid_and_spans_planes()
    {
        var values = Gen.Rune().Sample(iterations: 10000, seed: 1);
        values.Should().HaveCount(10000);
        values.Should().NotContain(r => r.Value >= 0xD800 && r.Value <= 0xDFFF, "no surrogate is a Rune");
        values.Should().Contain(r => r.Value > 0xFFFF, "a value above FFFF must appear");
        values.Should().Contain(r => r.Value < 0x10000, "a value below 10000 must appear");
    }

    [Fact]
    public void test_001_S3_R12_rune_always_failing_shrinks_to_a()
    {
        Counterexample(Gen.Rune(), _ => false).Should().Be(new Rune('a'));
    }

    // R13

    [Fact]
    public void test_001_S3_R13_complex_has_nan_part_and_fractions()
    {
        var values = Gen.Complex().Sample(iterations: 1000, seed: 1);
        values.Should().HaveCount(1000);
        values.Should().Contain(c => double.IsNaN(c.Real) || double.IsNaN(c.Imaginary), "a value with a NaN part must appear");
        values.Should().Contain(c => HasFraction(c.Real) && HasFraction(c.Imaginary), "a value with a fraction in each part must appear");
    }

    [Fact]
    public void test_001_S3_R13_complex_without_special_values_is_finite()
    {
        var values = Gen.Complex().WithoutSpecialValues().Sample(iterations: 10000, seed: 1);
        values.Should().HaveCount(10000);
        values.Should().OnlyContain(c => double.IsFinite(c.Real) && double.IsFinite(c.Imaginary));
    }
}
