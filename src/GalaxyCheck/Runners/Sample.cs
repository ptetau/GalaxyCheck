using Verifiable.Contracts;
using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using GalaxyCheck.Runners;
using System;
using System.Collections.Generic;

namespace GalaxyCheck
{
    using GalaxyCheck.Runners.Sample;

    public static partial class Extensions
    {
        /// <summary>Generates one value.</summary>
        public static T SampleOne<T>(
            this IGen<T> gen,
            int? seed = null,
            int? size = null) => gen.Sample(iterations: 1, seed: seed, size: size ?? 100)[0];

        /// <summary>Generates a list of values, 100 by default.</summary>
        public static List<T> Sample<T>(
            this IGen<T> gen,
            int? iterations = null,
            int? seed = null,
            int? size = null) => SampleHelpers.Run(gen.Advanced, iterations, seed, size).Values;

        /// <summary>Generates one value, with the count of discards and of random choices.</summary>
        public static SampleOneWithMetricsResult<T> SampleOneWithMetrics<T>(
            this IGenAdvanced<T> advanced,
            int? seed = null,
            int? size = null)
        {
            var sample = SampleHelpers.Run(advanced, iterations: 1, seed: seed, size: size ?? 100);
            return new SampleOneWithMetricsResult<T>(
                sample.Values[0],
                sample.Discards,
                sample.RandomnessConsumption);
        }

        /// <summary>Generates a list of values, with the count of discards and of random choices.</summary>
        public static SampleWithMetricsResult<T> SampleWithMetrics<T>(
            this IGenAdvanced<T> advanced,
            int? iterations = null,
            int? seed = null,
            int? size = null) => SampleHelpers.Run(advanced, iterations, seed, size);
    }
}

namespace GalaxyCheck.Runners.Sample
{
    /// <summary>One sampled value, with metrics.</summary>
    /// <param name="Value">The sampled value.</param>
    /// <param name="Discards">The count of discarded values.</param>
    /// <param name="RandomnessConsumption">The count of random choices that the generator made.</param>
    [Verifiable]
    public record SampleOneWithMetricsResult<T>(
        T Value,
        int Discards,
        int RandomnessConsumption)
    {
        /// <summary>The verified invariant that sample discard and randomness counts cannot be negative.</summary>
        [Invariant("Sample discard and randomness counts cannot be negative.")]
        public bool HasValidCounts() => Discards >= 0 && RandomnessConsumption >= 0;
    }

    /// <summary>Sampled values, with metrics.</summary>
    /// <param name="Values">The sampled values.</param>
    /// <param name="Discards">The count of discarded values.</param>
    /// <param name="RandomnessConsumption">The count of random choices that the generator made.</param>
    [Unverifiable]
    public record SampleWithMetricsResult<T>(
        List<T> Values,
        int Discards,
        int RandomnessConsumption);

    [Unverifiable]
    internal static class SampleHelpers
    {
        internal static SampleWithMetricsResult<T> Run<T>(
            IGenAdvanced<T> gen,
            int? iterations,
            int? seed,
            int? size)
        {
            var resolvedIterations = iterations ?? CheckRunner.DefaultIterations;
            var resolvedSeed = CheckRunner.CreateSeed(seed);
            var source = GenSource.Create(resolvedSeed, 0);
            source.SizeFixed = size != null;
            var values = new List<T>(Math.Max(0, resolvedIterations));

            for (var i = 0; i < resolvedIterations; i++)
            {
                var iterationSize = CheckRunner.SizeOf(i, resolvedIterations, size);
                source.BeginRandom(iterationSize);
                try
                {
                    values.Add(gen.Generate(source));
                }
                catch (Exception ex) when (!CheckRunner.IsPassedOn(ex))
                {
                    throw CheckRunner.GenError(ex, GenParameters.Parse(resolvedSeed, iterationSize));
                }
                finally
                {
                    source.End();
                }
            }

            return new SampleWithMetricsResult<T>(
                values,
                source.Discards,
                (int)Math.Min(int.MaxValue, source.Consumption));
        }
    }
}
