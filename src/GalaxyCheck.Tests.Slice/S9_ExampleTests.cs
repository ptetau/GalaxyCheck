using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S13_ExampleTests
{
    [Fact]
    public void test_001_S9_R52_xunit_v2_examples_run_first_then_generated_cases()
    {
        var phase = S13Support.Plain("xunit2");
        phase.One("ExOrder").Outcome.Should().Be("Passed", phase.Output);
        var log = phase.Log("ExOrder");
        log.Length.Should().BeGreaterThan(2, phase.Output);
        log[0].Should().Be("1,2", phase.Output);
        log[1].Should().Be("3,4", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_xunit_v2_failing_example_fails_with_inputs_and_is_not_shrunk()
    {
        var phase = S13Support.Plain("xunit2");
        var result = phase.One("ExFails");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("3", phase.Output).And.Contain("4", phase.Output);
        phase.Log("ExFails").Should().Equal(new[] { "3,4" }, phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_xunit_v2_example_with_missing_input_names_second_parameter()
    {
        var phase = S13Support.Plain("xunit2");
        var result = phase.One("ExMissing");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("second", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_xunit_v2_example_with_wrong_type_names_parameter()
    {
        var phase = S13Support.Plain("xunit2");
        var result = phase.One("ExWrongType");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("number", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_xunit_v3_examples_run_first_then_generated_cases()
    {
        var phase = S13Support.Plain("xunit3");
        phase.One("ExOrder").Outcome.Should().Be("Passed", phase.Output);
        var log = phase.Log("ExOrder");
        log.Length.Should().BeGreaterThan(2, phase.Output);
        log[0].Should().Be("1,2", phase.Output);
        log[1].Should().Be("3,4", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_xunit_v3_failing_example_fails_with_inputs_and_is_not_shrunk()
    {
        var phase = S13Support.Plain("xunit3");
        var result = phase.One("ExFails");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("3", phase.Output).And.Contain("4", phase.Output);
        phase.Log("ExFails").Should().Equal(new[] { "3,4" }, phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_xunit_v3_example_with_missing_input_names_second_parameter()
    {
        var phase = S13Support.Plain("xunit3");
        var result = phase.One("ExMissing");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("second", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_xunit_v3_example_with_wrong_type_names_parameter()
    {
        var phase = S13Support.Plain("xunit3");
        var result = phase.One("ExWrongType");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("number", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_nunit_examples_run_first_then_generated_cases()
    {
        var phase = S13Support.Plain("nunit");
        phase.One("ExOrder").Outcome.Should().Be("Passed", phase.Output);
        var log = phase.Log("ExOrder");
        log.Length.Should().BeGreaterThan(2, phase.Output);
        log[0].Should().Be("1,2", phase.Output);
        log[1].Should().Be("3,4", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_nunit_failing_example_fails_with_inputs_and_is_not_shrunk()
    {
        var phase = S13Support.Plain("nunit");
        var result = phase.One("ExFails");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("3", phase.Output).And.Contain("4", phase.Output);
        phase.Log("ExFails").Should().Equal(new[] { "3,4" }, phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_nunit_example_with_missing_input_names_second_parameter()
    {
        var phase = S13Support.Plain("nunit");
        var result = phase.One("ExMissing");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("second", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_nunit_example_with_wrong_type_names_parameter()
    {
        var phase = S13Support.Plain("nunit");
        var result = phase.One("ExWrongType");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("number", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_mstest_examples_run_first_then_generated_cases()
    {
        var phase = S13Support.Plain("mstest");
        phase.One("ExOrder").Outcome.Should().Be("Passed", phase.Output);
        var log = phase.Log("ExOrder");
        log.Length.Should().BeGreaterThan(2, phase.Output);
        log[0].Should().Be("1,2", phase.Output);
        log[1].Should().Be("3,4", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_mstest_failing_example_fails_with_inputs_and_is_not_shrunk()
    {
        var phase = S13Support.Plain("mstest");
        var result = phase.One("ExFails");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("3", phase.Output).And.Contain("4", phase.Output);
        phase.Log("ExFails").Should().Equal(new[] { "3,4" }, phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_mstest_example_with_missing_input_names_second_parameter()
    {
        var phase = S13Support.Plain("mstest");
        var result = phase.One("ExMissing");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("second", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_mstest_example_with_wrong_type_names_parameter()
    {
        var phase = S13Support.Plain("mstest");
        var result = phase.One("ExWrongType");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("number", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_tunit_examples_run_first_then_generated_cases()
    {
        var phase = S13Support.Plain("tunit");
        phase.One("ExOrder").Outcome.Should().Be("Passed", phase.Output);
        var log = phase.Log("ExOrder");
        log.Length.Should().BeGreaterThan(2, phase.Output);
        log[0].Should().Be("1,2", phase.Output);
        log[1].Should().Be("3,4", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_tunit_failing_example_fails_with_inputs_and_is_not_shrunk()
    {
        var phase = S13Support.Plain("tunit");
        var result = phase.One("ExFails");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("3", phase.Output).And.Contain("4", phase.Output);
        phase.Log("ExFails").Should().Equal(new[] { "3,4" }, phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_tunit_example_with_missing_input_names_second_parameter()
    {
        var phase = S13Support.Plain("tunit");
        var result = phase.One("ExMissing");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("second", phase.Output);
    }

    [Fact]
    public void test_001_S9_R52_tunit_example_with_wrong_type_names_parameter()
    {
        var phase = S13Support.Plain("tunit");
        var result = phase.One("ExWrongType");
        result.Outcome.Should().Be("Failed", phase.Output);
        result.Message.Should().Contain("number", phase.Output);
    }
}
