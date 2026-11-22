using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;
using GalaxyCheck.Gens;
using static Tests.V2.DomainGenAttributes;
using GalaxyCheck.Gens.Injection.Int32;

namespace Tests.V2.GenTests.SetGenTests
{
    public class AboutValidation
    {
        [Property]
        public void ItErrorsWhenCountIsNegative(
            [Seed] int seed,
            [Size] int size,
            [PickGen] GalaxyCheck.IGen<object> elementGen,
            [LessThanEqual(-1)] int count)
        {
            var gen = GalaxyCheck.Gen.Set(elementGen).WithCount(count);

            Action test = () => gen.SampleOne(seed: seed, size: size);

            test.Should()
                .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                .WithGenErrorMessage("'count' cannot be negative");
        }

        [Property]
        public void ItErrorsWhenMinimumCountIsNegative(
            [Seed] int seed,
            [Size] int size,
            [PickGen] GalaxyCheck.IGen<object> elementGen,
            [LessThanEqual(-1)] int minCount)
        {
            var gen = GalaxyCheck.Gen.Set(elementGen).WithCountGreaterThanEqual(minCount);

            Action test = () => gen.SampleOne(seed: seed, size: size);

            test.Should()
                .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                .WithGenErrorMessage("'minCount' cannot be negative");
        }

        [Property]
        public void ItErrorsWhenMaximumCountIsNegative(
            [Seed] int seed,
            [Size] int size,
            [PickGen] GalaxyCheck.IGen<object> elementGen,
            [LessThanEqual(-1)] int maxCount)
        {
            var gen = GalaxyCheck.Gen.Set(elementGen).WithCountLessThanEqual(maxCount);

            Action test = () => gen.SampleOne(seed: seed, size: size);

            test.Should()
                .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                .WithGenErrorMessage("'maxCount' cannot be negative");
        }

        [Property]
        public Property ItErrorsWhenMinimumCountIsGreaterThanMaximumCount() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            from elementGen in DomainGen.Gen()
            from minCount in Gen.Int32().Between(1, 20)
            from maxCount in Gen.Int32().GreaterThanEqual(0).LessThan(minCount)
            select TestProperty.ForThese(() =>
            {
                void ShouldError(ISetGen<object> gen)
                {
                    Action action = () => gen.SampleOne(seed: seed, size: size);

                    action.Should()
                        .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                        .WithGenErrorMessage("'minCount' cannot be greater than 'maxCount'");
                }

                ShouldError(GalaxyCheck.Gen.Set(elementGen).WithCountGreaterThanEqual(minCount).WithCountLessThanEqual(maxCount));
                ShouldError(GalaxyCheck.Gen.Set(elementGen).WithCountLessThanEqual(maxCount).WithCountGreaterThanEqual(minCount));
            }));
    }
}
