using System;
using System.Threading;
using System.Threading.Tasks;

namespace GalaxyCheck
{
    /// <summary>The checks that fail a property which wastes its test cases.</summary>
    public enum HealthCheck
    {
        TooManyDiscards,
        SlowGenerator,
        TestFunctionNeverRuns
    }

    public class HealthCheckException : Exception
    {
        public HealthCheck Check { get; }

        public HealthCheckException(HealthCheck check, string message) : base(message)
        {
            Check = check;
        }
    }

    public partial class Property
    {
        /// <summary>Discards the test case when the condition is false.</summary>
        public static void Precondition(bool condition) => throw new System.NotImplementedException("not implemented");

        public static AsyncProperty<T0> ForAllAsync<T0>(IGen<T0> gen0, Func<T0, CancellationToken, Task<bool>> func) => throw new System.NotImplementedException("not implemented");

        public static AsyncProperty<T0> ForAllAsync<T0>(IGen<T0> gen0, Func<T0, CancellationToken, Task> action) => throw new System.NotImplementedException("not implemented");
    }
}

namespace GalaxyCheck.Runners
{
    internal static class CheckOptionsStub
    {
        public static void ThrowIfUsed(TimeSpan? timeout, bool exhaustive, HealthCheck[]? suppressHealthChecks)
        {
            if (timeout != null || exhaustive == false || suppressHealthChecks != null)
            {
                throw new System.NotImplementedException("not implemented");
            }
        }
    }
}
