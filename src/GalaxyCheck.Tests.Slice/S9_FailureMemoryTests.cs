using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S13_FailureMemoryTests
{
    [Fact]
    public void test_001_S9_R38_xunit_v2_failed_property_saves_replay_under_test_name()
    {
        var phase = S13Support.Memory("xunit2").First;
        var result = phase.One("MemSwitchable");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Failures.Should().NotBeNull($"the file .galaxycheck/failures.json must exist. Output:\n{phase.Output}");
        var key = phase.Key("MemSwitchable");
        key.Should().NotBeNull($"the file must hold the full name of the test. Output:\n{phase.Output}");
        var replay = phase.Failures![key!];
        replay.Should().NotBeNullOrWhiteSpace();
        result.Message.Should().Contain(replay, "the message of the failure holds the same replay string");
    }

    [Fact]
    public void test_001_S9_R38_xunit_v2_next_run_tries_saved_counterexample_first()
    {
        var phase = S13Support.Memory("xunit2").Second;
        phase.One("MemSwitchable").Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemSwitchable").Should().NotBeEmpty(phase.Output);
        phase.Log("MemSwitchable")[0].Should().Be("10", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_xunit_v2_passing_test_removes_saved_entry()
    {
        var phase = S13Support.Memory("xunit2").Third;
        phase.One("MemSwitchable").Outcome.Should().Be("Passed", phase.Output);
        phase.Key("MemSwitchable").Should().BeNull($"the file must no longer hold the entry. Output:\n{phase.Output}");
    }

    [Fact]
    public void test_001_S9_R38_xunit_v2_replay_attribute_takes_precedence_over_saved_entry()
    {
        var phase = S13Support.Memory("xunit2").Fourth;
        var result = phase.One("MemReplayOverride");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemReplayOverride").Should().NotBeEmpty(phase.Output);
        phase.Log("MemReplayOverride")[0].Should().Be("50", phase.Output);
        result.Message.Should().Contain("50", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_xunit_v2_switched_off_failure_memory_writes_no_file()
    {
        var phase = S13Support.Plain("xunit2");
        phase.One("PlainDisabled").Outcome.Should().Be("Failed", phase.Output);
        File.Exists(S13Support.FailureFile(S13Support.Folder("xunit2", "Plain"))).Should().BeFalse(phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_xunit_v3_failed_property_saves_replay_under_test_name()
    {
        var phase = S13Support.Memory("xunit3").First;
        var result = phase.One("MemSwitchable");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Failures.Should().NotBeNull($"the file .galaxycheck/failures.json must exist. Output:\n{phase.Output}");
        var key = phase.Key("MemSwitchable");
        key.Should().NotBeNull($"the file must hold the full name of the test. Output:\n{phase.Output}");
        var replay = phase.Failures![key!];
        replay.Should().NotBeNullOrWhiteSpace();
        result.Message.Should().Contain(replay, "the message of the failure holds the same replay string");
    }

    [Fact]
    public void test_001_S9_R38_xunit_v3_next_run_tries_saved_counterexample_first()
    {
        var phase = S13Support.Memory("xunit3").Second;
        phase.One("MemSwitchable").Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemSwitchable").Should().NotBeEmpty(phase.Output);
        phase.Log("MemSwitchable")[0].Should().Be("10", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_xunit_v3_passing_test_removes_saved_entry()
    {
        var phase = S13Support.Memory("xunit3").Third;
        phase.One("MemSwitchable").Outcome.Should().Be("Passed", phase.Output);
        phase.Key("MemSwitchable").Should().BeNull($"the file must no longer hold the entry. Output:\n{phase.Output}");
    }

    [Fact]
    public void test_001_S9_R38_xunit_v3_replay_attribute_takes_precedence_over_saved_entry()
    {
        var phase = S13Support.Memory("xunit3").Fourth;
        var result = phase.One("MemReplayOverride");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemReplayOverride").Should().NotBeEmpty(phase.Output);
        phase.Log("MemReplayOverride")[0].Should().Be("50", phase.Output);
        result.Message.Should().Contain("50", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_xunit_v3_switched_off_failure_memory_writes_no_file()
    {
        var phase = S13Support.Plain("xunit3");
        phase.One("PlainDisabled").Outcome.Should().Be("Failed", phase.Output);
        File.Exists(S13Support.FailureFile(S13Support.Folder("xunit3", "Plain"))).Should().BeFalse(phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_nunit_failed_property_saves_replay_under_test_name()
    {
        var phase = S13Support.Memory("nunit").First;
        var result = phase.One("MemSwitchable");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Failures.Should().NotBeNull($"the file .galaxycheck/failures.json must exist. Output:\n{phase.Output}");
        var key = phase.Key("MemSwitchable");
        key.Should().NotBeNull($"the file must hold the full name of the test. Output:\n{phase.Output}");
        var replay = phase.Failures![key!];
        replay.Should().NotBeNullOrWhiteSpace();
        result.Message.Should().Contain(replay, "the message of the failure holds the same replay string");
    }

    [Fact]
    public void test_001_S9_R38_nunit_next_run_tries_saved_counterexample_first()
    {
        var phase = S13Support.Memory("nunit").Second;
        phase.One("MemSwitchable").Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemSwitchable").Should().NotBeEmpty(phase.Output);
        phase.Log("MemSwitchable")[0].Should().Be("10", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_nunit_passing_test_removes_saved_entry()
    {
        var phase = S13Support.Memory("nunit").Third;
        phase.One("MemSwitchable").Outcome.Should().Be("Passed", phase.Output);
        phase.Key("MemSwitchable").Should().BeNull($"the file must no longer hold the entry. Output:\n{phase.Output}");
    }

    [Fact]
    public void test_001_S9_R38_nunit_replay_attribute_takes_precedence_over_saved_entry()
    {
        var phase = S13Support.Memory("nunit").Fourth;
        var result = phase.One("MemReplayOverride");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemReplayOverride").Should().NotBeEmpty(phase.Output);
        phase.Log("MemReplayOverride")[0].Should().Be("50", phase.Output);
        result.Message.Should().Contain("50", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_nunit_switched_off_failure_memory_writes_no_file()
    {
        var phase = S13Support.Plain("nunit");
        phase.One("PlainDisabled").Outcome.Should().Be("Failed", phase.Output);
        File.Exists(S13Support.FailureFile(S13Support.Folder("nunit", "Plain"))).Should().BeFalse(phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_mstest_failed_property_saves_replay_under_test_name()
    {
        var phase = S13Support.Memory("mstest").First;
        var result = phase.One("MemSwitchable");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Failures.Should().NotBeNull($"the file .galaxycheck/failures.json must exist. Output:\n{phase.Output}");
        var key = phase.Key("MemSwitchable");
        key.Should().NotBeNull($"the file must hold the full name of the test. Output:\n{phase.Output}");
        var replay = phase.Failures![key!];
        replay.Should().NotBeNullOrWhiteSpace();
        result.Message.Should().Contain(replay, "the message of the failure holds the same replay string");
    }

    [Fact]
    public void test_001_S9_R38_mstest_next_run_tries_saved_counterexample_first()
    {
        var phase = S13Support.Memory("mstest").Second;
        phase.One("MemSwitchable").Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemSwitchable").Should().NotBeEmpty(phase.Output);
        phase.Log("MemSwitchable")[0].Should().Be("10", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_mstest_passing_test_removes_saved_entry()
    {
        var phase = S13Support.Memory("mstest").Third;
        phase.One("MemSwitchable").Outcome.Should().Be("Passed", phase.Output);
        phase.Key("MemSwitchable").Should().BeNull($"the file must no longer hold the entry. Output:\n{phase.Output}");
    }

    [Fact]
    public void test_001_S9_R38_mstest_replay_attribute_takes_precedence_over_saved_entry()
    {
        var phase = S13Support.Memory("mstest").Fourth;
        var result = phase.One("MemReplayOverride");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemReplayOverride").Should().NotBeEmpty(phase.Output);
        phase.Log("MemReplayOverride")[0].Should().Be("50", phase.Output);
        result.Message.Should().Contain("50", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_mstest_switched_off_failure_memory_writes_no_file()
    {
        var phase = S13Support.Plain("mstest");
        phase.One("PlainDisabled").Outcome.Should().Be("Failed", phase.Output);
        File.Exists(S13Support.FailureFile(S13Support.Folder("mstest", "Plain"))).Should().BeFalse(phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_tunit_failed_property_saves_replay_under_test_name()
    {
        var phase = S13Support.Memory("tunit").First;
        var result = phase.One("MemSwitchable");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Failures.Should().NotBeNull($"the file .galaxycheck/failures.json must exist. Output:\n{phase.Output}");
        var key = phase.Key("MemSwitchable");
        key.Should().NotBeNull($"the file must hold the full name of the test. Output:\n{phase.Output}");
        var replay = phase.Failures![key!];
        replay.Should().NotBeNullOrWhiteSpace();
        result.Message.Should().Contain(replay, "the message of the failure holds the same replay string");
    }

    [Fact]
    public void test_001_S9_R38_tunit_next_run_tries_saved_counterexample_first()
    {
        var phase = S13Support.Memory("tunit").Second;
        phase.One("MemSwitchable").Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemSwitchable").Should().NotBeEmpty(phase.Output);
        phase.Log("MemSwitchable")[0].Should().Be("10", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_tunit_passing_test_removes_saved_entry()
    {
        var phase = S13Support.Memory("tunit").Third;
        phase.One("MemSwitchable").Outcome.Should().Be("Passed", phase.Output);
        phase.Key("MemSwitchable").Should().BeNull($"the file must no longer hold the entry. Output:\n{phase.Output}");
    }

    [Fact]
    public void test_001_S9_R38_tunit_replay_attribute_takes_precedence_over_saved_entry()
    {
        var phase = S13Support.Memory("tunit").Fourth;
        var result = phase.One("MemReplayOverride");
        result.Outcome.Should().Be("Failed", phase.Output);
        phase.Log("MemReplayOverride").Should().NotBeEmpty(phase.Output);
        phase.Log("MemReplayOverride")[0].Should().Be("50", phase.Output);
        result.Message.Should().Contain("50", phase.Output);
    }

    [Fact]
    public void test_001_S9_R38_tunit_switched_off_failure_memory_writes_no_file()
    {
        var phase = S13Support.Plain("tunit");
        phase.One("PlainDisabled").Outcome.Should().Be("Failed", phase.Output);
        File.Exists(S13Support.FailureFile(S13Support.Folder("tunit", "Plain"))).Should().BeFalse(phase.Output);
    }
}
