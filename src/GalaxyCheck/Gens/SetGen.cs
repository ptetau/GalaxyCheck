namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using GalaxyCheck.Gens.Internal;

    public static partial class Gen
    {
        /// <summary>Generates sets whose elements come from the given generator.</summary>
        public static ISetGen<T> Set<T>(IGen<T> elementGen) => new SetGen<T>(
            ElementGen: elementGen,
            Count: RangeIntention.Unspecified());
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Creates a generator that produces sets, the elements of which are produced by the given generator. By
        /// default, the generator produces sets ranging from count 0 to 20 - but this can be configured using the
        /// builder methods on <see cref="ISetGen{T}"/>.
        /// </summary>
        /// <param name="elementGen">The generator used to produce the elements of the set.</param>
        /// <returns>The new generator.</returns>
        public static ISetGen<T> SetOf<T>(this IGen<T> gen) => Gen.Set(gen);

        /// <summary>
        /// Constrains the generator so that it only produces sets with counts within the supplied range (inclusive).
        /// </summary>
        /// <param name="x">The first bound of the range.</param>
        /// <param name="y">The second bound of the range.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static ISetGen<T> WithCountBetween<T>(this ISetGen<T> gen, int x, int y)
        {
            var minCount = x > y ? y : x;
            var maxCount = x > y ? x : y;
            return gen.WithCountGreaterThanEqual(minCount).WithCountLessThanEqual(maxCount);
        }

        /// <summary>
        /// Constrains the generator so that it only produces sets with greater than the given count.
        /// </summary>
        /// <param name="exclusiveMinCount">The minimum count that generated sets should be constrained to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static ISetGen<T> WithCountGreaterThan<T>(this ISetGen<T> gen, int exclusiveMinCount) =>
            gen.WithCountGreaterThanEqual(exclusiveMinCount + 1);

        /// <summary>
        /// Constrains the generator so that it only produces sets less than the given count.
        /// </summary>
        /// <param name="exclusiveMaxCount">The maximum count that generated sets should be constrained to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static ISetGen<T> WithCountLessThan<T>(this ISetGen<T> gen, int exclusiveMaxCount) =>
            gen.WithCountLessThanEqual(exclusiveMaxCount - 1);
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System;
    using System.Collections.Generic;

    /// <summary>A generator of sets with options for the count of elements.</summary>
    public interface ISetGen<T> : IGen<IReadOnlySet<T>>
    {
        /// <summary>
        /// Constrains the generator so that it only produces sets with the given count.
        /// </summary>
        /// <param name="count">The count to constrain generated sets to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        ISetGen<T> WithCount(int count);

        /// <summary>
        /// Constrains the generator so that it only produces sets with at least the given count.
        /// </summary>
        /// <param name="minCount">The minimum count that generated sets should be constrained to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        ISetGen<T> WithCountGreaterThanEqual(int minCount);

        /// <summary>
        /// Constrains the generator so that it only produces sets with at most the given count.
        /// </summary>
        /// <param name="maxCount">The maximum count that generated sets should be constrained to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        ISetGen<T> WithCountLessThanEqual(int maxCount);

        /// <summary>
        /// Modifies how the generator biases counts of produced sets with respect to the size parameter.
        /// </summary>
        /// <returns>A new generator with the biasing effect applied.</returns>
        ISetGen<T> WithCountBias(Gen.Bias bias);
    }

    [global::Verifiable.Contracts.Unverifiable]
    internal record SetGen<T>(
        IGen<T> ElementGen,
        RangeIntention Count) : BaseGen<IReadOnlySet<T>>, ISetGen<T>
    {
        private const int MaxAttemptsPerElement = 100;

        public ISetGen<T> WithCount(int count) => this with { Count = RangeIntention.Exact(count) };

        public ISetGen<T> WithCountGreaterThanEqual(int minCount) => this with { Count = Count.WithMinimum(minCount) };

        public ISetGen<T> WithCountLessThanEqual(int maxCount) => this with { Count = Count.WithMaximum(maxCount) };

        public ISetGen<T> WithCountBias(Gen.Bias bias)
        {
            throw new System.NotImplementedException();
        }

        public override IReadOnlySet<T> Generate(GenSource source)
        {
            var (minCount, maxCount) = CountRange.Resolve(Count, enableCountLimit: false);
            var elementGen = ElementGen.Advanced;

            var targetCount = source.IsReplaying ? 0 : IntCore.RandomInt32(source, minCount, maxCount, Gen.Bias.WithSize);
            var set = new HashSet<T>();

            var size = source.Size;
            try
            {
                while (set.Count < maxCount)
                {
                    var elementStart = source.BeginSpan();
                    if (set.Count >= minCount)
                    {
                        var more = source.IsReplaying
                            ? source.Read(1) == 1
                            : source.Record(set.Count < targetCount ? 1UL : 0UL) == 1;

                        if (!more)
                        {
                            break;
                        }
                    }

                    var added = false;
                    for (var attempt = 0; attempt < MaxAttemptsPerElement; attempt++)
                    {
                        var start = source.BeginSpan();
                        var element = elementGen.Generate(source);
                        source.EndSpan(start);

                        if (set.Add(element))
                        {
                            added = true;
                            break;
                        }

                        if ((attempt + 1) % 5 == 0 && !source.IsReplaying)
                        {
                            // A bigger size may hold more unique values.
                            source.Size = Math.Min(100, source.Size + 5);
                        }
                    }

                    source.EndSpan(elementStart);

                    if (!added)
                    {
                        if (set.Count < minCount)
                        {
                            throw new Exceptions.GenExhaustionException();
                        }

                        // The element generator has no more unique values. Stop here, as a "stop" choice would.
                        break;
                    }
                }
            }
            finally
            {
                source.Size = size;
            }

            return set;
        }
    }
}
