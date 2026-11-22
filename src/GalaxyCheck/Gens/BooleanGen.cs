namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>
        /// Creates a generator that produces booleans. The value false is simpler than true.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IGen<bool> Boolean() => new Gens.BooleanGen();
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;

    internal sealed record BooleanGen : BaseGen<bool>, GalaxyCheck.Properties.IFiniteGen
    {
        public long? Count(long limit) => limit >= 2 ? 2 : null;

        public void WriteChoices(long index, System.Collections.Generic.List<ulong> choices) => choices.Add((ulong)index);

        public override bool Generate(GenSource source) => IntCore.Index(source, 1, Gen.Bias.WithSize) == 1;
    }
}
