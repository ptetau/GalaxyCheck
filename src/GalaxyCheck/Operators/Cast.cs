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

        public static IGen<T> Cast<T>(this IGen<T> gen) => gen;
    }
}

namespace GalaxyCheck.Gens.Internal
{
    internal sealed record CastGen<T>(IGen Source) : BaseGen<T>
    {
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
