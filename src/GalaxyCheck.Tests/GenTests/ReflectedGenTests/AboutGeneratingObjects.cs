using FluentAssertions;
using GalaxyCheck;
using System.Linq;
using Xunit;

namespace Tests.V2.GenTests.ReflectedGenTests
{
    public class AboutGeneratingObjects
    {
        private class EmptyClass { }

        [Fact]
        public void ItCanGenerateAnEmptyObject()
        {
            var gen = GalaxyCheck.Gen.Create<EmptyClass>();

            var instance = gen.SampleOne(seed: 0);

            instance.Should().NotBeNull();
        }

        private class ClassWithOneProperty
        {
            public int Property { get; set; }
        }

        [Property]
        public Property ItCanGenerateAnObjectWithAProperty() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Factory()
                    .RegisterType(GalaxyCheck.Gen.Int32().Where(x => x != 0))
                    .Create<ClassWithOneProperty>();

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
                instance.Property.Should().NotBe(0);
            }));

        private class ClassWithTwoProperties
        {
            public int Property1 { get; set; }

            public int Property2 { get; set; }
        }

        [Property]
        public Property ItCanGenerateAnObjectWithTwoProperties() => new Property(
            from value1 in DomainGen.Any()
            from value2 in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Factory()
                    .RegisterType(GalaxyCheck.Gen.Int32().Where(x => x != 0))
                    .Create<ClassWithTwoProperties>();

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
                instance.Property1.Should().NotBe(0);
                instance.Property2.Should().NotBe(0);
            }));

        private class ClassWithOneField
        {
#pragma warning disable CS0649
            public int Field;
#pragma warning restore CS0649
        }

        [Property]
        public Property ItCanGenerateAnObjectWithAField() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Factory()
                    .RegisterType(GalaxyCheck.Gen.Int32().Where(x => x != 0))
                    .Create<ClassWithOneField>();

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
                instance.Field.Should().NotBe(0);
            }));

        private class ClassWithOneReadOnlyProperty
        {
            public int Property { get; } = 0;
        }

        [Fact]
        public void ItAvoidsGenerationOfReadOnlyProperties()
        {
            var gen = GalaxyCheck.Gen.Create<ClassWithOneReadOnlyProperty>();

            var instance = gen.SampleOne(seed: 0);

            instance.Should().NotBeNull();
        }

        private class ClassWithOnePropertyWithPrivateSetter
        {
            public int Property { get; private set; } = 0;
        }

        [Fact]
        public void ItAvoidsGenerationOfPropertiesWithPrivateSetter()
        {
            var gen = GalaxyCheck.Gen.Create<ClassWithOnePropertyWithPrivateSetter>();

            var instance = gen.SampleOne(seed: 0);

            instance.Should().NotBeNull();
        }
    }
}
