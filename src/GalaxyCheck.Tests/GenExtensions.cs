using GalaxyCheck;
using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using GalaxyCheck.Runners;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests.V2
{
    public static class GenExtensions
    {
        /// <summary>
        /// Samples values, and for each value its traversal: the value, then each shrink that the shrinker makes
        /// when every value is a counterexample.
        /// </summary>
        public static List<List<T>> SampleNTraversals<T>(this IGen<T> gen, int count, int seed, int size)
        {
            var source = GenSource.Create(seed, size);
            var result = new List<List<T>>();

            for (var i = 0; i < count; i++)
            {
                source.BeginRandom(size);
                var value = Generate(gen, source);
                result.Add(Traverse(gen, source, value, size, 100));
            }

            return result;
        }

        public static List<T> SampleOneTraversal<T>(this IGen<T> gen, int seed, int size) =>
            gen.SampleNTraversals(1, seed, size).Single();

        public static string RenderOneTraversal<T>(this IGen<T> gen, int seed, int size,
            Func<T, string>? renderer = null) => string.Join(
                Environment.NewLine,
                gen.SampleNTraversals(1, seed, size).Single()
                    .Take(500)
                    .Select(x => renderer == null ? x!.ToString()! : renderer(x)));

        private static T Generate<T>(IGen<T> gen, GenSource source)
        {
            try
            {
                return gen.Advanced.Generate(source);
            }
            catch (GenErrorSignal signal)
            {
                throw new Exceptions.GenErrorException("Error during generation: " + signal.Message, null);
            }
            finally
            {
                source.End();
            }
        }

        private static List<T> Traverse<T>(IGen<T> gen, GenSource source, T root, int size, int maxCount)
        {
            var values = new List<T> { root };
            var shrinker = new Shrinker(source, int.MaxValue);

            foreach (var _ in shrinker.Candidates())
            {
                if (values.Count >= maxCount) break;

                source.BeginReplay(shrinker.Candidate, shrinker.CandidateLength, size);
                T value;
                try
                {
                    value = gen.Advanced.Generate(source);
                }
                catch (Exception)
                {
                    continue;
                }
                finally
                {
                    source.End();
                }

                if (shrinker.TryAccept(source))
                {
                    values.Add(value);
                }
            }

            return values;
        }
    }
}
