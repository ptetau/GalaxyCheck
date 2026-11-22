using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;

namespace Tests.V2.GenTests.InfiniteGenTests
{
    public class AboutExhaustion
    {
        [Property(Iterations = 10)]
        public Property ItExhaustsWithAnImpossiblePredicate() => new Property(
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.Where(_ => false).InfiniteOf();

                Action test = () => gen.SampleOne(seed: seed, size: size).Take(1).ToList();

                test.Should().Throw<GalaxyCheck.Exceptions.GenExhaustionException>();
            }));
    }
}
