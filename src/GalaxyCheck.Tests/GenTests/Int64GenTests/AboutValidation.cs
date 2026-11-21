using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;

namespace Tests.V2.GenTests.Int64GenTests
{
    public class AboutValidation
    {
        [Property]
        public Property ItErrorsWhenMinimumIsGreaterThanMaximum() => new Property(
            from min in Gen.Int64().GreaterThan(long.MinValue)
            from max in Gen.Int64().LessThan(min)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().GreaterThanEqual(min).LessThanEqual(max);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'min' cannot be greater than 'max'");
            }));

        [Property]
        public Property ItErrorsWhenMinimumIsGreaterThanOrigin() => new Property(
            from min in Gen.Int64().GreaterThan(long.MinValue)
            from origin in Gen.Int64().LessThan(min)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().GreaterThanEqual(min).ShrinkTowards(origin);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'origin' must be between 'min' and 'max'");
            }));

        [Property]
        public Property ItErrorsWhenMaximumIsLessThanOrigin() => new Property(
            from max in Gen.Int64().LessThan(long.MaxValue)
            from origin in Gen.Int64().GreaterThan(max)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int64().LessThanEqual(max).ShrinkTowards(origin);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'origin' must be between 'min' and 'max'");
            }));
    }
}
