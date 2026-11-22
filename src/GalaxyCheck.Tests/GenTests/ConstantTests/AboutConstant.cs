using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.ConstantTests
{
    public class AboutConstant
    {
        [Property]
        public Property ItProducesTheGivenValue() => new Property(
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Constant(value);

                var traversal = gen.SampleOneTraversal(seed: seed, size: size);

                traversal
                    .Should().ContainSingle()
                    .Subject.Should().Be(value);
            }));

        [Property]
        public Property ItDoesNotConsumeRandomness() => new Property(
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Constant(value);

                var sample = gen.Advanced.SampleOneWithMetrics(seed: seed, size: size);

                sample.RandomnessConsumption.Should().Be(0);
            }));
    }
}
