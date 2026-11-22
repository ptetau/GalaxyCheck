using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.ReflectedGenTests
{
    public class AboutGeneratingStructs
    {
        private struct EmptyStruct
        {
        }

        [Property]
        public Property ItCanGenerateAnEmptyStruct() => new Property(
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Create<EmptyStruct>();

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
            }));

        private struct StructWithOneProperty
        {
            public int Property { get; set; }

        }

        [Property]
        public Property ItCanGenerateAStructWithOneProperty() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Factory()
                    .RegisterType(GalaxyCheck.Gen.Int32().Where(x => x != 0))
                    .Create<StructWithOneProperty>();

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
                instance.Property.Should().NotBe(0);
            }));

        private struct StructWithTwoProperties
        {
            public int Property1 { get; set; }
            public int Property2 { get; set; }
        }

        [Property]
        public Property ItCanGenerateAStructWithTwoProperties() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Factory()
                    .RegisterType(GalaxyCheck.Gen.Int32().Where(x => x != 0))
                    .Create<StructWithTwoProperties>();

                var instance = gen.SampleOne(seed: 0);

                instance.Should().NotBeNull();
                instance.Property1.Should().NotBe(0);
                instance.Property2.Should().NotBe(0);
            }));
        
        
        private struct StructWithOneConstructorArgument
        {
            public StructWithOneConstructorArgument(int property)
            {
                Property = property;
                UsedConstructor = true;
            }

            public int Property { get; set; }
            
            public bool UsedConstructor { get; set; }
        }

        [Property]
        public Property ItCanGenerateAStructWithOneConstructorArgument() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Factory()
                    .RegisterType(GalaxyCheck.Gen.Int32().Where(x => x != 0))
                    .Create<StructWithOneConstructorArgument>();

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
                instance.Property.Should().NotBe(0);
                instance.UsedConstructor.Should().BeTrue();
            }));
    }
}
