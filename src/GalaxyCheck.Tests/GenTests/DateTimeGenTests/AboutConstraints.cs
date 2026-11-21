using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;
using static Tests.V2.DomainGenAttributes;

namespace Tests.V2.GenTests.DateTimeGenTests
{
    public class AboutConstraints
    {
        [Property]
        public void ItProducesValuesFromTheGivenDate(
            [Seed] int seed,
            [Size] int size,
            DateTime dateTime)
        {
            var gen = GalaxyCheck.Gen.DateTime().From(dateTime);

            var sample = gen.SampleOneTraversal(seed: seed, size: size);

            sample.Should().OnlyContain(dt => dt >= dateTime);
        }

        [Property]
        public void ItProducesValuesToTheGivenDate(
            [Seed] int seed,
            [Size] int size,
            DateTime dateTime)
        {
            var gen = GalaxyCheck.Gen.DateTime().Until(dateTime);

            var sample = gen.SampleOneTraversal(seed: seed, size: size);

            sample.Should().OnlyContain(dt => dt <= dateTime);
        }

        [Property]
        public Property ItProducesValuesWithinTheGivenDates() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            from fromDateTime in Gen.DateTime()
            from untilDateTime in Gen.DateTime().From(fromDateTime)
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.DateTime().From(fromDateTime).Until(untilDateTime);

                var sample = gen.SampleOneTraversal(seed: seed, size: size);

                sample.Should().OnlyContain(dt => fromDateTime <= dt && dt <= untilDateTime);
            }));

    }
}
