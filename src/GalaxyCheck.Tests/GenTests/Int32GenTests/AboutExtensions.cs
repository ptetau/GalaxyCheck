using FluentAssertions;
using GalaxyCheck;
using GalaxyCheck.Gens;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Tests.V2.GenTests.Int32GenTests
{
    public class AboutExtensions
    {
        [Property]
        public Property BetweenDefersToUnderlyingMethods() => new Property(
            from min in Gen.Int32()
            from max in Gen.Int32().GreaterThanEqual(min)
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
            from minExclusive in Gen.Int32().LessThan(int.MaxValue)
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

                var gen = mockGen.Object.GreaterThan(int.MaxValue);

                mockGen.Verify(gen0 => gen0.GreaterThanEqual(It.IsAny<int>()), Times.Never);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("Arithmetic operation resulted in an overflow.");
            }));

        [Property]
        public Property LessThanDefersToUnderlyingMethod() => new Property(
            from maxExclusive in Gen.Int32().GreaterThan(int.MinValue)
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

                var gen = mockGen.Object.LessThan(int.MinValue);

                mockGen.Verify(gen => gen.LessThanEqual(It.IsAny<int>()), Times.Never);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("Arithmetic operation resulted in an overflow.");
            }));

        private static Mock<IIntGen<int>> SetupMock()
        {
            var mockGen = new Mock<IIntGen<int>>();
            mockGen.Setup((gen) => gen.GreaterThanEqual(It.IsAny<int>())).Returns(mockGen.Object);
            mockGen.Setup((gen) => gen.LessThanEqual(It.IsAny<int>())).Returns(mockGen.Object);
            return mockGen;
        }
    }
}
