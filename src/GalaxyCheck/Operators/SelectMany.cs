using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using System;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>
        /// Projects each value of a generator to a new generator by the given selector. The choices of the source
        /// generator come before the choices of the projected generator, so that both shrink together.
        /// </summary>
        /// <typeparam name="T">The type of the source generator's value.</typeparam>
        /// <typeparam name="TResult">The type of the projected generator's value.</typeparam>
        /// <param name="gen">The source generator.</param>
        /// <param name="selector">A function that returns a generator for each value of the source.</param>
        /// <returns>A new generator with the projection applied.</returns>
        public static IGen<TResult> SelectMany<T, TResult>(this IGen<T> gen, Func<T, IGen<TResult>> selector) =>
            new SelectManyGen<T, TResult>(gen, selector);

        public static IGen<TResult> SelectMany<T, TResultGen, TResult>(
            this IGen<T> gen,
            Func<T, IGen<TResultGen>> genSelector,
            Func<T, TResultGen, TResult> valueSelector) =>
                new SelectManyGen<T, TResultGen, TResult>(gen, genSelector, valueSelector);
    }
}

namespace GalaxyCheck.Gens.Internal
{
    internal sealed record SelectManyGen<T, TResult>(IGen<T> Source, Func<T, IGen<TResult>> Selector) : BaseGen<TResult>, GalaxyCheck.Properties.IFiniteGen
    {
        public long? Count(long limit) => GalaxyCheck.Properties.FiniteBind.Count(Source, x => Selector((T)x!), limit);

        public void WriteChoices(long index, System.Collections.Generic.List<ulong> choices) =>
            GalaxyCheck.Properties.FiniteBind.WriteChoices(Source, x => Selector((T)x!), index, choices);

        public override TResult Generate(GenSource source)
        {
            var value = Source.Advanced.Generate(source);
            return Selector(value).Advanced.Generate(source);
        }
    }

    internal sealed record SelectManyGen<T, TResultGen, TResult>(
        IGen<T> Source,
        Func<T, IGen<TResultGen>> GenSelector,
        Func<T, TResultGen, TResult> ValueSelector) : BaseGen<TResult>, GalaxyCheck.Properties.IFiniteGen
    {
        public long? Count(long limit) => GalaxyCheck.Properties.FiniteBind.Count(Source, x => GenSelector((T)x!), limit);

        public void WriteChoices(long index, System.Collections.Generic.List<ulong> choices) =>
            GalaxyCheck.Properties.FiniteBind.WriteChoices(Source, x => GenSelector((T)x!), index, choices);

        public override TResult Generate(GenSource source)
        {
            var value = Source.Advanced.Generate(source);
            var other = GenSelector(value).Advanced.Generate(source);
            return ValueSelector(value, other);
        }
    }
}
