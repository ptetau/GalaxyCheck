using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.RunnerTests.CheckTests
{
    public class AboutDeepCheck
    {
        [Property]
        public Property IfDeepCheckIsEnabled_ForAnEasilyFalsifiableProperty_ItIsNotLimitedByDeepCheckDisabled() => new Property(
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var property = GalaxyCheck.Gen.Int32().ForAll(x => x < 1);

                var result = property.Check(seed: seed, deepCheck: true);

                result.TerminationReason.Should().NotBe(GalaxyCheck.Runners.Check.TerminationReason.DeepCheckDisabled);
            }));

        [Property]
        public Property IfDeepCheckIsDisabled_ForAnEasilyFalsifiableProperty_ItIsLimitedByDeepCheckDisabled() => new Property(
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var property = GalaxyCheck.Gen.Int32().ForAll(x => x < 1);

                var result = property.Check(seed: seed, deepCheck: false);

                result.TerminationReason.Should().Be(GalaxyCheck.Runners.Check.TerminationReason.DeepCheckDisabled);
            }));

        [Property]
        public Property DefaultDeepCheckStateIsEnabled() => new Property(
            from seed in DomainGen.Seed()
            select TestProperty.ForThese(() =>
            {
                var property = GalaxyCheck.Gen.Int32().ForAll(x => x < 1);

                var result0 = property.Check(seed: seed);
                var result1 = property.Check(seed: seed, deepCheck: true);

                result0.Counterexample!.Replay.Should().BeEquivalentTo(result1.Counterexample!.Replay);
                result0.Iterations.Should().Be(result1.Iterations);
            }));

    }
}
