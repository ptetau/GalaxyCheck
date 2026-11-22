using FluentAssertions;
using GalaxyCheck;
using System.Collections.Generic;
using System.Linq;

namespace Tests.V2.GenTests.CharGenTests
{
    public class AboutDefaults
    {
        [Property]
        public Property TheDefaultMinimumIsTheMinimumInt32() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen0 = GalaxyCheck.Gen.Char();
                var gen1 = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.All);

                SampleTraversal(gen0, seed, size).Should().BeEquivalentTo(SampleTraversal(gen1, seed, size));
            }));

        private static List<char> SampleTraversal(GalaxyCheck.IGen<char> gen, int seed, int size) => gen.SampleOneTraversal(seed: seed, size: size)
            .Take(100)
            .ToList();
    }
}
