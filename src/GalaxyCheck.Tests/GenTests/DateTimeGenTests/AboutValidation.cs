using FluentAssertions;
using GalaxyCheck;
using System;
using System.Linq;

namespace Tests.V2.GenTests.DateTimeGenTests
{
    public class AboutValidation
    {
        [Property]
        public Property ItErrorsWhenFromIsAfterUntil() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            from fromDateTime in Gen.DateTime().From(DateTime.MinValue.AddMilliseconds(1))
            from untilDateTime in Gen.DateTime().Until(fromDateTime.AddMilliseconds(-1))
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.DateTime().From(fromDateTime).Until(untilDateTime);

                Action action = () => gen.SampleOne(seed: seed, size: size);

                action.Should()
                    .Throw<GalaxyCheck.Exceptions.GenErrorException>()
                    .WithGenErrorMessage("'from' datetime cannot be after 'until' datetime");
            }));
    }
}
