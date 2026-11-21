using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;

namespace Tests.V2.GenTests.ChooseGenTests
{
    public class AboutValidation
    {
        [Property]
        public Property ItErrorsWhenNoGensAreGiven() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                void AssertError(GalaxyCheck.IGen<object> gen)
                {
                    Action action = () => gen.SampleOne(seed: seed, size: size);

                    action.Should()
                        .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                        .WithGenErrorMessage("'choices' must be non-empty");
                }

                {
                    var source = Array.Empty<(GalaxyCheck.IGen<object>, int)>();
                    var gen = GalaxyCheck.Gen.Choose(source);

                    AssertError(gen);
                }

                {
                    var source = Array.Empty<GalaxyCheck.IGen<object>>();
                    var gen = GalaxyCheck.Gen.Choose(source);

                    AssertError(gen);
                }

                {
                    AssertError(GalaxyCheck.Gen.Choose<object>());
                }
            }));

        [Property]
        public Property ItErrorsWhenTheTotalWeightIsZero() => new Property(
            from choiceGen in DomainGen.Gen()
            from choiceWeights in Gen.Constant(0).ListOf().WithCountGreaterThanEqual(1)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                void AssertError(GalaxyCheck.IGen<object> gen)
                {
                    Action action = () => gen.SampleOne(seed: seed, size: size);

                    action.Should()
                        .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                        .WithGenErrorMessage("'choices' must contain at least one generator with a weight greater than zero");
                }

                {
                    var source = choiceWeights.Select(choiceWeight => (choiceGen, choiceWeight)).ToArray();
                    var gen = GalaxyCheck.Gen.Choose(source);

                    AssertError(gen);
                }

                {
                    var gen = choiceWeights.Aggregate(
                        GalaxyCheck.Gen.Choose<object>(),
                        (acc, curr) => acc.WithChoice(choiceGen, curr));

                    AssertError(gen);
                }
            }));

        [Property]
        public Property ItErrorsWhenThereIsANegativelyWeightedChoice() => new Property(
            from choiceGen in DomainGen.Gen()
            from choiceWeight in Gen.Int32().LessThan(0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                void AssertError(GalaxyCheck.IGen<object> gen)
                {
                    Action action = () => gen.SampleOne(seed: seed, size: size);

                    action.Should()
                        .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                        .WithGenErrorMessage("'choices' must not contain a negatively weighted generator");
                }

                {
                    var source = new[] { (choiceGen, choiceWeight) };
                    var gen = GalaxyCheck.Gen.Choose(source);

                    AssertError(gen);
                }

                {
                    var gen = GalaxyCheck.Gen.Choose<object>().WithChoice(choiceGen, choiceWeight);

                    AssertError(gen);
                }
            }));
    }
}
