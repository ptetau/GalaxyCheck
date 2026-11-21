using FluentAssertions;
using GalaxyCheck;
using GalaxyCheck.Gens;
using Moq;
using System;
using System.Linq;

namespace Tests.V2.GenTests.Int64GenTests
{
    public class AboutExtensions
    {
        [Property]
        public Property BetweenDefersToUnderlyingMethods() => new Property(
            from min in Gen.Int64()
            from max in Gen.Int64().GreaterThanEqual(min)
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
            from minExclusive in Gen.Int64().LessThan(long.MaxValue)
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                mockGen.Object.GreaterThan(minExclusive);

                mockGen.Verify(gen => gen.GreaterThanEqual(minExclusive + 1), Times.Once);
            }));

        [Property]
        public Property GreaterThanErrorsAtMaxValue() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                var gen = mockGen.Object.GreaterThan(long.MaxValue);

                mockGen.Verify(gen => gen.GreaterThanEqual(It.IsAny<long>()), Times.Never);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("Arithmetic operation resulted in an overflow.");
            }));

        [Property]
        public Property LessThanDefersToUnderlyingMethod() => new Property(
            from maxExclusive in Gen.Int64().GreaterThan(long.MinValue)
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                mockGen.Object.LessThan(maxExclusive);

                mockGen.Verify(gen => gen.LessThanEqual(maxExclusive - 1), Times.Once);
            }));

        [Property]
        public Property LessThanErrorsAtMinValue() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                var gen = mockGen.Object.LessThan(long.MinValue);

                mockGen.Verify(gen => gen.LessThanEqual(It.IsAny<long>()), Times.Never);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("Arithmetic operation resulted in an overflow.");
            }));

        private static Mock<IIntGen<long>> SetupMock()
        {
            var mockGen = new Mock<IIntGen<long>>();
            mockGen.Setup((gen) => gen.GreaterThanEqual(It.IsAny<long>())).Returns(mockGen.Object);
            mockGen.Setup((gen) => gen.LessThanEqual(It.IsAny<long>())).Returns(mockGen.Object);
            return mockGen;
        }
    }
}
