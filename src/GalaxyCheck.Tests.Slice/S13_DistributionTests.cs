using System.Numerics;
using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

/// <summary>
/// The distribution checks of R71. A chi-squared statistic at the 0.001 level: the critical value is 27.877 for
/// 10 buckets (9 degrees of freedom) and 10.828 for 2 buckets (1 degree of freedom).
/// A uniform generator fails the check on one seed with a chance of 0.001. So that no fixed seed can be unlucky,
/// a generator passes when the check passes on at least two of the seeds 0, 1, and 2; a non-uniform generator fails on all.
/// </summary>
public class S13_DistributionTests
{
    private const int Values = 10_000;
    private const double Critical9 = 27.877;
    private const double Critical1 = 10.828;
    private static readonly int[] ChiSeeds = { 0, 1, 2 };

    private static double ChiSquared(int[] counts)
    {
        var expected = (double)counts.Sum() / counts.Length;
        return counts.Sum(o => (o - expected) * (o - expected) / expected);
    }

    private static double Critical(int buckets) => buckets switch
    {
        2 => Critical1,
        10 => Critical9,
        _ => throw new ArgumentOutOfRangeException(nameof(buckets)),
    };

    /// <summary>Gives the statistic of each seed, and adds a problem when a value has no bucket.</summary>
    private static List<double> Statistics<T>(IGen<T> gen, int buckets, Func<T, int> bucketOf, string name, List<string> problems)
    {
        var statistics = new List<double>();
        foreach (var seed in ChiSeeds)
        {
            var counts = new int[buckets];
            foreach (var value in gen.Sample(iterations: Values, seed: seed))
            {
                var bucket = bucketOf(value);
                if (bucket < 0 || bucket >= buckets)
                {
                    problems.Add($"{name}, seed {seed}: the value {S13.Show(value)} is outside the buckets");
                    return statistics;
                }

                counts[bucket]++;
            }

            if (counts.Sum() != Values)
            {
                problems.Add($"{name}, seed {seed}: {counts.Sum()} values, not {Values}");
            }

            statistics.Add(ChiSquared(counts));
        }

        return statistics;
    }

    private static void AssertUniform<T>(IGen<T> gen, int buckets, Func<T, int> bucketOf, string name, List<string> problems)
    {
        try
        {
            var statistics = Statistics(gen, buckets, bucketOf, name, problems);
            var passes = statistics.Count(s => s <= Critical(buckets));
            if (statistics.Count == ChiSeeds.Length && passes < 2)
            {
                problems.Add($"{name}: the chi-squared check failed on {ChiSeeds.Length - passes} of {ChiSeeds.Length} seeds (statistics {string.Join(", ", statistics.Select(s => s.ToString("F2")))}, critical value {Critical(buckets)})");
            }
        }
        catch (Exception e)
        {
            problems.Add($"{name}: {e.GetType().Name}: {e.Message}");
        }
    }

    private static int Bucket<T>(T value) where T : IBinaryInteger<T> =>
        T.IsNegative(value) || value > T.CreateChecked(9) ? -1 : int.CreateChecked(value);

    [Fact]
    public void test_001_S13_R71_boolean_passes_chi_squared()
    {
        var problems = new List<string>();
        AssertUniform(Gen.Boolean(), 2, b => b ? 1 : 0, "Boolean", problems);
        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    [Fact]
    public void test_001_S13_R71_element_of_10_values_passes_chi_squared()
    {
        var problems = new List<string>();
        var values = new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };
        AssertUniform(Gen.Element(values), 10, v => Array.IndexOf(values, v), "Element", problems);
        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    [Fact]
    public void test_001_S13_R71_int32_between_0_and_9_with_no_bias_passes_chi_squared()
    {
        var problems = new List<string>();
        AssertUniform(Gen.Int32().Between(0, 9).WithBias(Gen.Bias.None), 10, Bucket, "Int32", problems);
        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    [Fact]
    public void test_001_S13_R71_each_bounded_integer_generator_with_no_bias_passes_chi_squared()
    {
        var problems = new List<string>();
        AssertUniform(Gen.Byte().Between((byte)0, (byte)9).WithBias(Gen.Bias.None), 10, Bucket, "Byte", problems);
        AssertUniform(Gen.SByte().Between((sbyte)0, (sbyte)9).WithBias(Gen.Bias.None), 10, Bucket, "SByte", problems);
        AssertUniform(Gen.Int16().Between((short)0, (short)9).WithBias(Gen.Bias.None), 10, Bucket, "Int16", problems);
        AssertUniform(Gen.UInt16().Between((ushort)0, (ushort)9).WithBias(Gen.Bias.None), 10, Bucket, "UInt16", problems);
        AssertUniform(Gen.UInt32().Between(0u, 9u).WithBias(Gen.Bias.None), 10, Bucket, "UInt32", problems);
        AssertUniform(Gen.Int64().Between(0L, 9L).WithBias(Gen.Bias.None), 10, Bucket, "Int64", problems);
        AssertUniform(Gen.UInt64().Between(0UL, 9UL).WithBias(Gen.Bias.None), 10, Bucket, "UInt64", problems);
        AssertUniform(Gen.IntPtr().Between((nint)0, (nint)9).WithBias(Gen.Bias.None), 10, Bucket, "IntPtr", problems);
        AssertUniform(Gen.UIntPtr().Between((nuint)0, (nuint)9).WithBias(Gen.Bias.None), 10, Bucket, "UIntPtr", problems);
        AssertUniform(Gen.Int128().Between((Int128)0, (Int128)9).WithBias(Gen.Bias.None), 10, Bucket, "Int128", problems);
        AssertUniform(Gen.UInt128().Between((UInt128)0, (UInt128)9).WithBias(Gen.Bias.None), 10, Bucket, "UInt128", problems);
        AssertUniform(Gen.BigInteger().Between(BigInteger.Zero, new BigInteger(9)).WithBias(Gen.Bias.None), 10, Bucket, "BigInteger", problems);
        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    [Fact]
    public void test_001_S13_R71_chi_squared_fails_for_a_generator_that_never_gives_9()
    {
        var problems = new List<string>();
        var statistics = Statistics(Gen.Int32().Between(0, 8).WithBias(Gen.Bias.None), 10, Bucket, "Int32 0 to 8", problems);
        Assert.True(problems.Count == 0, S13.Problems(problems));
        Assert.Equal(ChiSeeds.Length, statistics.Count);
        Assert.True(statistics.All(s => s > Critical9), $"the check must fail on each seed: statistics {string.Join(", ", statistics)}");
    }

    private static void MinAndMax<T>(IGen<T> gen, T min, T max, string name, List<string> problems)
    {
        foreach (var seed in S13.Seeds)
        {
            try
            {
                var values = gen.Sample(iterations: 2000, seed: seed, size: 100);
                var hasMin = values.Any(v => EqualityComparer<T>.Default.Equals(v, min));
                var hasMax = values.Any(v => EqualityComparer<T>.Default.Equals(v, max));
                if (!hasMin || !hasMax)
                {
                    problems.Add($"{name}, seed {seed}: {(hasMin ? "" : $"no minimum {min} ")}{(hasMax ? "" : $"no maximum {max}")}in 2,000 values");
                }
            }
            catch (Exception e)
            {
                problems.Add($"{name}, seed {seed}: {e.GetType().Name}: {e.Message}");
            }
        }
    }

    [Fact]
    public void test_001_S13_R71_each_new_integer_generator_gives_its_minimum_and_maximum_in_2000_values()
    {
        var problems = new List<string>();
        MinAndMax(Gen.SByte(), sbyte.MinValue, sbyte.MaxValue, "SByte", problems);
        MinAndMax(Gen.UInt16(), ushort.MinValue, ushort.MaxValue, "UInt16", problems);
        MinAndMax(Gen.UInt32(), uint.MinValue, uint.MaxValue, "UInt32", problems);
        MinAndMax(Gen.UInt64(), ulong.MinValue, ulong.MaxValue, "UInt64", problems);
        MinAndMax(Gen.IntPtr(), nint.MinValue, nint.MaxValue, "IntPtr", problems);
        MinAndMax(Gen.UIntPtr(), nuint.MinValue, nuint.MaxValue, "UIntPtr", problems);
        MinAndMax(Gen.Int128(), Int128.MinValue, Int128.MaxValue, "Int128", problems);
        MinAndMax(Gen.UInt128(), UInt128.MinValue, UInt128.MaxValue, "UInt128", problems);
        MinAndMax(Gen.BigInteger(), -BigInteger.Pow(2, 127), BigInteger.Pow(2, 127), "BigInteger", problems);
        Assert.True(problems.Count == 0, S13.Problems(problems));
    }

    [Fact]
    public void test_001_S13_R71_each_existing_integer_generator_gives_its_minimum_and_maximum_in_2000_values()
    {
        var problems = new List<string>();
        MinAndMax(Gen.Byte(), byte.MinValue, byte.MaxValue, "Byte", problems);
        MinAndMax(Gen.Int16(), short.MinValue, short.MaxValue, "Int16", problems);
        MinAndMax(Gen.Int32(), int.MinValue, int.MaxValue, "Int32", problems);
        MinAndMax(Gen.Int64(), long.MinValue, long.MaxValue, "Int64", problems);
        Assert.True(problems.Count == 0, S13.Problems(problems));
    }
}
