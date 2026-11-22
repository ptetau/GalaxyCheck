using FluentAssertions;
using GalaxyCheck;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Tests.V2.GenTests.ReflectedGenTests
{
    public class AboutGeneratingSets
    {
        [Property]
        public Property ItCanGenerateISets() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<ISet<int>> SampleTraversal(GalaxyCheck.IGen<ISet<int>> gen) =>
                    AboutGeneratingSets.SampleTraversal(gen, seed, size);

                var gen0 = GalaxyCheck.Gen.Create<ISet<int>>();
                var gen1 = GalaxyCheck.Gen.Int32().SetOf().Select(x => x.ToHashSet());

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property ItCanGenerateHashSets() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<HashSet<int>> SampleTraversal(GalaxyCheck.IGen<HashSet<int>> gen) =>
                    AboutGeneratingSets.SampleTraversal(gen, seed, size);

                var gen0 = GalaxyCheck.Gen.Create<HashSet<int>>();
                var gen1 = GalaxyCheck.Gen.Int32().SetOf().Select(x => x.ToHashSet());

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        [Property]
        public Property ItCanGenerateImmutableHashSets() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<ImmutableHashSet<int>> SampleTraversal(GalaxyCheck.IGen<ImmutableHashSet<int>> gen) =>
                    AboutGeneratingSets.SampleTraversal(gen, seed, size);

                var gen0 = GalaxyCheck.Gen.Create<ImmutableHashSet<int>>();
                var gen1 = GalaxyCheck.Gen.Int32().SetOf().Select(x => x.ToImmutableHashSet());

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        private static List<T> SampleTraversal<T>(GalaxyCheck.IGen<T> gen, int seed, int size) => gen.SampleOneTraversal(seed: seed, size: size);
    }
}
