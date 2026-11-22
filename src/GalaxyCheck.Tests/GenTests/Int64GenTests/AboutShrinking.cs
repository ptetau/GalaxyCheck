using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.Int64GenTests
{
    public class AboutShrinking
    {
        [Property]
        public Property ItShrinksToTheOrigin() => new Property(
            from bias in DomainGen.Bias()
            from origin in Gen.Int64()
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().WithBias(bias).ShrinkTowards(origin);

                var minimum = gen.Minimum(seed: seed);

                minimum.Should().Be(origin);
            }));

        [Property]
        public Property ItShrinksToTheLocalMinimum_ForAPositiveRange() => new Property(
            from bias in DomainGen.Bias()
            from origin in Gen.Int64().Between(0, 100)
            from localMin in Gen.Int64().Between(origin, 200)
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().WithBias(bias).ShrinkTowards(origin);

                var minimum = gen.Minimum(seed: seed, pred: value => value >= localMin);

                minimum.Should().Be(localMin);
            }));

        [Property]
        public Property ItShrinksToTheLocalMinimum_ForANegativeRange() => new Property(
            from bias in DomainGen.Bias()
            from origin in Gen.Int64().Between(0, -100)
            from localMin in Gen.Int64().Between(origin, -200)
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().WithBias(bias).ShrinkTowards(origin);

                var minimum = gen.Minimum(seed: seed, pred: value => value <= localMin);

                minimum.Should().Be(localMin);
            }));

        [Property]
        public Property ItShrinksAnyScenarioWithin128Attempts_ForAPositiveRange() => new Property(
            from bias in DomainGen.Bias().NoShrink()
            from localMin in Gen.Int64().Between(0, long.MaxValue / 2).NoShrink()
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().GreaterThanEqual(0).WithBias(bias);

                var minimum = gen.Advanced.MinimumWithMetrics(
                    seed: seed,
                    deepMinimum: false,
                    pred: value => value >= localMin);

                minimum.Shrinks.Should().BeLessOrEqualTo(128);
            }));

        [Property]
        public Property ItShrinksAnyScenarioWithin128Attempts_ForANegativeRange() => new Property(
            from bias in DomainGen.Bias().NoShrink()
            from localMin in Gen.Int64().Between(0, long.MinValue / 2).NoShrink()
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().LessThanEqual(0).WithBias(bias);

                var minimum = gen.Advanced.MinimumWithMetrics(
                    seed: seed,
                    deepMinimum: false,
                    pred: value => value <= localMin);

                minimum.Shrinks.Should().BeLessOrEqualTo(128);
            }));
    }
}
