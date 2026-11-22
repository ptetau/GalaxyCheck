using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.RunnerTests.CheckTests
{
    public class AboutIterations
    {
        [Property]
        public Property IfThePropertyIsInfallible_ItCallsTheTestFunctionForEachIteration() => new Property(
            from gen in DomainGen.Gen()
            from iterations in DomainGen.Iterations()
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var numberOfCalls = 0;
                var property = gen.ForAll((_) =>
                {
                    numberOfCalls++;
                    return true;
                });

                property.Check(iterations: iterations, seed: seed, exhaustive: false);

                numberOfCalls.Should().Be(iterations);
            }));

        [Property]
        public Property IfThePropertyIsInfallible_ItReturnsTheGivenIterations() => new Property(
            from gen in DomainGen.Gen()
            from iterations in DomainGen.Iterations()
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
             {
                 var property = gen.ForAll((_) => true);

                 var result = property.Check(iterations: iterations, seed: seed, exhaustive: false);

                 result.Iterations.Should().Be(iterations);
             }));
    }
}
