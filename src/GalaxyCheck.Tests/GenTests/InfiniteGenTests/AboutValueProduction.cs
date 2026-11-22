using FluentAssertions;
using GalaxyCheck;
using System.Collections.Generic;
using System.Linq;

namespace Tests.V2.GenTests.InfiniteGenTests
{
    public class AboutValueProduction
    {
        [Property]
        public Property ItProducesValuesDeterministically() => new Property(
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.InfiniteOf();

                var sample = gen.SampleOne(seed: seed, size: size);

                sample.Take(10).Should().BeEquivalentTo(sample.Take(10));
            }));

    }
}
