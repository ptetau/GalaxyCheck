namespace GalaxyCheck.Tests.Features.Generators.Reflection.ObjectGraph;

public class AboutErrorReproduction
{
    [Property]
    [InlineData(typeof(ExampleObjects.UnconstructableClass), "")]
    [InlineData(typeof(ClassWithNestedUnconstructableClass), " at path '$.Property'")]
    [InlineData(typeof(ClassWithNestedNullableUnconstructableClass), " at path '$.Property'")]
    [InlineData(typeof(List<ExampleObjects.UnconstructableClass>), " at path '$.[*]'")]
    public Property WhenTypeIsUnresolvable(Type type, string expectedPathDescription)
    {
        return Property.ForAll(DomainGen.Seed(), seed =>
        {
            // Arrange
            var gen = Gen.Create(type);

            // Act
            var action = () => gen.Sample(args => args with { Seed = seed });

            // Assert
            action
                .Should()
                .Throw<Exceptions.GenErrorException>()
                .WithGenErrorMessage($"could not resolve type '*'{expectedPathDescription}");
        });
    }

    private class ClassWithNestedUnconstructableClass
    {
        public ExampleObjects.UnconstructableClass Property { get; set; } = null!;
    }

    private class ClassWithNestedNullableUnconstructableClass
    {
        public ExampleObjects.UnconstructableClass? Property { get; set; } = null!;
    }
}
