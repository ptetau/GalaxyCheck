using GalaxyCheck.Properties;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GalaxyCheck
{
    /// <summary>The checks that fail a property which wastes its test cases.</summary>
    public enum HealthCheck
    {
        /// <summary>More than 90% of the generated values were discarded.</summary>
        TooManyDiscards,

        /// <summary>One value took longer than one second to generate.</summary>
        SlowGenerator,

        /// <summary>No test case reached the test function.</summary>
        TestFunctionNeverRuns
    }

    /// <summary>Thrown by a check or an assert when a health check trips.</summary>
    public class HealthCheckException : Exception
    {
        /// <summary>The check that tripped.</summary>
        public HealthCheck Check { get; }

        /// <summary>Makes the exception of the check.</summary>
        public HealthCheckException(HealthCheck check, string message) : base(message)
        {
            Check = check;
        }
    }

    public partial class Property
    {
        /// <summary>Discards the test case when the condition is false.</summary>
        public static void Precondition(bool condition)
        {
            if (!condition)
            {
                throw PreconditionFailedException.Instance;
            }
        }

        /// <summary>
        /// A property with a test function that takes a token. The token is cancelled when the time limit of the
        /// test case passes. The token is not generated.
        /// </summary>
        public static AsyncProperty<T0> ForAllAsync<T0>(IGen<T0> gen0, Func<T0, CancellationToken, Task<bool>> func) =>
            new AsyncProperty<T0>(
                gen0.Select(x => TestFactory.Create(
                    x,
                    () => new ValueTask<bool>(func(x, TestContext.Token)),
                    new object?[] { x })));

        public static AsyncProperty<T0> ForAllAsync<T0>(IGen<T0> gen0, Func<T0, CancellationToken, Task> action) =>
            ForAllAsync(gen0, async (T0 x, CancellationToken token) =>
            {
                await action(x, token);
                return true;
            });
    }
}

namespace GalaxyCheck.Runners
{
    internal static class HealthMessages
    {
        public static HealthCheckException Exception(HealthCheck check, string problem) => new HealthCheckException(
            check,
            $"Health check {check} failed: {problem} To switch this check off, pass suppressHealthChecks: new[] {{ HealthCheck.{check} }}.");
    }
}
