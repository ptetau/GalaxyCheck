
namespace GalaxyCheck.Tests;

public static class DomainGen
{
    public static class TestFunctions
    {
        public static IGen<Func<bool>> Nullary() => Gen.Function(Gen.Boolean());

        public static IGen<Func<T, bool>> Unary<T>() => Gen.Function<T, bool>(Gen.Boolean());
    }

    public static class Gens
    {
        public static IGen<IGen<object>> Error() => Gen
            .String()
            .WithLengthBetween(0, 10)
            .Select(Gen.Advanced.Error<object>);
    }

    public static class Properties
    {
        public static IGen<PropertyProxy> Any() =>
            from seed in Seed()
            from size in Size()
            from testFunction in TestFunctions.Unary<object>()
            select new PropertyProxy(seed, size, testFunction);

        public static IGen<Property> Nullary() =>
            from testFunction in TestFunctions.Nullary()
            select Property.Nullary(testFunction);
    }

    public static IGen<int> Size() =>
        Gen.Int32().Between(GalaxyCheck.Gens.Parameters.Size.Zero.Value, GalaxyCheck.Gens.Parameters.Size.Max.Value);

    public static IGen<int> Seed() => Gen.Int32().NoShrink();

    public static IGen<int?> SeedWaypoint() => Seed().OrNullStruct();

    public static IGen<Property> ToProperty(this IGen<IGen<object>> metaGen) =>
        from gen in metaGen
        from testFunction in TestFunctions.Unary<object>()
        select new Property((IGen<Property.Test<object>>)Property.ForAll<object>(gen, testFunction));

    public class SeedAttribute : GenAttribute
    {
        public override IGen Value => Seed();
    }
}
