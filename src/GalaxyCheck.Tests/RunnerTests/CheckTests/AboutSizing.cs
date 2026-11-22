using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.RunnerTests.CheckTests
{
    public class AboutSizing
    {
        [Property]
        public Property ItFalsifiesAPropertyThatOnlyFalsifiesAtLargerSizes() => new Property(
            from iterations in DomainGen.Iterations(allowZero: false)
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var property = GalaxyCheck.Gen.Create(parameters => (parameters.Size.Value, parameters)).ForAll(x => x < 50);

                var result = property.Check(iterations: iterations, seed: seed);

                result.Falsified.Should().BeTrue();
            }));

        [Property]
        public Property ItFalsifiesAPropertyThatOnlyFalsifiesAtSmallerSizes() => new Property(
            from iterations in DomainGen.Iterations(allowZero: false)
            where iterations >= 2 // Otherwise the first iteration is 100
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var property = GalaxyCheck.Gen.Create(parameters => (parameters.Size.Value, parameters)).ForAll(x => x > 50);

                var result = property.Check(iterations: iterations, seed: seed);

                result.Falsified.Should().BeTrue();
            }));

        [Property]
        public Property ForAConstantSize_AndANonResizingGen_ItDoesNotResize() => new Property(
            from gen in DomainGen.Gen()
            from func in DomainGen.Predicate<object>()
            from iterations in DomainGen.Iterations()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var property = gen.ForAll(_ => true);

                var result = property.Check(iterations: iterations, seed: seed, size: size);

                result.NextParameters.Size.Value.Should().Be(size);
            }));
    }
}
