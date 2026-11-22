using FluentAssertions;
using GalaxyCheck;
using GalaxyCheck.Gens;
using Moq;
using System;
using System.Linq;

namespace Tests.V2.GenTests.Int16GenTests
{
    public class AboutExtensions
    {
        [Property]
        public Property BetweenDefersToUnderlyingMethods() => new Property(
            from min in Gen.Int16()
            from max in Gen.Int16().GreaterThanEqual(min)
            select TestProperty.ForThese(() =>
            {
                {
                    var mockGen = SetupMock();

                    mockGen.Object.Between(min, max);

                    mockGen.Verify(gen => gen.GreaterThanEqual(min), Times.Once);
                    mockGen.Verify(gen => gen.LessThanEqual(max), Times.Once);
                }

                {
                    var mockGen = SetupMock();

                    mockGen.Object.Between(max, min);

                    mockGen.Verify(gen => gen.GreaterThanEqual(min), Times.Once);
                    mockGen.Verify(gen => gen.LessThanEqual(max), Times.Once);
                }
            }));

        [Property]
        public Property GreaterThanDefersToUnderlyingMethod() => new Property(
            from minExclusive in Gen.Int16().LessThan(short.MaxValue)
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                mockGen.Object.GreaterThan(minExclusive);

                mockGen.Verify(gen => gen.GreaterThanEqual((short)(minExclusive + 1)), Times.Once);
            }));

        [Property]
        public Property GreaterThanErrorsAtMaxValue() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                var gen = mockGen.Object.GreaterThan(short.MaxValue);

                mockGen.Verify(gen => gen.GreaterThanEqual(It.IsAny<short>()), Times.Never);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("Arithmetic operation resulted in an overflow.");
            }));

        [Property]
        public Property LessThanDefersToUnderlyingMethod() => new Property(
            from maxExclusive in Gen.Int16().GreaterThan(short.MinValue)
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                mockGen.Object.LessThan(maxExclusive);

                mockGen.Verify(gen => gen.LessThanEqual((short)(maxExclusive - 1)), Times.Once);
            }));

        [Property]
        public Property LessThanErrorsAtMinValue() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                var gen = mockGen.Object.LessThan(short.MinValue);

                mockGen.Verify(gen => gen.LessThanEqual(It.IsAny<short>()), Times.Never);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("Arithmetic operation resulted in an overflow.");
            }));

        private static Mock<IIntGen<short>> SetupMock()
        {
            var mockGen = new Mock<IIntGen<short>>();
            mockGen.Setup((gen) => gen.GreaterThanEqual(It.IsAny<short>())).Returns(mockGen.Object);
            mockGen.Setup((gen) => gen.LessThanEqual(It.IsAny<short>())).Returns(mockGen.Object);
            return mockGen;
        }
    }
}
