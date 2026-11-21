using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.Int64GenTests
{
    public class AboutConstraints
    {
        [Property]
        public Property ItProducesValuesGreaterThanOrEqualMinimum() => new Property(
            from min in Gen.Int64()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().GreaterThanEqual(min);

                var exampleSpace = gen.SampleOneTraversal(seed: seed, size: size);

                exampleSpace.Take(100).Should().OnlyContain(value => value >= min);
            }));

        [Property]
        public Property ItProducesValuesLessThanOrEqualMaximum() => new Property(
            from max in Gen.Int64()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().LessThanEqual(max);

                var exampleSpace = gen.SampleOneTraversal(seed: seed, size: size);

                exampleSpace.Take(100).Should().OnlyContain(value => value <= max);
            }));
    }
}
