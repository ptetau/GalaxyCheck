using FluentAssertions;
using GalaxyCheck;
using System.Collections.Generic;
using System.Linq;

namespace Tests.V2.GenTests.ReflectedGenTests
{
    public class AboutGeneratingArrays
    {
        [Property]
        public Property ItCanGenerateArrays() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                List<int[]> SampleTraversal(GalaxyCheck.IGen<int[]> gen) =>
                    AboutGeneratingArrays.SampleTraversal(gen, seed, size);

                var gen0 = GalaxyCheck.Gen.Create<int[]>();
                var gen1 = GalaxyCheck.Gen.Int32().ListOf().Select(x => x.ToArray());

                var sample0 = SampleTraversal(gen0);
                var sample1 = SampleTraversal(gen1);

                sample0.Should().BeEquivalentTo(sample1);
            }));

        private static List<T> SampleTraversal<T>(GalaxyCheck.IGen<T> gen, int seed, int size) => gen.SampleOneTraversal(seed: seed, size: size)
            .Take(100)
            .ToList();
    }
}
