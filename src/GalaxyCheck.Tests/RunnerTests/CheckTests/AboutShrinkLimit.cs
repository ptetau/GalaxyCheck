using FluentAssertions;
using System.Collections.Generic;
using GalaxyCheck;
using GalaxyCheck.Runners.Check;
using NebulaCheck;
using System.Linq;
using Xunit;
using Gen = NebulaCheck.Gen;
using Property = NebulaCheck.Property;
using Test = NebulaCheck.Test;

namespace Tests.V2.RunnerTests.CheckTests
{
    public class AboutShrinkLimit
    {
        // Each of the 1000 elements can shrink, so the generator has more shrinks than each limit.
        private static readonly GalaxyCheck.IGen<IReadOnlyList<int>> InfinitelyShrinkableGen =
            GalaxyCheck.Gen.Int32().GreaterThanEqual(1).WithBias(GalaxyCheck.Gen.Bias.None).ListOf().WithCount(1000);

        [Property]
        public NebulaCheck.IGen<Test> ItShrinksToTheGivenLimit() =>
            from shrinkLimit in Gen.Int32().Between(0, 100)
            from seed in DomainGen.Seed()
            select Property.ForThese(() =>
            {
                var property = InfinitelyShrinkableGen.ForAll(_ => false);

                var result = property.Check(seed: seed, shrinkLimit: shrinkLimit);

                result.Shrinks.Should().Be(shrinkLimit);
                result.TerminationReason.Should().Be(TerminationReason.ReachedShrinkLimit);
            });

        [Property]
        public NebulaCheck.IGen<Test> IfTheShrinkLimitIsZero_ThePropertyCanStillBeFalsified() =>
            from gen in DomainGen.Gen()
            from shrinkLimit in Gen.Int32().LessThanEqual(0)
            from seed in DomainGen.Seed()
            select Property.ForThese(() =>
            {
                var property = gen.ForAll(_ => false);

                var result = property.Check(seed: seed, shrinkLimit: shrinkLimit);

                result.Falsified.Should().BeTrue();
            });

        [Fact]
        public void TheDefaultShrinkLimitIsFiveHundred()
        {
            var property = InfinitelyShrinkableGen.ForAll(_ => false);

            var result = property.Check(seed: 0);

            result.Shrinks.Should().Be(500);
        }
    }
}
