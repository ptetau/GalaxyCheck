using System.Text.RegularExpressions;
using FluentAssertions;
using GalaxyCheck;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S7_TextTests
{
    private const string Pattern = @"[a-c]{2,4}-\d{3}";

    [Fact]
    public void test_001_S5_R34_pattern_values_match_whole_pattern()
    {
        var regex = new Regex(@"^(?:" + Pattern + @")\z", RegexOptions.ECMAScript);

        var values = Gen.StringMatching(Pattern).Sample(iterations: 1000, seed: 0);

        values.Should().HaveCount(1000);
        values.Should().OnlyContain(v => regex.IsMatch(v));
    }

    [Fact]
    public void test_001_S5_R34_pattern_failure_shrinks_to_aa_000()
    {
        var result = Property.ForAll(Gen.StringMatching(Pattern), (string _) => false).Check(seed: 0);

        result.Falsified.Should().BeTrue();
        result.Counterexample!.Value.Should().Be("aa-000");
    }

    [Fact]
    public void test_001_S5_R34_lookbehind_gives_generator_error()
    {
        var act = () => Gen.StringMatching("(?<=a)b").Sample(iterations: 10, seed: 0);

        act.Should().Throw<GalaxyCheck.Exceptions.GenErrorException>().Which.Message.ToLowerInvariant().Should().Contain("lookbehind");
    }

    [Fact]
    public void test_001_S5_R35_uri_values_are_absolute_http_or_https_and_different()
    {
        var values = Gen.Uri().Sample(iterations: 100, seed: 0);

        values.Should().HaveCount(100);
        values.Should().OnlyContain(u => u.IsAbsoluteUri);
        values.Should().OnlyContain(u => u.Scheme == "http" || u.Scheme == "https");
        values.Select(u => u.Scheme).Should().Contain("http").And.Contain("https");
        values.Select(u => u.ToString()).Distinct().Count().Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public void test_001_S5_R35_version_has_two_three_and_four_parts()
    {
        var values = Gen.Version().Sample(iterations: 1000, seed: 0);

        var parts = values.Select(v => v.ToString().Split('.').Length).ToHashSet();
        parts.Should().Contain(2);
        parts.Should().Contain(3);
        parts.Should().Contain(4);
    }

    [Fact]
    public void test_001_S5_R35_email_has_one_at_local_part_and_domain_with_dot()
    {
        var values = Gen.Email().Sample(iterations: 1000, seed: 0);

        values.Should().HaveCount(1000);
        foreach (var value in values)
        {
            value.Count(c => c == '@').Should().Be(1, because: value);
            var at = value.IndexOf('@');
            value.Substring(0, at).Should().NotBeEmpty(because: value);
            value.Substring(at + 1).Should().Contain(".", because: value);
        }
    }

    [Fact]
    public void test_001_S5_R35_create_uri_equals_explicit_uri_generator()
    {
        var created = Gen.Create<Uri>().Sample(iterations: 100, seed: 0);
        var explicitly = Gen.Uri().Sample(iterations: 100, seed: 0);

        created.Should().Equal(explicitly);
    }

    [Fact]
    public void test_001_S5_R35_create_version_equals_explicit_version_generator()
    {
        var created = Gen.Create<Version>().Sample(iterations: 100, seed: 0);
        var explicitly = Gen.Version().Sample(iterations: 100, seed: 0);

        created.Should().Equal(explicitly);
    }
}
