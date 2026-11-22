using FluentAssertions;
using GalaxyCheck;
using System.Collections.Generic;
using System.Linq;

namespace Tests.V2.GenTests.Int32GenTests
{
    public class AboutDefaults
    {
        [Property]
        public Property TheDefaultMinimumIsTheMinimumInt32() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<int> SampleTraversal(GalaxyCheck.IGen<int> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int32();
                var gen1 = gen0.GreaterThanEqual(int.MinValue);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property TheDefaultMaximumIsTheMaximumInt32() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<int> SampleTraversal(GalaxyCheck.IGen<int> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int32();
                var gen1 = gen0.LessThanEqual(int.MaxValue);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property IfTheRangeSpansZero_TheDefaultOriginIsZero() => new Property(
            from min in Gen.Int32().LessThanEqual(0)
            from max in Gen.Int32().GreaterThanEqual(0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<int> SampleTraversal(GalaxyCheck.IGen<int> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int32().GreaterThanEqual(min).LessThanEqual(max);
                var gen1 = gen0.ShrinkTowards(0);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property IfTheRangeIsPositive_TheDefaultOriginIsTheMinimum_BecauseItIsClosestToZero() => new Property(
            from min in Gen.Int32().GreaterThanEqual(0)
            from max in Gen.Int32().GreaterThanEqual(min)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<int> SampleTraversal(GalaxyCheck.IGen<int> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int32().GreaterThanEqual(min).LessThanEqual(max);
                var gen1 = gen0.ShrinkTowards(min);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property IfTheRangeIsNegative_TheDefaultOriginIsTheMaximum_BecauseItIsClosestToZero() => new Property(
            from min in Gen.Int32().LessThanEqual(0)
            from max in Gen.Int32().Between(min, 0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<int> SampleTraversal(GalaxyCheck.IGen<int> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int32().GreaterThanEqual(min).LessThanEqual(max);
                var gen1 = gen0.ShrinkTowards(max);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property TheDefaultBiasIsWithSize() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<int> SampleTraversal(GalaxyCheck.IGen<int> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int32();
                var gen1 = gen0.WithBias(GalaxyCheck.Gen.Bias.WithSize);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));
    }
}
