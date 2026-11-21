using FluentAssertions;
using GalaxyCheck;
using NebulaCheck;
using System.Linq;
using Property = NebulaCheck.Property;
using Test = NebulaCheck.Test;

namespace Tests.V2.GenTests.InfiniteGenTests
{
    public class AboutShrinking
    {
        [Property]
        public NebulaCheck.IGen<Test> WhenItIsNotEnumerated_ItWillNotShrink() =>
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select Property.ForThese(() =>
            {
                var gen = elementGen.InfiniteOf();

                var sample = gen.SampleOneTraversal(seed: seed, size: size);

                sample.Should().ContainSingle();
            });

        [Property]
        public NebulaCheck.IGen<Test> WhenItIsEnumeratedOnce_AndTheElementGeneratorDoesNotShrink_ItWillShrinkOnce() =>
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select Property.ForThese(() =>
            {
                var gen = elementGen.NoShrink().InfiniteOf().Select(enumerable =>
                {
                    enumerable.Take(1).ToList();
                    return enumerable;
                });

                var sample = gen.SampleOneTraversal(seed: seed, size: size);

                sample.Should().HaveCount(2);
            });


        [Property]
        public NebulaCheck.IGen<Test> WhenItIsEnumeratedOnce_ItWillShrinkToTheSmallestElementRepeated() =>
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select Property.ForThese(() =>
            {
                var elementMin = elementGen.Minimum(seed: seed, size: size);

                var infiniteGen = elementGen.InfiniteOf().Select(enumerable =>
                {
                    enumerable.Take(1).ToList();
                    return enumerable;
                });

                var minimum = infiniteGen.Minimum(seed: seed, size: size);
                
                minimum.Take(10).Should().BeEquivalentTo(Enumerable.Range(0, 10).Select(_ => elementMin));
            });
    }
}
