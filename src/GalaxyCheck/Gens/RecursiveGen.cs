using System;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>
        /// Generates values of a recursive type. A leaf has depth 0, and each call of <paramref name="wrap"/> adds one
        /// level. No value is deeper than <paramref name="maxDepth"/>. A failing value shrinks towards a leaf.
        /// </summary>
        /// <param name="leafGen">The generator of the leaves.</param>
        /// <param name="wrap">A function that builds a generator of values from a generator of smaller values.</param>
        /// <param name="maxDepth">The maximum depth of a value.</param>
        /// <returns>The new generator.</returns>
        public static IGen<T> Recursive<T>(IGen<T> leafGen, Func<IGen<T>, IGen<T>> wrap, int maxDepth = 5)
        {
            if (leafGen is null)
            {
                return Advanced.Error<T>("'leafGen' must not be null");
            }

            if (wrap is null)
            {
                return Advanced.Error<T>("'wrap' must not be null");
            }

            if (maxDepth < 0 || maxDepth > Gens.RecursiveGen<T>.MaxDepthLimit)
            {
                return Advanced.Error<T>($"'maxDepth' must be between 0 and {Gens.RecursiveGen<T>.MaxDepthLimit}");
            }

            return new Gens.RecursiveGen<T>(leafGen, wrap, maxDepth);
        }
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using System;

    /// <summary>
    /// Builds one generator per depth. The generator of depth d chooses a leaf (choice 0) or a wrap of the generator
    /// of depth d - 1 (choice 1), so the depth never passes the limit and a failing value shrinks towards a leaf.
    /// </summary>
    internal sealed record RecursiveGen<T>(IGen<T> LeafGen, Func<IGen<T>, IGen<T>> Wrap, int MaxDepth) : GenProvider<T>
    {
        public const int MaxDepthLimit = 100;

        protected override IGen<T> Get
        {
            get
            {
                // Each depth reads the choice of a branch, and the last depth has only the leaf, so that a tree has the
                // same choices at each depth and the shrinker can put a subtree in the place of its parent.
                IGen<T> gen = new WeightedChoiceGen<T>(new[] { LeafGen.Advanced }, new[] { 1 });
                for (var depth = 1; depth <= MaxDepth; depth++)
                {
                    gen = new WeightedChoiceGen<T>(new[] { LeafGen.Advanced, Wrap(gen).Advanced }, new[] { 1, 1 });
                }

                return gen;
            }
        }
    }
}
