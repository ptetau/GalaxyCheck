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
        /// <summary>
        /// A guard against a filter that can never pass. The health check judges the share of discards over the whole
        /// run, so the guard is far above the retries that an ordinary filter needs for one value.
        /// </summary>
        private const int MaxConsecutiveDiscards = 1000;

        /// <summary>
        /// In a replay, the retries after the recorded choices run out are all the same, so a replay stops after this
        /// many retries once the choices run out.
        /// </summary>
        private const int MaxReplayDiscards = 100;

        /// <summary>
        /// Generates values until one passes the predicate. The first attempt uses the size of the source, so a value
        /// that passes at once is the same as without the filter. Each retry first records its size as a frozen
        /// choice (see <see cref="GenSource.RetrySize"/>). The span of a discarded attempt holds that choice too, so
        /// the shrinker can delete the attempt and the later attempts keep their sizes. The learned good size belongs to
        /// the method of the predicate, so the filter objects that a <c>SelectMany</c> builds for each value share it.
        /// </summary>
        public override T Generate(GenSource source)
        {
            var gen = Source.Advanced;
            var size = source.Size;

            try
            {
                var start = source.BeginSpan();
                for (var attempt = 0; attempt < MaxConsecutiveDiscards; attempt++)
                {
                    var value = gen.Generate(source);

                    if (Predicate(value))
                    {
                        source.EndSpan(start);
                        source.Kept++;
                        if (attempt > 0) source.RememberGoodSize(Predicate.Method, source.Size);
                        return value;
                    }

                    source.Discards++;
                    if (attempt + 1 >= MaxReplayDiscards && source.ReplayExhausted) break;
                    source.Size = source.RetrySize(Predicate.Method, size, attempt + 1);
                    source.EndSpan(start);
                    start = source.BeginSpan();
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
