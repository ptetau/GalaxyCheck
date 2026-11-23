namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using GalaxyCheck.Gens.Internal;

    public static partial class Gen
    {
        /// <summary>
        /// Creates a generator that produces lists, the elements of which are produced by the given generator. By
        /// default, the generator produces lists ranging from count 0 to 20 - but this can be configured using the
        /// builder methods on <see cref="IListGen{T}"/>.
        /// </summary>
        /// <param name="elementGen">The generator used to produce the elements of the list.</param>
        /// <returns>The new generator.</returns>
        public static IListGen<T> List<T>(IGen<T> elementGen) => new ListGen<T>(
            ElementGen: elementGen,
            Count: RangeIntention.Unspecified(),
            Bias: null,
            EnableCountLimit: null);
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Creates a generator that produces lists, the elements of which are produced by the given generator. By
        /// default, the generator produces lists ranging from count 0 to 20 - but this can be configured using the
        /// builder methods on <see cref="IListGen{T}"/>.
        /// </summary>
        /// <param name="elementGen">The generator used to produce the elements of the list.</param>
        /// <returns>The new generator.</returns>
        public static IListGen<T> ListOf<T>(this IGen<T> gen) => Gen.List(gen);

        /// <summary>
        /// Constrains the generator so that it only produces lists with counts within the supplied range (inclusive).
        /// </summary>
        /// <param name="x">The first bound of the range.</param>
        /// <param name="y">The second bound of the range.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IListGen<T> WithCountBetween<T>(this IListGen<T> gen, int x, int y)
        {
            var minCount = x > y ? y : x;
            var maxCount = x > y ? x : y;
            return gen.WithCountGreaterThanEqual(minCount).WithCountLessThanEqual(maxCount);
        }

        /// <summary>
        /// Constrains the generator so that it only produces lists with greater than the given count.
        /// </summary>
        /// <param name="exclusiveMinCount">The minimum count that generated lists should be constrained to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IListGen<T> WithCountGreaterThan<T>(this IListGen<T> gen, int exclusiveMinCount) =>
            gen.WithCountGreaterThanEqual(exclusiveMinCount + 1);

        /// <summary>
        /// Constrains the generator so that it only produces lists less than the given count.
        /// </summary>
        /// <param name="exclusiveMaxCount">The maximum count that generated lists should be constrained to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IListGen<T> WithCountLessThan<T>(this IListGen<T> gen, int exclusiveMaxCount) =>
            gen.WithCountLessThanEqual(exclusiveMaxCount - 1);
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System;
    using System.Buffers;
    using System.Collections.Generic;

    /// <summary>A generator of lists with options for the count of elements.</summary>
    public interface IListGen<T> : IGen<IReadOnlyList<T>>
    {
        /// <summary>
        /// Constrains the generator so that it only produces lists with the given count.
        /// </summary>
        /// <param name="count">The count to constrain generated lists to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        IListGen<T> WithCount(int count);

        /// <summary>
        /// Constrains the generator so that it only produces lists with at least the given count.
        /// </summary>
        /// <param name="minCount">The minimum count that generated lists should be constrained to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        IListGen<T> WithCountGreaterThanEqual(int minCount);

        /// <summary>
        /// Constrains the generator so that it only produces lists with at most the given count.
        /// </summary>
        /// <param name="maxCount">The maximum count that generated lists should be constrained to.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        IListGen<T> WithCountLessThanEqual(int maxCount);

        /// <summary>
        /// Modifies how the generator biases the count of produced lists with respect to the size parameter.
        /// </summary>
        /// <param name="bias">The new bias.</param>
        /// <returns>A new generator with the new bias applied.</returns>
        IListGen<T> WithCountBias(Gen.Bias bias);

        /// <summary>
        /// Disables the count limit. The count limit stops a list from having more than 1000 elements.
        /// </summary>
        /// <returns>A new generator with the limit disabled.</returns>
        IListGen<T> DisableCountLimitUnsafe();
    }

    internal record ListGen<T>(
        IGen<T> ElementGen,
        RangeIntention Count,
        Gen.Bias? Bias,
        bool? EnableCountLimit) : BaseGen<IReadOnlyList<T>>, IListGen<T>
    {
        public IListGen<T> WithCount(int count) =>
            this with { Count = RangeIntention.Exact(count) };

        public IListGen<T> WithCountGreaterThanEqual(int minCount) =>
            this with { Count = Count.WithMinimum(minCount) };

        public IListGen<T> WithCountLessThanEqual(int maxCount) =>
            this with { Count = Count.WithMaximum(maxCount) };

        public IListGen<T> WithCountBias(Gen.Bias bias) => this with { Bias = bias };

        public IListGen<T> DisableCountLimitUnsafe() => this with { EnableCountLimit = false };

        public override IReadOnlyList<T> Generate(GenSource source)
        {
            var (minCount, maxCount) = CountRange.Resolve(Count, EnableCountLimit ?? true);
            return Collections.GenerateList(source, ElementGen.Advanced, minCount, maxCount, Bias ?? Gen.Bias.WithSize);
        }
    }

    internal static class CountRange
    {
        public const int CountLimit = 1000;

        public static (int minCount, int maxCount) Resolve(RangeIntention count, bool enableCountLimit)
        {
            var (minCount, maxCount) = count.Match(
                onUnspecified: () => (0, 20),
                onExact: exact =>
                {
                    if (exact < 0) throw new GenErrorSignal("'count' cannot be negative");
                    return (exact, exact);
                },
                onBounded: (min, max) =>
                {
                    var resolvedMin = min ?? 0;
                    var resolvedMax = max ?? (resolvedMin > int.MaxValue - 20 ? int.MaxValue : resolvedMin + 20);

                    if (resolvedMin < 0) throw new GenErrorSignal("'minCount' cannot be negative");
                    if (resolvedMax < 0) throw new GenErrorSignal("'maxCount' cannot be negative");
                    if (resolvedMin > resolvedMax) throw new GenErrorSignal("'minCount' cannot be greater than 'maxCount'");

                    return (resolvedMin, resolvedMax);
                });

            if (enableCountLimit && (minCount > CountLimit || maxCount > CountLimit))
            {
                throw new GenErrorSignal($"Count limit exceeded. This is a built-in safety mechanism to prevent hanging tests. If generating a list with over 1000 elements was intended, relax this constraint by calling {nameof(IListGen<object>)}.{nameof(IListGen<object>.DisableCountLimitUnsafe)}().");
            }

            return (minCount, maxCount);
        }
    }

    internal static class Collections
    {
        /// <summary>
        /// Generates the elements of a collection. Before each element above the minimum count, a choice of 1 means
        /// "one more element" and a choice of 0 means "stop". Each element and its choice is a span, so the shrinker
        /// can delete it.
        /// </summary>
        public static List<T> GenerateList<T>(GenSource source, IGenAdvanced<T> elementGen, int minCount, int maxCount, Gen.Bias bias)
        {
            if (!source.IsReplaying)
            {
                var count = IntCore.RandomInt32(source, minCount, maxCount, bias);
                var list = new List<T>(count);
                for (var i = 0; i < count; i++)
                {
                    var start = source.BeginSpan();
                    if (i >= minCount)
                    {
                        source.Record(1);
                    }

                    list.Add(elementGen.Generate(source));
                    source.EndSpan(start);
                }

                if (count < maxCount)
                {
                    source.Record(0);
                    source.MarkStop();
                }

                return list;
            }

            var pool = ArrayPool<T>.Shared;
            var buffer = pool.Rent(Math.Min(Math.Max(minCount, 16), 1024));
            var length = 0;
            try
            {
                while (length < maxCount)
                {
                    var start = source.BeginSpan();
                    if (length >= minCount && source.Read(1) == 0)
                    {
                        source.MarkStop();
                        break;
                    }

                    var element = elementGen.Generate(source);
                    source.EndSpan(start);

                    if (length == buffer.Length)
                    {
                        var bigger = pool.Rent(buffer.Length * 2);
                        Array.Copy(buffer, bigger, length);
                        pool.Return(buffer, clearArray: true);
                        buffer = bigger;
                    }

                    buffer[length++] = element;
                }

                var result = new List<T>(length);
                for (var i = 0; i < length; i++)
                {
                    result.Add(buffer[i]);
                }

                return result;
            }
            finally
            {
                pool.Return(buffer, clearArray: true);
            }
        }
    }
}
