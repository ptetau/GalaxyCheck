using FluentAssertions;
using GalaxyCheck;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests.V2.GenTests.InfiniteGenTests
{
    public class AboutIterationLimits
    {
        private const int ExpectedDefaultIterationLimit = 1000;

        [Property]
        public Property AGeneratedEnumerableHasTheDefaultIterationLimitIfNotConfiguredOtherwise() => new Property(
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.InfiniteOf().Select(EnsureSourceCanShrink);

                var sample = gen.SampleOneTraversal(seed: seed, size: size);

                AssertLimit(sample, ExpectedDefaultIterationLimit);
            }));

        [Property]
        public Property AGeneratedEnumerableHasTheGivenLimit() => new Property(
            from elementGen in DomainGen.Gen()
            from limit in Gen.Int32().Between(1, 1000)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.InfiniteOf(iterationLimit: limit).Select(EnsureSourceCanShrink);

                var sample = gen.SampleOneTraversal(seed: seed, size: size);

                AssertLimit(sample, limit);
            }));

        [Property]
        public Property AGeneratedEnumerableWithoutALimitNeverThrows() => new Property(
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.InfiniteOf(iterationLimit: null).Select(EnsureSourceCanShrink);

                var sample = gen.SampleOneTraversal(seed: seed, size: size);

                AssertUnlimited(sample);
            }));

        private static void AssertLimit<T>(List<IEnumerable<T>> traversal, int expectedLimit)
        {
            traversal.Take(10).ToList().ForEach(enumerable =>
            {
                AssertLimit(enumerable, expectedLimit);
            });
        }

        private static void AssertLimit<T>(IEnumerable<T> enumerable, int expectedLimit)
        {
            // It doesn't thrown when the limit is hit
            enumerable.Take(expectedLimit).ToList();

            // It throws when it exceeds the limit
            Action action = () => enumerable.Take(expectedLimit + 1).ToList();
            action.Should()
                .Throw<GalaxyCheck.Exceptions.GenLimitExceededException>()
                .WithMessage("Infinite enumerable exceeded iteration limit. This is a built-in safety mechanism to prevent hanging tests. Relax this constraint by configuring the iterationLimit parameter.");
        }

        private static void AssertUnlimited<T>(List<IEnumerable<T>> traversal)
        {
            traversal.Take(10).ToList().ForEach(enumerable =>
            {
                AssertUnlimited(enumerable);
            });
        }

        private static void AssertUnlimited<T>(IEnumerable<T> enumerable)
        {
            enumerable.Take(ExpectedDefaultIterationLimit + 1).ToList();
        }

        private static IEnumerable<T> EnsureSourceCanShrink<T>(IEnumerable<T> source)
        {
            // Ensure the enumerable has a shrink by enumerating at least one elements. Then it will be able to at
            // least shrink to the enumerable that repeats a single element.
            source.Take(1).ToList();
            return source;
        }
    }
}
