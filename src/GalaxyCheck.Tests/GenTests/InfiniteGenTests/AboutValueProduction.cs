using FluentAssertions;
using GalaxyCheck;
using NebulaCheck;
using System.Collections.Generic;
using System.Linq;
using Gen = NebulaCheck.Gen;
using Property = NebulaCheck.Property;
using Test = NebulaCheck.Test;

namespace Tests.V2.GenTests.InfiniteGenTests
{
    public class AboutValueProduction
    {
        [Property]
        public NebulaCheck.IGen<Test> ItProducesValuesDeterministically() =>
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select Property.ForThese(() =>
            {
                var gen = elementGen.InfiniteOf();

                var sample = gen.SampleOne(seed: seed, size: size);

                sample.Take(10).Should().BeEquivalentTo(sample.Take(10));
            });

    }
}
