using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.ReflectedGenTests
{
    public class AboutGeneratingRecords
    {
        private record EmptyRecord();

        [Property]
        public Property ItCanGenerateAnEmptyRecord() => new Property(
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Create<EmptyRecord>();

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
            }));

        private record RecordWithOneProperty(int Property);

        [Property]
        public Property ItCanGenerateARecordWithOneProperty() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Factory()
                    .RegisterType(GalaxyCheck.Gen.Int32().Where(x => x != 0))
                    .Create<RecordWithOneProperty>();

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
                instance.Property.Should().NotBe(0);
            }));

        private record RecordWithTwoProperties(int Property1, int Property2);

        [Property]
        public Property ItCanGenerateARecordWithTwoProperties() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Factory()
                    .RegisterType(GalaxyCheck.Gen.Int32().Where(x => x != 0))
                    .Create<RecordWithTwoProperties>();

                var instance = gen.SampleOne(seed: 0);

                instance.Should().NotBeNull();
                instance.Property1.Should().NotBe(0);
                instance.Property2.Should().NotBe(0);
            }));
    }
}
