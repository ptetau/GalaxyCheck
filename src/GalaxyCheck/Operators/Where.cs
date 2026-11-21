using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using System;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>
        /// Filters a generator's values based on a predicate. A value that does not pass the predicate is discarded,
        /// and the generator tries again.
        /// </summary>
        /// <typeparam name="T">The type of the generator's value.</typeparam>
        /// <param name="gen">The generator to apply the predicate to.</param>
        /// <param name="pred">A predicate function that tests each value.</param>
        /// <returns>A new generator with the filter applied.</returns>
        public static IGen<T> Where<T>(this IGen<T> gen, Func<T, bool> pred) => new WhereGen<T>(gen, pred);
    }
}

namespace GalaxyCheck.Gens.Internal
{
    internal sealed record WhereGen<T>(IGen<T> Source, Func<T, bool> Predicate) : BaseGen<T>
    {
        private const int MaxConsecutiveDiscards = 100;
        private const int DiscardsBeforeResize = 5;

        public override T Generate(GenSource source)
        {
            var gen = Source.Advanced;
            var size = source.Size;

            try
            {
                for (var attempt = 0; attempt < MaxConsecutiveDiscards; attempt++)
                {
                    var start = source.BeginSpan();
                    var value = gen.Generate(source);
                    source.EndSpan(start);

                    if (Predicate(value))
                    {
                        return value;
                    }

                    source.Discards++;

                    if ((attempt + 1) % DiscardsBeforeResize == 0 && !source.IsReplaying)
                    {
                        // A bigger size may hold more values that pass the predicate.
                        source.Size = Math.Min(100, source.Size + 5);
                    }
                }
            }
            finally
            {
                source.Size = size;
            }

            throw new Exceptions.GenExhaustionException();
        }
    }
}
