using GalaxyCheck;
using GalaxyCheck.Gens;
using Moq;
using System.Linq;

namespace Tests.V2.GenTests.ListGenTests
{
    public class AboutExtensions
    {
        [Property]
        public Property WithCountBetweenDefersToUnderlyingMethods() => new Property(
            from minCount in Gen.Int32().LessThanEqual(20)
            from maxCount in Gen.Int32().GreaterThanEqual(minCount)
            select TestProperty.ForThese(() =>
            {
                {
                    var mockGen = SetupMock();

                    mockGen.Object.WithCountBetween(minCount, maxCount);

                    mockGen.Verify(gen => gen.WithCountGreaterThanEqual(minCount), Times.Once);
                    mockGen.Verify(gen => gen.WithCountLessThanEqual(maxCount), Times.Once);
                }

                {
                    var mockGen = SetupMock();

                    mockGen.Object.WithCountBetween(maxCount, minCount);

                    mockGen.Verify(gen => gen.WithCountGreaterThanEqual(minCount), Times.Once);
                    mockGen.Verify(gen => gen.WithCountLessThanEqual(maxCount), Times.Once);
                }
            }));

        [Property]
        public Property GreaterThanCountDefersToUnderlyingMethod() => new Property(
            from minCountExclusive in Gen.Int32()
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                mockGen.Object.WithCountGreaterThan(minCountExclusive);

                mockGen.Verify(gen => gen.WithCountGreaterThanEqual(minCountExclusive + 1), Times.Once);
            }));

        [Property]
        public Property LessThanCountDefersToUnderlyingMethod() => new Property(
            from maxCountExclusive in Gen.Int32()
            select TestProperty.ForThese(() =>
            {
                var mockGen = SetupMock();

                mockGen.Object.WithCountLessThan(maxCountExclusive);

                mockGen.Verify(gen => gen.WithCountLessThanEqual(maxCountExclusive - 1), Times.Once);
            }));

        private static Mock<IListGen<object>> SetupMock()
        {
            var mockGen = new Mock<IListGen<object>>();
            mockGen.Setup((gen) => gen.WithCountGreaterThanEqual(It.IsAny<int>())).Returns(mockGen.Object);
            mockGen.Setup((gen) => gen.WithCountLessThanEqual(It.IsAny<int>())).Returns(mockGen.Object);
            return mockGen;
        }
    }
}
