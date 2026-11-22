namespace GalaxyCheck
{
    using GalaxyCheck.Gens;

    public static partial class Gen
    {
        /// <summary>
        /// Generates valid Unicode scalar values, inside and outside the Basic Multilingual Plane. The values shrink
        /// toward <c>'a'</c>.
        /// </summary>
        public static IGen<System.Text.Rune> Rune() => new RuneGen();
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System.Text;

    /// <summary>
    /// Generates an index into the scalar values without the surrogates, so that each index is a valid rune.
    /// </summary>
    internal sealed record RuneGen : BaseGen<Rune>
    {
        private const int SurrogateStart = 0xD800;
        private const int SurrogateCount = 0x800;
        private const int MaxIndex = 0x10FFFF - SurrogateCount;
        private const int OriginIndex = 'a';

        public override Rune Generate(GenSource source)
        {
            var index = (int)IntCore.Int64(source, 0, MaxIndex, OriginIndex, Gen.Bias.WithSize);
            return new Rune(index < SurrogateStart ? index : index + SurrogateCount);
        }
    }
}
