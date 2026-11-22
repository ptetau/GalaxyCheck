using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using System;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>
        /// Projects each element in a generator to a new form.
        /// </summary>
        /// <typeparam name="T">The type of the generator's value.</typeparam>
        /// <typeparam name="TResult">The type of the resultant generator's value.</typeparam>
        /// <param name="gen">The generator to apply the projection to.</param>
        /// <param name="selector">A transform function to apply to each value.</param>
        /// <returns>A new generator with the projection applied.</returns>
        public static IGen<TResult> Select<T, TResult>(this IGen<T> gen, Func<T, TResult> selector) =>
            new SelectGen<T, TResult>(gen, selector);
    }
}

namespace GalaxyCheck.Gens.Internal
{
    internal sealed record SelectGen<T, TResult>(IGen<T> Source, Func<T, TResult> Selector) : BaseGen<TResult>, GalaxyCheck.Properties.IFiniteGen
    {
        public long? Count(long limit) => GalaxyCheck.Properties.Finite.Of(Source, limit);

        public void WriteChoices(long index, System.Collections.Generic.List<ulong> choices) =>
            ((GalaxyCheck.Properties.IFiniteGen)Source.Advanced).WriteChoices(index, choices);

        public override TResult Generate(GenSource source) => Selector(Source.Advanced.Generate(source));
    }
}
