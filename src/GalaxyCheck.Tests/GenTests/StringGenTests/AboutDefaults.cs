using FluentAssertions;
using GalaxyCheck;
using System.Collections.Generic;
using System.Linq;

namespace Tests.V2.GenTests.StringGenTests
{
    public class AboutDefaults
    {
        [Property]
        public Property TheDefaultMinimumLengthIsZero() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<string> SampleTraversal(GalaxyCheck.IGen<string> gen) =>
                    AboutDefaults.SampleTraversal(gen, seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.String();
                var gen1 = gen0.WithLengthGreaterThanEqual(0);

                SampleTraversal(gen0).Should().BeEquivalentTo(SampleTraversal(gen1));
            }));

        [Property]
        public Property TheDefaultMaximumLengthIsOneHundred() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<string> SampleTraversal(GalaxyCheck.IGen<string> gen) =>
                    AboutDefaults.SampleTraversal(gen, seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.String();
                var gen1 = gen0.WithLengthLessThanEqual(100);

                SampleTraversal(gen0).Should().BeEquivalentTo(SampleTraversal(gen1));
            }));

        [Property]
        public Property TheDefaultLengthBiasIsWithSize() => new Property(
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<string> SampleTraversal(GalaxyCheck.IGen<string> gen) =>
                    AboutDefaults.SampleTraversal(gen, seed: seed, size: size);

                var gen0 = GalaxyCheck.Gen.String();
                var gen1 = gen0.WithLengthBias(GalaxyCheck.Gen.Bias.WithSize);

                SampleTraversal(gen0).Should().BeEquivalentTo(SampleTraversal(gen1));
            }));

        private static List<string> SampleTraversal(GalaxyCheck.IGen<string> gen, int seed, int size) => gen.SampleOneTraversal(seed: seed, size: size)
            .Take(100)
            .ToList();
    }
}
