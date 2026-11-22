using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.ListGenTests
{
    public class AboutConstraints
    {
        [Property]
        public Property ItProducesValuesWithCountGreaterThanOrEqualMinimumCount() => new Property(
            from elementGen in DomainGen.Gen()
            from minCount in Gen.Int32().Between(0, 10)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.ListOf().WithCountGreaterThanEqual(minCount);

                var exampleSpace = gen.SampleOneTraversal(seed: seed, size: size);

                exampleSpace.Take(100).Should().OnlyContain(value => value.Count >= minCount);
            }));

        [Property]
        public Property ItProducesValuesWithCountLessThanOrEqualMaximumCount() => new Property(
            from elementGen in DomainGen.Gen()
            from maxCount in Gen.Int32().Between(0, 10)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.ListOf().WithCountLessThanEqual(maxCount);

                var exampleSpace = gen.SampleOneTraversal(seed: seed, size: size);

                exampleSpace.Take(100).Should().OnlyContain(value => value.Count <= maxCount);
            }));

        [Property]
        public Property ItProducesValuesWithOfTheSpecificCount() => new Property(
            from elementGen in DomainGen.Gen()
            from count in Gen.Int32().Between(0, 20)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.ListOf().WithCount(count);

                var exampleSpace = gen.SampleOneTraversal(seed: seed, size: size);

                exampleSpace.Take(100).Should().OnlyContain(value => value.Count == count);
            }));
    }
}
