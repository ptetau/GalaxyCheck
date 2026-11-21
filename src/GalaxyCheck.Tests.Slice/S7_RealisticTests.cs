using FluentAssertions;
using GalaxyCheck;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S7_RealisticTests
{
    public static IEnumerable<object[]> Generators()
    {
        yield return new object[] { "FirstName", Gen.Realistic.FirstName() };
        yield return new object[] { "LastName", Gen.Realistic.LastName() };
        yield return new object[] { "FullName", Gen.Realistic.FullName() };
        yield return new object[] { "StreetAddress", Gen.Realistic.StreetAddress() };
        yield return new object[] { "City", Gen.Realistic.City() };
        yield return new object[] { "Country", Gen.Realistic.Country() };
        yield return new object[] { "PhoneNumber", Gen.Realistic.PhoneNumber() };
        yield return new object[] { "CompanyName", Gen.Realistic.CompanyName() };
    }

    [Theory]
    [MemberData(nameof(Generators))]
    public void test_001_S7_R59_realistic_values_are_not_empty_and_vary(string name, IGen<string> gen)
    {
        var values = gen.Sample(iterations: 100, seed: 0);

        values.Should().HaveCount(100, because: name);
        values.Should().OnlyContain(v => !string.IsNullOrEmpty(v), because: name);
        values.Distinct().Count().Should().BeGreaterThanOrEqualTo(10, because: name);
    }

    [Fact]
    public void test_001_S7_R59_first_name_failure_shrinks_to_first_list_entry()
    {
        var result = Property.ForAll(Gen.Realistic.FirstName(), (string _) => false).Check(seed: 0);

        result.Falsified.Should().BeTrue();
        result.Counterexample!.Value.Should().Be(Gen.Realistic.FirstNames[0]);
    }

    [Fact]
    public void test_001_S7_R59_full_name_is_first_name_space_last_name()
    {
        var firstNames = Gen.Realistic.FirstNames;
        var lastNames = Gen.Realistic.LastNames;

        var values = Gen.Realistic.FullName().Sample(iterations: 100, seed: 0);

        values.Should().HaveCount(100);
        foreach (var value in values)
        {
            firstNames.Any(f => value.StartsWith(f + " ") && lastNames.Contains(value.Substring(f.Length + 1)))
                .Should().BeTrue(because: value);
        }
    }
}
