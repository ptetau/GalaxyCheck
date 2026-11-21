using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;

namespace Tests.V2.GenTests.Int32GenTests
{
    public class AboutValidation
    {
        [Property]
        public Property ItErrorsWhenMinimumIsGreaterThanMaximum() => new Property(
            from min in Gen.Int32().GreaterThan(int.MinValue)
            from max in Gen.Int32().LessThan(min)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int32().GreaterThanEqual(min).LessThanEqual(max);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'min' cannot be greater than 'max'");
            }));

        [Property]
        public Property ItErrorsWhenMinimumIsGreaterThanOrigin() => new Property(
            from min in Gen.Int32().GreaterThan(int.MinValue)
            from origin in Gen.Int32().LessThan(min)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int32().GreaterThanEqual(min).ShrinkTowards(origin);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'origin' must be between 'min' and 'max'");
            }));

        [Property]
        public Property ItErrorsWhenMaximumIsLessThanOrigin() => new Property(
            from max in Gen.Int32().LessThan(int.MaxValue)
            from origin in Gen.Int32().GreaterThan(max)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Int32().LessThanEqual(max).ShrinkTowards(origin);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'origin' must be between 'min' and 'max'");
            }));
    }
}
