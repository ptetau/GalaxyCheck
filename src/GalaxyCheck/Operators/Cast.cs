using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using System;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>
        /// Casts each value of the generator to the given type.
        /// </summary>
        /// <typeparam name="T">The type to cast to.</typeparam>
        /// <param name="gen">The generator to cast the values of.</param>
        /// <returns>A new generator with the cast applied.</returns>
        public static IGen<T> Cast<T>(this IGen gen) => gen is IGen<T> typed ? typed : new CastGen<T>(gen);

        /// <summary>Returns the generator as it is. The overload stops a cast of a typed generator through <see cref="IGen"/>.</summary>
        public static IGen<T> Cast<T>(this IGen<T> gen) => gen;
    }
}

namespace GalaxyCheck.Gens.Internal
{
    [global::Verifiable.Contracts.Unverifiable]
    internal sealed record CastGen<T>(IGen Source) : BaseGen<T>, GalaxyCheck.Properties.IFiniteGen
    {
        public long? Count(long limit) => GalaxyCheck.Properties.Finite.Of(Source, limit);

        public void WriteChoices(long index, System.Collections.Generic.List<ulong> choices) =>
            ((GalaxyCheck.Properties.IFiniteGen)Source.Advanced).WriteChoices(index, choices);

        public override T Generate(GenSource source)
        {
            var value = Source.Advanced.GenerateObject(source);
            if (value is T typed) return typed;

            try
            {
                return (T)value!;
            }
            catch (Exception)
            {
                return (T)Convert.ChangeType(value, typeof(T))!;
            }
        }
    }
}
