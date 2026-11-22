using FluentAssertions;
using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S5_TimeTests
{
    private static List<T> SampleSeeds<T>(IGen<T> gen)
    {
        var values = new List<T>();
        for (var seed = 0; seed < 100; seed++)
        {
            values.AddRange(gen.Sample(iterations: 100, seed: seed));
        }

        return values;
    }

    private static object? FailingCounterexample<T>(IGen<T> gen)
    {
        var result = Property.ForAll(gen, x => false).Check(iterations: 100, seed: 0);
        result.Falsified.Should().BeTrue();
        return result.Counterexample!.Value;
    }

    [Fact]
    public void test_001_S4_R11_timespan_min_and_max_appear()
    {
        var values = SampleSeeds(Gen.TimeSpan());
        values.Should().Contain(TimeSpan.MinValue);
        values.Should().Contain(TimeSpan.MaxValue);
    }

    [Fact]
    public void test_001_S4_R11_dateonly_min_and_max_appear()
    {
        var values = SampleSeeds(Gen.DateOnly());
        values.Should().Contain(DateOnly.MinValue);
        values.Should().Contain(DateOnly.MaxValue);
    }

    [Fact]
    public void test_001_S4_R11_timeonly_min_and_max_appear()
    {
        var values = SampleSeeds(Gen.TimeOnly());
        values.Should().Contain(TimeOnly.MinValue);
        values.Should().Contain(TimeOnly.MaxValue);
    }

    [Fact]
    public void test_001_S4_R11_dateonly_within_bounds_stays_in_year_2020()
    {
        var gen = Gen.DateOnly().Within(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31));
        var values = gen.Sample(iterations: 1000, seed: 0);
        values.Should().HaveCount(1000);
        values.Should().OnlyContain(d => d.Year == 2020);
    }

    [Fact]
    public void test_001_S4_R11_datetimeoffset_has_positive_and_negative_offsets()
    {
        var values = Gen.DateTimeOffset().Sample(iterations: 1000, seed: 0);
        values.Should().Contain(d => d.Offset > TimeSpan.Zero);
        values.Should().Contain(d => d.Offset < TimeSpan.Zero);
    }

    [Fact]
    public void test_001_S4_R11_timespan_failing_property_shrinks_to_origin()
    {
        FailingCounterexample(Gen.TimeSpan()).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void test_001_S4_R11_datetimeoffset_failing_property_shrinks_to_origin()
    {
        var expected = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        FailingCounterexample(Gen.DateTimeOffset()).Should().Be(expected);
    }

    [Fact]
    public void test_001_S4_R11_dateonly_failing_property_shrinks_to_origin()
    {
        FailingCounterexample(Gen.DateOnly()).Should().Be(new DateOnly(2000, 1, 1));
    }

    [Fact]
    public void test_001_S4_R11_timeonly_failing_property_shrinks_to_origin()
    {
        FailingCounterexample(Gen.TimeOnly()).Should().Be(new TimeOnly(0, 0));
    }
}
