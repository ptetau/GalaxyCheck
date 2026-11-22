using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>
        /// Prevents the generator from shrinking its values.
        /// </summary>
        /// <typeparam name="T">The type of the generator's value.</typeparam>
        /// <param name="gen">The generator to prevent shrinking for.</param>
        /// <returns>A new generator with shrinking disabled.</returns>
        public static IGen<T> NoShrink<T>(this IGen<T> gen) => new NoShrinkGen<T>(gen);
    }
}

namespace GalaxyCheck.Gens.Internal
{
    /// <summary>
    /// Records one frozen choice, a seed, and generates the value from a private source with that seed. The shrinker
    /// does not change a frozen choice, so the value does not shrink.
    /// </summary>
    internal sealed record NoShrinkGen<T>(IGen<T> Source) : BaseGen<T>
    {
        public override T Generate(GenSource source)
        {
            var seed = source.DrawUniform(ulong.MaxValue);
            source.FreezeLast();

            var inner = new GenSource(seed, source.Size, capacity: 4) { SizeFixed = source.SizeFixed };
            inner.BeginRandom(source.Size);
            var value = Source.Advanced.Generate(inner);
            source.Discards += inner.Discards;
            source.Kept += inner.Kept;
            return value;
        }
    }
}
