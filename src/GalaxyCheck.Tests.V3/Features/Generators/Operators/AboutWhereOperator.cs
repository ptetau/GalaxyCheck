namespace GalaxyCheck.Tests.Features.Generators.Operators;

public class AboutWhereOperator
{
    [Property]
    public Property ItProducesValuesThatPassThePredicate()
    {
        return Property.ForAll(DomainGen.Seed(), DomainGen.Size(), Test);

        static void Test(int seed, int size)
        {
            // Arrange
            var testFunction = DummyTestFunctions.Int32.NonZero();
            var baseGen = Gen.Int32();
            var filteredGen = baseGen.Where(testFunction);

            // Act
            var sample = filteredGen.SampleTraversal(args => args with { Seed = seed, Size = size });

            // Assert
            sample.Should().OnlyContain(x => testFunction(x));
        }
    }

    [Property]
    public Property ItCanAdaptToPredicatesThatOnlyPassForLargerSizes()
    {
        return Property.ForAll(DomainGen.Seed(), DomainGen.Size(), Test);

        static void Test(int seed, int size)
        {
            // Arrange
            var testFunction = DummyTestFunctions.Size.Top50thPercentile();
            var baseGen = DummyGens.TheInputSize();
            var filteredGen = baseGen.Where(testFunction);

            // Act
            var act = () => filteredGen.SampleTraversal(args => args with { Seed = seed, Size = size });

            // Assert
            act.Should().NotThrow<Exceptions.GenExhaustionException>();
        }
    }
}
