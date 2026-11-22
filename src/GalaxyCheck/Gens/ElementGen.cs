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

    internal sealed record ElementGen<T>(List<T> Elements) : BaseGen<T>
    {
        public override T Generate(GenSource source) =>
            Elements[IntCore.Index(source, Elements.Count - 1, Gen.Bias.WithSize)];
    }
}
