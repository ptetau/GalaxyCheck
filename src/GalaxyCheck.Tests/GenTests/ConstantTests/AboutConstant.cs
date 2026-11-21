using FluentAssertions;
using GalaxyCheck;
using NebulaCheck;
using System.Linq;
using Property = NebulaCheck.Property;
using Test = NebulaCheck.Test;

namespace Tests.V2.GenTests.ConstantTests
{
    public class AboutConstant
    {
        [Property]
        public NebulaCheck.IGen<Test> ItProducesTheGivenValue() =>
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select Property.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Constant(value);

                var traversal = gen.SampleOneTraversal(seed: seed, size: size);

                traversal
                    .Should().ContainSingle()
                    .Subject.Should().Be(value);
            });

        [Property]
        public NebulaCheck.IGen<Test> ItDoesNotConsumeRandomness() =>
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select Property.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Constant(value);

                var sample = gen.Advanced.SampleOneWithMetrics(seed: seed, size: size);

                sample.RandomnessConsumption.Should().Be(0);
            });
    }
}
