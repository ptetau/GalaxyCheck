using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.InfiniteGenTests
{
    public class AboutShrinking
    {
        [Property]
        public Property WhenItIsNotEnumerated_ItWillNotShrink() => new Property(
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.InfiniteOf();

                var sample = gen.SampleOneTraversal(seed: seed, size: size);

                sample.Should().ContainSingle();
            }));

        [Property]
        public Property WhenItIsEnumeratedOnce_AndTheElementGeneratorDoesNotShrink_ItWillShrinkOnce() => new Property(
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = elementGen.NoShrink().InfiniteOf().Select(enumerable =>
                {
                    enumerable.Take(1).ToList();
                    return enumerable;
                });

                var sample = gen.SampleOneTraversal(seed: seed, size: size);

                sample.Should().HaveCount(2);
            }));


        [Property]
        public Property WhenItIsEnumeratedOnce_ItWillShrinkToTheSmallestElementRepeated() => new Property(
            from elementGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var elementMin = elementGen.Minimum(seed: seed, size: size);

                var infiniteGen = elementGen.InfiniteOf().Select(enumerable =>
                {
                    enumerable.Take(1).ToList();
                    return enumerable;
                });

                var minimum = infiniteGen.Minimum(seed: seed, size: size);
                
                minimum.Take(10).Should().BeEquivalentTo(Enumerable.Range(0, 10).Select(_ => elementMin));
            }));
    }
}
