using FluentAssertions;
using GalaxyCheck;
using System.Collections.Generic;
using System.Linq;

namespace Tests.V2.GenTests.Int16GenTests
{
    public class AboutDefaults
    {
        [Property]
        public Property TheDefaultMinimumIsTheMinimumInt16() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<short> SampleTraversal(GalaxyCheck.IGen<short> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int16();
                var gen1 = gen0.GreaterThanEqual(short.MinValue);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property TheDefaultMaximumIsTheMaximumInt16() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<short> SampleTraversal(GalaxyCheck.IGen<short> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int16();
                var gen1 = gen0.LessThanEqual(short.MaxValue);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property IfTheRangeSpansZero_TheDefaultOriginIsZero() => new Property(
            from min in Gen.Int16().LessThanEqual(0)
            from max in Gen.Int16().GreaterThanEqual(0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<short> SampleTraversal(GalaxyCheck.IGen<short> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int16().GreaterThanEqual(min).LessThanEqual(max);
                var gen1 = gen0.ShrinkTowards(0);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property IfTheRangeIsPositive_TheDefaultOriginIsTheMinimum_BecauseItIsClosestToZero() => new Property(
            from min in Gen.Int16().GreaterThanEqual(0)
            from max in Gen.Int16().GreaterThanEqual(min)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<short> SampleTraversal(GalaxyCheck.IGen<short> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int16().GreaterThanEqual(min).LessThanEqual(max);
                var gen1 = gen0.ShrinkTowards(min);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property IfTheRangeIsNegative_TheDefaultOriginIsTheMaximum_BecauseItIsClosestToZero() => new Property(
            from min in Gen.Int16().LessThanEqual(0)
            from max in Gen.Int16().Between(min, 0)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<short> SampleTraversal(GalaxyCheck.IGen<short> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int16().GreaterThanEqual(min).LessThanEqual(max);
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
                List<short> SampleTraversal(GalaxyCheck.IGen<short> gen) => gen.SampleOneTraversal(seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.Int16();
                var gen1 = gen0.WithBias(GalaxyCheck.Gen.Bias.WithSize);

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));
    }
}
