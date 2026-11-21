using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;
using Xunit;

namespace Tests.V2.GenTests.ReflectedGenTests
{
    public class AboutExceptionsDuringObjectCreation
    {
        private class ClassWithDefaultThrowingConstructor
        {
            public ClassWithDefaultThrowingConstructor()
            {
                throw new Exception("Test exception");
            }
        }

        [Property]
        public Property ItErrorsWhenTheDefaultConstructorThrowsAnException() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Create<ClassWithDefaultThrowingConstructor>();

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'System.Exception' was thrown while calling constructor with message 'Test exception'");
            }));

        private class ClassWithNonDefaultThrowingConstructor
        {
            public ClassWithNonDefaultThrowingConstructor(int x)
            {
                throw new Exception("Test exception");
            }
        }

        [Property]
        public Property ItErrorsWhenTheNonDefaultConstructorThrowsAnException() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Create<ClassWithNonDefaultThrowingConstructor>();

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'System.Exception' was thrown while calling constructor with message 'Test exception'");
            }));

        private class ClassWithThrowingPropertySetter
        {
            public int MyProperty {
                get
                {
                    return 0;
                }
                set
                {
                    throw new Exception("Test exception");
                }
            }

            public ClassWithThrowingPropertySetter()
            {
            }
        }

        [Property]
        public Property ItErrorsWhenAPropertySetterThrowsAnException() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Create<ClassWithThrowingPropertySetter>();

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'System.Exception' was thrown while setting property with message 'Test exception'");
            }));
    }
}
