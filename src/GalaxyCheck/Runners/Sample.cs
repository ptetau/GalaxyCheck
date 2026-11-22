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
        public static T SampleOne<T>(
            this IGen<T> gen,
            int? seed = null,
            int? size = null) => gen.Sample(iterations: 1, seed: seed, size: size ?? 100)[0];

        public static List<T> Sample<T>(
            this IGen<T> gen,
            int? iterations = null,
            int? seed = null,
            int? size = null) => SampleHelpers.Run(gen.Advanced, iterations, seed, size).Values;

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

        public static SampleWithMetricsResult<T> SampleWithMetrics<T>(
            this IGenAdvanced<T> advanced,
            int? iterations = null,
            int? seed = null,
            int? size = null) => SampleHelpers.Run(advanced, iterations, seed, size);
    }
}

namespace GalaxyCheck.Runners.Sample
{
    public record SampleOneWithMetricsResult<T>(
        T Value,
        int Discards,
        int RandomnessConsumption);

    public record SampleWithMetricsResult<T>(
        List<T> Values,
        int Discards,
        int RandomnessConsumption);

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
