using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;

namespace Tests.V2.GenTests.ReflectedGenTests
{
    public class AboutGeneratingDeepObjects
    {
        private class ClassWithOneProperty
        {
            public object Property { get; set; } = null!;
        }

        private class ClassWithOneNestedProperty
        {
            public ClassWithOneProperty Property { get; set; } = null!;
        }   

        [Property]
        public Property ItCanGenerateAnObjectWithANestedProperty() => new Property(
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Create<ClassWithOneNestedProperty>()
                    .OverrideMember(x => x.Property.Property, GalaxyCheck.Gen.Constant(value));

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
                instance.Property.Should().NotBeNull();
                instance.Property.Property.Should().Be(value);
            }));

        private class ClassWithRecursiveProperty
        {
            public ClassWithRecursiveProperty Property { get; set; } = null!;
        }

        [Property]
        public Property IfTheClassHasARecursiveProperty_ItErrors() => new Property(
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Create<ClassWithRecursiveProperty>();

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("detected circular reference on type '*ClassWithRecursiveProperty' at path '$.Property'");
            }));
    }
}
