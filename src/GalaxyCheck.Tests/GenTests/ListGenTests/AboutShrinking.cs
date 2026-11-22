using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.ListGenTests
{
    public class AboutShrinking
    {
        [Property]
        public Property ItShrinksTheCountToTheMinimumCountWhilstShrinkingTheElementsToTheirMinimums() => new Property(
            from bias in DomainGen.Bias()
            from elementGen in DomainGen.Gen()
            from minCount in Gen.Int32().Between(0, 20)
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var elementMinimum = elementGen.Minimum(seed: seed);
                var gen = GalaxyCheck.Gen.List(elementGen).WithCountGreaterThanEqual(minCount);

                var minimum = gen.Minimum(seed: seed);

                minimum.Should()
                    .HaveCount(minCount).And
                    .AllBeEquivalentTo(elementMinimum);
            }));

        [Property]
        public Property ItShrinksTheCountToTheLocalMinimumCount() => new Property(
            from bias in DomainGen.Bias()
            from elementGen in DomainGen.Gen()
            from localMinCount in Gen.Int32().Between(0, 5)
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.List(elementGen);

                var minimum = gen.Minimum(seed: seed, pred: list => list.Count >= localMinCount);

                minimum.Should().HaveCount(localMinCount);
            }));

        [Property]
        public Property ItShrinksToTheSinglePredicatedElement() => new Property(
            from bias in DomainGen.Bias()
            from elementMinimum in Gen.Int32().Between(0, 100)
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int32().Between(0, 100).ListOf();

                var minimum = gen.Minimum(seed: seed, pred: list => list.Any(element => element >= elementMinimum));

                minimum.Should().BeEquivalentTo(new[] { elementMinimum });
            }));
    }
}
