using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using FluentAssertions;
using GalaxyCheck;
using GalaxyCheck.Gens;
using GalaxyCheck.Runners;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;

namespace GalaxyCheck.Tests.Slice.S8Samples
{
    public record Point(int X, int Y);

    public enum Three { One, Two, Three }
}

namespace GalaxyCheck.Tests.Slice
{
    using GalaxyCheck.Tests.Slice.S8Samples;

    public class S8_PropertyFeaturesTests
    {
        private static readonly Regex EvenShare = new(@"\d+(\.\d+)?\s*%\s*even");

        private static string FailureMessage(Action assert)
        {
            try
            {
                assert();
            }
            catch (PropertyFailedException e)
            {
                return e.Message;
            }

            throw new Xunit.Sdk.XunitException("The property did not fail");
        }

        private static async Task<string> FailureMessageAsync(Func<Task> assert)
        {
            try
            {
                await assert();
            }
            catch (PropertyFailedException e)
            {
                return e.Message;
            }

            throw new Xunit.Sdk.XunitException("The property did not fail");
        }

        // R36: labels

        [Fact]
        public void test_001_S6_R36_passed_report_lists_label_share()
        {
            var property = Property.ForAll(Gen.Int32().Between(0, 9), x => true)
                .Label("even", x => x % 2 == 0);

            var result = property.Check(iterations: 100, seed: 1);

            result.Falsified.Should().BeFalse();
            result.Labels.Should().ContainKey("even");
            result.Labels["even"].Should().BeInRange(0, 100);
            result.Report.Should().MatchRegex(EvenShare.ToString());
        }

        [Fact]
        public void test_001_S6_R36_failure_report_lists_label_share()
        {
            var property = Property.ForAll(Gen.Int32().Between(0, 9), x => x < 5)
                .Label("even", x => x % 2 == 0);

            var message = FailureMessage(() => property.Assert(iterations: 100, seed: 1));

            message.Should().MatchRegex(EvenShare.ToString());
        }

        // R37: cover

        [Fact]
        public void test_001_S6_R37_unmet_cover_fails_with_label_required_and_actual_share()
        {
            // 100 of the 1000 values (10%) have the label. The domain is larger than the iterations.
            // The generator has no bias, so that the share does not depend on the size of each test case.
            var property = Property.ForAll(Gen.Int32().Between(1, 1000).WithBias(Gen.Bias.None), x => true)
                .Cover("rare", 50, x => x <= 100);

            var result = property.Check(iterations: 100, seed: 1);
            result.Falsified.Should().BeTrue();

            var message = FailureMessage(() => property.Assert(iterations: 100, seed: 1));
            message.Should().Contain("rare");
            message.Should().Contain("50%");
            Regex.Matches(message, @"\d+(\.\d+)?%").Select(m => m.Value)
                .Should().Contain(v => v != "50%", "the message must also hold the actual share");
        }

        [Fact]
        public void test_001_S6_R37_met_cover_passes()
        {
            // 500 of the 1000 values (50%) have the label, and the rule asks for 5%.
            var property = Property.ForAll(Gen.Int32().Between(1, 1000), x => true)
                .Cover("half", 5, x => x <= 500);

            var result = property.Check(iterations: 100, seed: 1);

            result.Falsified.Should().BeFalse(result.Report);
        }

        // R55: exhaustive small domains

        [Fact]
        public void test_001_S6_R55_bool_and_enum_run_6_times_exhaustively()
        {
            var seen = new List<(bool, Three)>();
            var gen = Gen.ValueTuple(Gen.Boolean(), Gen.Enum<Three>());
            var property = Property.ForAll(gen, t =>
            {
                seen.Add((t.Item1, t.Item2));
                return true;
            });

            var result = property.Check(iterations: 100, seed: 1);

            seen.Should().HaveCount(6);
            seen.Distinct().Should().HaveCount(6);
            result.Exhaustive.Should().BeTrue();
            result.Report.Should().Contain("Exhaustive");
        }

        [Fact]
        public void test_001_S6_R55_each_value_from_1_to_10_runs_one_time()
        {
            var seen = new List<int>();
            var property = Property.ForAll(Gen.Int32().Between(1, 10), x =>
            {
                seen.Add(x);
                return true;
            });

            property.Check(iterations: 100, seed: 1);

            seen.OrderBy(x => x).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public void test_001_S6_R55_large_domain_runs_100_times_and_is_not_exhaustive()
        {
            var runs = 0;
            var property = Property.ForAll(Gen.Int32().Between(1, 1000), x =>
            {
                runs++;
                return true;
            });

            var result = property.Check(iterations: 100, seed: 1);

            runs.Should().Be(100);
            result.Exhaustive.Should().BeFalse();
            result.Report.Should().NotContainEquivalentOf("exhaustive");
        }

        [Fact]
        public void test_001_S6_R55_option_off_runs_100_times()
        {
            var runs = 0;
            var property = Property.ForAll(Gen.ValueTuple(Gen.Boolean(), Gen.Enum<Three>()), t =>
            {
                runs++;
                return true;
            });

            var result = property.Check(iterations: 100, seed: 1, exhaustive: false);

            runs.Should().Be(100);
            result.Exhaustive.Should().BeFalse();
        }

        [Fact]
        public void test_001_S6_R55_option_off_runs_100_times_for_1_to_10()
        {
            var runs = 0;
            var property = Property.ForAll(Gen.Int32().Between(1, 10), x =>
            {
                runs++;
                return true;
            });

            property.Check(iterations: 100, seed: 1, exhaustive: false);

            runs.Should().Be(100);
        }

        // R56: health checks

        private static Property<int> DiscardingProperty() =>
            Property.ForAll(Gen.Int32().Between(0, 999).Where(x => x < 50), x => true);

        private static Property<int> SlowProperty() =>
            Property.ForAll(Gen.Int32().Select(x =>
            {
                Thread.Sleep(2000);
                return x;
            }), x => true);

        [Fact]
        public void test_001_S6_R56_discard_check_names_check_and_suppression()
        {
            var act = () => DiscardingProperty().Check(iterations: 100, seed: 1);

            var thrown = act.Should().Throw<HealthCheckException>().Which;
            thrown.Check.Should().Be(HealthCheck.TooManyDiscards);
            thrown.Message.Should().Contain("TooManyDiscards").And.Contain("suppressHealthChecks");
        }

        [Fact]
        public void test_001_S6_R56_slow_generator_check_names_check()
        {
            var act = () => SlowProperty().Check(iterations: 3, seed: 1);

            var thrown = act.Should().Throw<HealthCheckException>().Which;
            thrown.Check.Should().Be(HealthCheck.SlowGenerator);
            thrown.Message.Should().Contain("SlowGenerator").And.Contain("suppressHealthChecks");
        }

        [Fact]
        public void test_001_S6_R56_suppressed_discard_check_does_not_fail()
        {
            var act = () => DiscardingProperty().Check(
                iterations: 100, seed: 1, suppressHealthChecks: new[] { HealthCheck.TooManyDiscards });

            act.Should().NotThrow<HealthCheckException>();
        }

        [Fact]
        public void test_001_S6_R56_suppressed_slow_generator_check_does_not_fail()
        {
            var act = () => SlowProperty().Check(
                iterations: 3, seed: 1, suppressHealthChecks: new[] { HealthCheck.SlowGenerator });

            act.Should().NotThrow<HealthCheckException>();
        }

        // R39: copyable counterexamples

        private static readonly ScriptOptions ScriptOptions = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,
                typeof(List<>).Assembly,
                typeof(Enumerable).Assembly,
                typeof(Point).Assembly)
            .WithImports("System", "System.Collections.Generic", "System.Linq", "GalaxyCheck.Tests.Slice.S8Samples");

        private static object? Evaluate(string expression) =>
            CSharpScript.EvaluateAsync<object?>(expression, ScriptOptions).GetAwaiter().GetResult();

        private static string ExpressionOf(object value)
        {
            CSharpExpression.TryFormat(value, out var expression).Should().BeTrue();
            return expression;
        }

        private const string Section = "Counterexample as C#:";

        [Fact]
        public void test_001_S6_R39_double_counterexample_has_compiling_expression()
        {
            var property = Property.ForAll(Gen.Element(new[] { 1.5 }), x => false);

            var message = FailureMessage(() => property.Assert(iterations: 10, seed: 1));

            var expression = ExpressionOf(1.5);
            message.Should().Contain(Section).And.Contain(expression);
            Evaluate(expression).Should().Be(1.5);
        }

        [Fact]
        public void test_001_S6_R39_string_counterexample_has_compiling_expression()
        {
            var value = "a\"b";
            var property = Property.ForAll(Gen.Element(new[] { value }), x => false);

            var message = FailureMessage(() => property.Assert(iterations: 10, seed: 1));

            var expression = ExpressionOf(value);
            message.Should().Contain(Section).And.Contain(expression);
            Evaluate(expression).Should().Be(value);
        }

        [Fact]
        public void test_001_S6_R39_list_counterexample_has_compiling_expression()
        {
            var value = new List<int> { 1, 2 };
            var property = Property.ForAll(Gen.Element(new[] { value }), x => false);

            var message = FailureMessage(() => property.Assert(iterations: 10, seed: 1));

            var expression = ExpressionOf(value);
            message.Should().Contain(Section).And.Contain(expression);
            ((IEnumerable)Evaluate(expression)!).Cast<object>().Select(Convert.ToInt32).Should().Equal(1, 2);
        }

        [Fact]
        public void test_001_S6_R39_record_counterexample_has_compiling_expression()
        {
            var value = new Point(1, 2);
            var property = Property.ForAll(Gen.Element(new[] { value }), x => false);

            var message = FailureMessage(() => property.Assert(iterations: 10, seed: 1));

            var expression = ExpressionOf(value);
            message.Should().Contain(Section).And.Contain(expression);
            Evaluate(expression).Should().Be(value);
        }

        [Fact]
        public void test_001_S6_R39_string_with_code_is_escaped_and_round_trips()
        {
            var value = "\"+System.Environment.GetEnvironmentVariable(\"PATH\")+\"\\\r\n\t";

            var expression = ExpressionOf(value);

            Evaluate(expression).Should().Be(value);
        }

        [Fact]
        public void test_001_S6_R39_function_counterexample_keeps_old_text()
        {
            Func<int, int> function = x => x;
            var property = Property.ForAll(Gen.Element(new[] { function }), f => false);

            CSharpExpression.TryFormat(function, out _).Should().BeFalse();
            var message = FailureMessage(() => property.Assert(iterations: 10, seed: 1));

            message.Should().Contain("Func");
        }

        // R40: timeout

        [Fact]
        public void test_001_S6_R40_sync_function_over_limit_fails_fast_with_limit_and_input()
        {
            var property = Property.ForAll(Gen.Element(new[] { 7 }), x =>
            {
                Thread.Sleep(5000);
                return true;
            });

            var watch = Stopwatch.StartNew();
            var result = property.Check(iterations: 10, seed: 1, timeout: TimeSpan.FromMilliseconds(100));
            watch.Stop();
            result.Falsified.Should().BeTrue();
            watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));

            watch.Restart();
            var message = FailureMessage(() =>
                property.Assert(iterations: 10, seed: 1, timeout: TimeSpan.FromMilliseconds(100)));
            watch.Stop();
            watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
            message.Should().Contain("Timed out after 100 ms").And.Contain("7");
        }

        [Fact]
        public async Task test_001_S6_R40_async_function_over_limit_fails_fast_with_limit_and_input()
        {
            var property = Property.ForAllAsync(Gen.Element(new[] { 7 }), async x =>
            {
                await Task.Delay(5000);
                return true;
            });

            var watch = Stopwatch.StartNew();
            var result = await property.CheckAsync(iterations: 10, seed: 1, timeout: TimeSpan.FromMilliseconds(100));
            watch.Stop();
            result.Falsified.Should().BeTrue();
            watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));

            watch.Restart();
            var message = await FailureMessageAsync(() =>
                property.AssertAsync(iterations: 10, seed: 1, timeout: TimeSpan.FromMilliseconds(100)));
            watch.Stop();
            watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
            message.Should().Contain("Timed out after 100 ms").And.Contain("7");
        }

        private static readonly Lazy<(IReadOnlyList<TrxResult> Results, CliResult Run)> XunitSample = new(() =>
            Trx.RunProject(Path.Combine(Repo.Samples, "S8", "XunitTimeout"), "xunit2"));

        [Fact]
        public void test_001_S6_R40_xunit_property_attribute_over_limit_fails_with_same_message()
        {
            var (results, run) = XunitSample.Value;

            var result = results.SingleOrDefault(r => r.TestName.Contains("OverTheLimit"));
            result.Should().NotBeNull("the sample run must report the test. Output:\n" + run.Output);
            result!.Outcome.Should().Be("Failed", run.Output);
            result.Message.Should().Contain("Timed out after 100 ms", run.Output);
        }

        // R41: cancellation

        [Fact]
        public async Task test_001_S6_R41_token_is_not_generated_and_not_in_counterexample()
        {
            var tokens = new List<CancellationToken>();
            var property = Property.ForAllAsync(Gen.Element(new[] { 42 }), async (x, cancellationToken) =>
            {
                tokens.Add(cancellationToken);
                await Task.Yield();
                return false;
            });

            var result = await property.CheckAsync(iterations: 10, seed: 1);
            var message = await FailureMessageAsync(() => property.AssertAsync(iterations: 10, seed: 1));

            result.Falsified.Should().BeTrue();
            result.Counterexample!.Value.Should().Be(42);
            tokens.Should().NotBeEmpty();
            message.Should().Contain("42");
            message.Should().NotContain("CancellationToken");
        }

        [Fact]
        public void test_001_S6_R41_xunit_property_with_token_parameter_generates_only_the_int()
        {
            var (results, run) = XunitSample.Value;

            var result = results.SingleOrDefault(r => r.TestName.Contains("WithTokenParameter"));
            result.Should().NotBeNull("the sample run must report the test. Output:\n" + run.Output);
            result!.Outcome.Should().Be("Failed", run.Output);
            result.Message.Should().NotContain("CancellationToken", run.Output);
        }

        [Fact]
        public async Task test_001_S6_R41_token_is_cancelled_when_the_limit_passes()
        {
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var property = Property.ForAllAsync(Gen.Element(new[] { 1 }), async (x, cancellationToken) =>
            {
                try
                {
                    await Task.Delay(5000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancelled.TrySetResult(true);
                    throw;
                }

                return true;
            });

            var result = await property.CheckAsync(iterations: 10, seed: 1, timeout: TimeSpan.FromMilliseconds(100));

            result.Falsified.Should().BeTrue();
            var finished = await Task.WhenAny(cancelled.Task, Task.Delay(2000));
            finished.Should().BeSameAs(cancelled.Task, "the token must be cancelled when the limit passes");
        }
    }
}
