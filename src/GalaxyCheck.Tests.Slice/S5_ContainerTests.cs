using System.Runtime.CompilerServices;
using FluentAssertions;
using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S5_ContainerTests
{
    private static readonly string[] Words = { "a", "b", "c", "d" };

    private static void AssertLength<T>(IGen<T> gen, int expected) where T : ITuple
    {
        var values = gen.Sample(iterations: 10, seed: 0);
        values.Should().HaveCount(10);
        values.Should().OnlyContain(v => v.Length == expected);
    }

    [Fact]
    public void test_001_S5_R14_dictionary_count_is_between_min_and_max()
    {
        var gen = Gen.Dictionary(Gen.Int32(), Gen.Element(Words)).WithCountBetween(2, 5);
        var values = gen.Sample(iterations: 1000, seed: 0);
        values.Should().HaveCount(1000);
        values.Should().OnlyContain(d => d.Count >= 2 && d.Count <= 5);
    }

    [Fact]
    public void test_001_S5_R14_dictionary_exact_count_has_distinct_keys()
    {
        var gen = Gen.Dictionary(Gen.Int32(), Gen.Element(Words)).WithCount(3);
        var values = gen.Sample(iterations: 1000, seed: 0);
        values.Should().HaveCount(1000);
        values.Should().OnlyContain(d => d.Count == 3 && d.Keys.Distinct().Count() == 3);
    }

    [Fact]
    public void test_001_S5_R14_dictionary_failing_property_shrinks_to_keys_0_and_1_with_values_0()
    {
        var gen = Gen.Dictionary(Gen.Int32(), Gen.Int32());
        var result = Property.ForAll(gen, d => d.Count < 2).Check(iterations: 1000, seed: 0);
        result.Falsified.Should().BeTrue();
        var counterexample = (IReadOnlyDictionary<int, int>)(object)result.Counterexample!.Value!;
        counterexample.Should().HaveCount(2);
        counterexample.Keys.Should().BeEquivalentTo(new[] { 0, 1 });
        counterexample.Values.Should().OnlyContain(v => v == 0);
    }

    [Fact]
    public void test_001_S5_R15_keyvaluepair_values_have_the_pair_type()
    {
        var values = Gen.KeyValuePair(Gen.Int32(), Gen.Boolean()).Sample(iterations: 100, seed: 0);
        values.Should().HaveCount(100);
        foreach (var value in values)
        {
            ((object)value).Should().BeOfType<KeyValuePair<int, bool>>();
        }
    }

    [Fact]
    public void test_001_S5_R15_valuetuple_values_have_the_count_of_elements()
    {
        var i = Gen.Int32();
        AssertLength(Gen.ValueTuple(i), 1);
        AssertLength(Gen.ValueTuple(i, i), 2);
        AssertLength(Gen.ValueTuple(i, i, i), 3);
        AssertLength(Gen.ValueTuple(i, i, i, i), 4);
        AssertLength(Gen.ValueTuple(i, i, i, i, i), 5);
        AssertLength(Gen.ValueTuple(i, i, i, i, i, i), 6);
        AssertLength(Gen.ValueTuple(i, i, i, i, i, i, i), 7);
    }

    [Fact]
    public void test_001_S5_R15_tuple_values_have_the_count_of_elements()
    {
        var i = Gen.Int32();
        AssertLength(Gen.Tuple(i), 1);
        AssertLength(Gen.Tuple(i, i), 2);
        AssertLength(Gen.Tuple(i, i, i), 3);
        AssertLength(Gen.Tuple(i, i, i, i), 4);
        AssertLength(Gen.Tuple(i, i, i, i, i), 5);
        AssertLength(Gen.Tuple(i, i, i, i, i, i), 6);
        AssertLength(Gen.Tuple(i, i, i, i, i, i, i), 7);
    }

    [Fact]
    public void test_001_S5_R15_valuetuple_failing_property_shrinks_each_element()
    {
        var gen = Gen.ValueTuple(Gen.Int32(), Gen.Int32());
        var result = Property.ForAll(gen, t => t.Item1 < 10 || t.Item2 < 20).Check(iterations: 1000, seed: 0);
        result.Falsified.Should().BeTrue();
        ((object)result.Counterexample!.Value!).Should().Be((10, 20));
    }
}
