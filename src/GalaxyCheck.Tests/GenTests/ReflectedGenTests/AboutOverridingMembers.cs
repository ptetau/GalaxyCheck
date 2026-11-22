using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;
using Xunit;
using static Tests.V2.DomainGenAttributes;

namespace Tests.V2.GenTests.ReflectedGenTests
{
    public class AboutOverridingMembers
    {
        private record RecordWithOneProperty(object Property);



        [Property]
        public Property ItCanOverrideAnExternalInitProperty() => new Property(
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Create<RecordWithOneProperty>()
                    .OverrideMember(x => x.Property, GalaxyCheck.Gen.Constant(value));

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
                instance.Property.Should().Be(value);
            }));

        private record RecordWithOneNestedProperty(RecordWithOneProperty Property);

        [Property]
        public Property ItCanOverrideANestedExternalInitProperty() => new Property(
            from value in DomainGen.Any()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Create<RecordWithOneNestedProperty>()
                    .OverrideMember(x => x.Property.Property, GalaxyCheck.Gen.Constant(value));

                var instance = gen.SampleOne(seed: seed, size: size);

                instance.Should().NotBeNull();
                instance.Property.Property.Should().Be(value);
            }));

        private record RecordWithOneProperty2(int Property);

        [Fact]
        public void ItErrorsWhenTryingToOverrideAMemberOfATypeRegisteredInTheFactory()
        {
            var genFactory = GalaxyCheck.Gen
                .Factory()
                .RegisterType(GalaxyCheck.Gen.Int32().Select(i => new RecordWithOneProperty2(1)));

            var gen = genFactory
                .Create<RecordWithOneProperty2>()
                .OverrideMember(x => x.Property, GalaxyCheck.Gen.Constant(2));

            Action action = () => gen.SampleOne();

            action.Should()
                .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                .WithGenErrorMessage("attempted to override expression 'x => x.Property' on type 'Tests.V2.GenTests.ReflectedGenTests.AboutOverridingMembers+RecordWithOneProperty2', but overriding members for registered types is not currently supported (GitHub issue: https://github.com/nth-commit/GalaxyCheck/issues/346)");
        }

        private record RecordWithAMethod(object Property)
        {
            public object Method() => null!;
        }

        [Property]
        public Property ItErrorsWhenExpressionIsNotMemberAccess() => new Property(
            from memberGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Create<RecordWithAMethod>()
                    .OverrideMember(x => x.Method(), memberGen);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("expression 'x => x.Method()' was invalid, an overriding expression may only contain member access");
            }));

        [Property]
        public Property ItErrorsWhenExpressionIsNotMemberAccess_AndACorrectOverrideWasSpecifiedAfterwards() => new Property(
            from memberGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Create<RecordWithAMethod>()
                    .OverrideMember(x => x.Method(), memberGen)
                    .OverrideMember(x => x.Property, memberGen);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("expression 'x => x.Method()' was invalid, an overriding expression may only contain member access");
            }));

        private class ClassWithNonDefaultConstructor
        {
            public object Property { get; }

            public ClassWithNonDefaultConstructor(object property)
            {
                Property = property;
            }
        }

        [Property(Skip = "Future validation")]
        public Property ItErrorsWhenMemberAccessIsCircumventedByConstructor() => new Property(
            from memberGen in DomainGen.Gen()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen
                    .Create<ClassWithNonDefaultConstructor>()
                    .OverrideMember(x => x.Property, memberGen);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("expression 'x => x.Property' targeted a valid member, but the type did not have a default constructor, so the member override would be ignored");
            }));
    }
}
