using FluentAssertions;
using GalaxyCheck;
using System.Collections.Generic;
using System.Linq;

namespace Tests.V2.GenTests.Int64GenTests
{
    public class AboutDefaults
    {
        [Property]
        public Property TheDefaultMinimumIsTheMinimumInt64() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<long> SampleTraversal(GalaxyCheck.IGen<long> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int64();
                var gen1 = gen0.GreaterThanEqual(long.MinValue);

                SampleTraversal(gen0).Should().BeEquivalentTo(SampleTraversal(gen1));
            }));

        [Property]
        public Property TheDefaultMaximumIsTheMaximumInt64() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<long> SampleTraversal(GalaxyCheck.IGen<long> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int64();
                var gen1 = gen0.LessThanEqual(long.MaxValue);

                SampleTraversal(gen0).Should().BeEquivalentTo(SampleTraversal(gen1));
            }));

        [Property]
        public Property IfTheRangeSpansZero_TheDefaultOriginIsZero() => new Property(
            from min in Gen.Int64().LessThanEqual(0)
            from max in Gen.Int64().GreaterThanEqual(0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<long> SampleTraversal(GalaxyCheck.IGen<long> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int64().GreaterThanEqual(min).LessThanEqual(max);
                var gen1 = gen0.ShrinkTowards(0);

                SampleTraversal(gen0).Should().BeEquivalentTo(SampleTraversal(gen1));
            }));

        [Property]
        public Property IfTheRangeIsPositive_TheDefaultOriginIsTheMinimum_BecauseItIsClosestToZero() => new Property(
            from min in Gen.Int64().GreaterThanEqual(0)
            from max in Gen.Int64().GreaterThanEqual(min)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<long> SampleTraversal(GalaxyCheck.IGen<long> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int64().GreaterThanEqual(min).LessThanEqual(max);
                var gen1 = gen0.ShrinkTowards(min);

                SampleTraversal(gen0).Should().BeEquivalentTo(SampleTraversal(gen1));
            }));

        [Property]
        public Property IfTheRangeIsNegative_TheDefaultOriginIsTheMaximum_BecauseItIsClosestToZero() => new Property(
            from min in Gen.Int64().LessThanEqual(0)
            from max in Gen.Int64().Between(min, 0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<long> SampleTraversal(GalaxyCheck.IGen<long> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int64().GreaterThanEqual(min).LessThanEqual(max);
                var gen1 = gen0.ShrinkTowards(max);

                SampleTraversal(gen0).Should().BeEquivalentTo(SampleTraversal(gen1));
            }));

        [Property]
        public Property TheDefaultBiasIsWithSize() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<long> SampleTraversal(GalaxyCheck.IGen<long> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int64();
                var gen1 = gen0.WithBias(GalaxyCheck.Gen.Bias.WithSize);

                SampleTraversal(gen0).Should().BeEquivalentTo(SampleTraversal(gen1));
            }));
    }
}
