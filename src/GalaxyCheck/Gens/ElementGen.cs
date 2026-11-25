using System.Collections.Generic;
using System.Linq;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>
        /// Creates a generator that produces an element of the given source. The values shrink towards the first
        /// element of the source.
        /// </summary>
        /// <param name="source">The elements to pick from.</param>
        /// <returns>The new generator.</returns>
        public static IGen<T> Element<T>(IEnumerable<T> source)
        {
            var sourceList = source.ToList();

            if (sourceList.Count == 0)
            {
                return Advanced.Error<T>("'source' must not be empty");
            }

            return new Gens.ElementGen<T>(sourceList);
        }
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;

    /// <summary>
    /// Picks an element. The elements are uniform by default; the characters of the char generator keep the bias of the
    /// string generator, so that a string is like a list of its characters.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal sealed record ElementGen<T>(List<T> Elements, Gen.Bias Bias = Gen.Bias.None) : BaseGen<T>, GalaxyCheck.Properties.IFiniteGen
    {
        public long? Count(long limit) => Elements.Count <= limit ? Elements.Count : null;

        public void WriteChoices(long index, List<ulong> choices)
        {
            if (Elements.Count > 1) choices.Add((ulong)index);
        }

        public override T Generate(GenSource source) =>
            Elements[IntCore.Index(source, Elements.Count - 1, Bias)];
    }
}
