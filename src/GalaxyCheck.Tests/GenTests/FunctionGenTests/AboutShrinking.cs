using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.FunctionGenTests
{
    public class AboutShrinking
    {
        [Property]
        public Property IfTheImageContainsTheTargetReturnValue_ItShrinksToTheFunctionWhereTheImageIsTheTargetReturnValue() => new Property(
            from returnValues in Gen.Int32().ListOf().WithCount(10)
            from targetReturnValue in Gen.Element(returnValues.AsEnumerable())
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var variables = Enumerable.Range(0, 10);
                var gen = GalaxyCheck.Gen.Int32().Between(0, returnValues.Count - 1).Select(i => returnValues[i]).FunctionOf<int, int>();

                var func = gen.Minimum(
                    seed: seed,
                    size: 100,
                    pred: (f) => variables.Any(variable => f(variable) == targetReturnValue));

                variables.Should().OnlyContain(variable => func(variable) == targetReturnValue);
            }));
    }
}
