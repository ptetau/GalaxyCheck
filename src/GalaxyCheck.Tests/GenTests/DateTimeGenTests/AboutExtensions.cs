using GalaxyCheck;
using GalaxyCheck.Gens;
using Moq;
using System;
using System.Linq;

namespace Tests.V2.GenTests.DateTimeGenTests
{
    public class AboutExtensions
    {
        [Property]
        public Property BetweenDefersToUnderlyingMethods() => new Property(
            from fromDate in Gen.DateTime()
            from untilDate in Gen.DateTime().From(fromDate)
            select TestProperty.ForThese(() =>
            {
                {
                    var mockGen = SetupMock();

                    mockGen.Object.Within(fromDate, untilDate);

                    mockGen.Verify(gen => gen.From(fromDate), Times.Once);
                    mockGen.Verify(gen => gen.Until(untilDate), Times.Once);
                }

                {
                    var mockGen = SetupMock();

                    mockGen.Object.Within(untilDate, fromDate);

                    mockGen.Verify(gen => gen.From(fromDate), Times.Once);
                    mockGen.Verify(gen => gen.Until(untilDate), Times.Once);
                }
            }));

        private static Mock<IDateTimeGen> SetupMock()
        {
            var mockGen = new Mock<IDateTimeGen>();
            mockGen.Setup((gen) => gen.From(It.IsAny<DateTime>())).Returns(mockGen.Object);
            mockGen.Setup((gen) => gen.Until(It.IsAny<DateTime>())).Returns(mockGen.Object);
            return mockGen;
        }
    }
}
