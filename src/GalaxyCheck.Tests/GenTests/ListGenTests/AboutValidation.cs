using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;
using GalaxyCheck.Gens;

namespace Tests.V2.GenTests.ListGenTests
{
    public class AboutValidation
    {
        [Property]
        public Property ItErrorsWhenCountIsNegative() => new Property(
            from elementGen in DomainGen.Gen()
            from count in Gen.Int32().LessThan(0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.List(elementGen).WithCount(count);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'count' cannot be negative");
            }));

        [Property]
        public Property ItErrorsWhenMinimumCountIsNegative() => new Property(
            from elementGen in DomainGen.Gen()
            from minCount in Gen.Int32().LessThan(0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.List(elementGen).WithCountGreaterThanEqual(minCount);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'minCount' cannot be negative");
            }));

        [Property]
        public Property ItErrorsWhenMaximumCountIsNegative() => new Property(
            from elementGen in DomainGen.Gen()
            from maxCount in Gen.Int32().LessThan(0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.List(elementGen).WithCountLessThanEqual(maxCount);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'maxCount' cannot be negative");
            }));

        [Property]
        public Property ItErrorsWhenMinimumCountIsGreaterThanMaximumCount() => new Property(
            from elementGen in DomainGen.Gen()
            from minCount in Gen.Int32().Between(1, 20)
            from maxCount in Gen.Int32().GreaterThanEqual(0).LessThan(minCount)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                void ShouldError(IListGen<object> gen)
                {
                    Action action = () => gen.SampleOne(seed: seed, size: size);

                    action.Should()
                        .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                        .WithGenErrorMessage("'minCount' cannot be greater than 'maxCount'");
                }

                ShouldError(GalaxyCheck.Gen.List(elementGen).WithCountGreaterThanEqual(minCount).WithCountLessThanEqual(maxCount));
                ShouldError(GalaxyCheck.Gen.List(elementGen).WithCountLessThanEqual(maxCount).WithCountGreaterThanEqual(minCount));
            }));
    }
}
