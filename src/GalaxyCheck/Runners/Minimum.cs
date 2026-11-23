using System;

namespace GalaxyCheck
{
    using GalaxyCheck.Runners;

    public static partial class Extensions
    {
        /// <summary>Finds the shrunk value for which the predicate is true. With no predicate, each value counts.</summary>
        public static T Minimum<T>(
            this IGen<T> gen,
            int iterations = 100,
            int? seed = null,
            int? size = null,
            bool deepMinimum = false,
            Func<T, bool>? pred = null) => gen.Advanced.MinimumWithMetrics<T>(
                iterations: iterations,
                seed: seed,
                size: size,
                deepMinimum: deepMinimum,
                pred: pred).Value;

        /// <summary>Finds the shrunk value for which the predicate is true, with the count of shrinks.</summary>
        public static MinimumWithMetrics<T> MinimumWithMetrics<T>(
            this IGenAdvanced<T> advanced,
            int iterations = 100,
            int? seed = null,
            int? size = null,
            bool deepMinimum = false,
            Func<T, bool>? pred = null)
        {
            pred ??= (_) => true;

            var property = advanced.AsGen().ForAll(x =>
            {
                var result = pred(x);
                return result == false;
            });

            // The search for a minimum has no exhaustive run and no health checks.
            var result = CheckRunner.Run(property, iterations, seed, size, null, null, deepMinimum, recordChecks: false);

            if (result.Falsified)
            {
                return new MinimumWithMetrics<T>(result.Counterexample!.Value, result.Shrinks);
            }

            throw new Exceptions.NoMinimalFoundException();
        }
    }
}

namespace GalaxyCheck.Runners
{
    /// <summary>The result of Minimum: the shrunk value and the count of shrinks.</summary>
    /// <param name="Value">The shrunk value.</param>
    /// <param name="Shrinks">The count of shrinks.</param>
    public record MinimumWithMetrics<T>(
        T Value,
        int Shrinks);
}
