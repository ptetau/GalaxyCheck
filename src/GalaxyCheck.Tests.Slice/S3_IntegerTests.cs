using System.Numerics;
using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S3_IntegerTests
{
    private const int Size = 100;

    private static void AssertExtremesAppear<T>(IGen<T> gen, T min, T max)
    {
        var sawMin = false;
        var sawMax = false;
        for (var seed = 0; seed < 100 && !(sawMin && sawMax); seed++)
        {
            foreach (var value in gen.Sample(iterations: 100, seed: seed, size: Size))
            {
                sawMin |= EqualityComparer<T>.Default.Equals(value, min);
                sawMax |= EqualityComparer<T>.Default.Equals(value, max);
            }
        }

        sawMin.Should().BeTrue($"the minimum {min} must appear in 100 values for a seed from 0 to 99");
        sawMax.Should().BeTrue($"the maximum {max} must appear in 100 values for a seed from 0 to 99");
    }

    [Fact]
    public void test_001_S3_R1_sbyte_covers_full_range() =>
        AssertExtremesAppear(Gen.SByte(), sbyte.MinValue, sbyte.MaxValue);

    [Fact]
    public void test_001_S3_R1_uint16_covers_full_range() =>
        AssertExtremesAppear(Gen.UInt16(), ushort.MinValue, ushort.MaxValue);

    [Fact]
    public void test_001_S3_R1_uint32_covers_full_range() =>
        AssertExtremesAppear(Gen.UInt32(), uint.MinValue, uint.MaxValue);

    [Fact]
    public void test_001_S3_R1_uint64_covers_full_range() =>
        AssertExtremesAppear(Gen.UInt64(), ulong.MinValue, ulong.MaxValue);

    [Fact]
    public void test_001_S3_R1_intptr_covers_full_range() =>
        AssertExtremesAppear(Gen.IntPtr(), nint.MinValue, nint.MaxValue);

    [Fact]
    public void test_001_S3_R1_uintptr_covers_full_range() =>
        AssertExtremesAppear(Gen.UIntPtr(), nuint.MinValue, nuint.MaxValue);

    [Fact]
    public void test_001_S3_R1_int128_covers_full_range() =>
        AssertExtremesAppear(Gen.Int128(), Int128.MinValue, Int128.MaxValue);

    [Fact]
    public void test_001_S3_R1_uint128_covers_full_range() =>
        AssertExtremesAppear(Gen.UInt128(), UInt128.MinValue, UInt128.MaxValue);

    [Fact]
    public void test_001_S3_R2_uint32_between_10_and_20_stays_in_bounds_and_hits_both()
    {
        var values = Gen.UInt32().Between(10u, 20u).Sample(iterations: 1000, seed: 0, size: Size);

        values.Should().HaveCount(1000);
        values.Should().OnlyContain(v => v >= 10u && v <= 20u);
        values.Should().Contain(10u);
        values.Should().Contain(20u);
    }

    [Fact]
    public void test_001_S3_R2_uint16_min_above_max_is_a_generator_error_naming_the_bound()
    {
        var act = () => Gen.UInt16().GreaterThanEqual(5).LessThanEqual(4).Sample(iterations: 10, seed: 0, size: Size);

        act.Should().Throw<GalaxyCheck.Exceptions.GenErrorException>().Where(e => e.Message.Contains("'min' cannot be greater than 'max'"));
    }

    [Fact]
    public void test_001_S3_R3_uint64_below_1000_shrinks_to_1000()
    {
        var result = Property.ForAll(Gen.UInt64(), (ulong x) => x < 1000UL).Check(iterations: 100, seed: 0, size: Size);

        result.Falsified.Should().BeTrue();
        result.Counterexample.Should().NotBeNull();
        ((object)result.Counterexample!.Value).Should().Be(1000UL);
    }

    [Fact]
    public void test_001_S3_R3_sbyte_between_minus_50_and_minus_10_always_failing_shrinks_to_minus_10()
    {
        var result = Property.ForAll(Gen.SByte().Between((sbyte)-50, (sbyte)-10),(sbyte x) => false).Check(iterations: 100, seed: 0, size: Size);

        result.Falsified.Should().BeTrue();
        result.Counterexample.Should().NotBeNull();
        ((object)result.Counterexample!.Value).Should().Be((sbyte)-10);
    }

    [Fact]
    public void test_001_S3_R2_int128_between_minus_5_and_5_stays_in_bounds()
    {
        var values = Gen.Int128().Between(-5, 5).Sample(iterations: 1000, seed: 0, size: Size);

        values.Should().HaveCount(1000);
        values.Should().OnlyContain(v => v >= (Int128)(-5) && v <= (Int128)5);
    }

    [Fact]
    public void test_001_S3_R4_biginteger_goes_below_int64_min_and_above_int64_max()
    {
        var below = false;
        var above = false;
        var gen = Gen.BigInteger();
        for (var seed = 0; seed < 100 && !(below && above); seed++)
        {
            foreach (var value in gen.Sample(iterations: 100, seed: seed, size: Size))
            {
                below |= value < new BigInteger(long.MinValue);
                above |= value > new BigInteger(long.MaxValue);
            }
        }

        below.Should().BeTrue("a value below Int64.MinValue must appear");
        above.Should().BeTrue("a value above Int64.MaxValue must appear");
    }

    [Fact]
    public void test_001_S3_R3_R4_biginteger_below_10_pow_30_shrinks_to_10_pow_30()
    {
        var limit = BigInteger.Pow(10, 30);
        var result = Property.ForAll(Gen.BigInteger(), (BigInteger x) => x < limit).Check(iterations: 100, seed: 0, size: Size);

        result.Falsified.Should().BeTrue();
        result.Counterexample.Should().NotBeNull();
        ((object)result.Counterexample!.Value).Should().Be(limit);
    }
}
