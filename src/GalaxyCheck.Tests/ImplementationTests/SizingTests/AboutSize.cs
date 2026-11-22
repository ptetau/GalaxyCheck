using GalaxyCheck;
using FluentAssertions;
using GalaxyCheck.Gens.Parameters;
using System;
using System.Linq;

namespace Tests.V2.ImplementationTests.SizingTests
{
    public class AboutSize
    {
        [Property]
        public Property ItContainsTheGivenValue() => new Property(
            from value in Gen.Int32().Between(0, 100)
            select TestProperty.ForThese(() =>
            {
                var size = new Size(value);

                size.Value.Should().Be(value);
            }));

        [Property]
        public Property IfValueIsNegative_ItThrows() => new Property(
            from value in Gen.Int32().LessThan(0)
            select TestProperty.ForThese(() =>
            {
                Action action = () => new Size(value);

                action.Should()
                    .Throw<ArgumentOutOfRangeException>()
                    .WithMessage("'value' must be between 0 and 100 (Parameter 'value')");
            }));

        [Property]
        public Property IfValueIsGreaterThanOneHundred_ItThrows() => new Property(
            from value in Gen.Int32().GreaterThan(100)
            select TestProperty.ForThese(() =>
            {
                Action action = () => new Size(value);

                action.Should()
                    .Throw<ArgumentOutOfRangeException>()
                    .WithMessage("'value' must be between 0 and 100 (Parameter 'value')");
            }));
    }
}
