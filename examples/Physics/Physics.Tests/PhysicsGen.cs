using GalaxyCheck;

namespace Physics.Tests;

/// <summary>Generators with explicit bounds. Each bound says which inputs a property is about.</summary>
public static class PhysicsGen
{
    /// <summary>A decimal exponent from -300 to 299, with the same chance for each. Bias.None turns off the default
    /// growth with the size, which otherwise keeps the first exponents near 0.</summary>
    public static IGen<int> Exponent => Gen.Int32().Between(-300, 299).WithBias(Gen.Bias.None);

    /// <summary>A component of 0, or a value from 1 to 10 of either sign.</summary>
    public static IGen<double> UnitScaleComponent => Gen.Choose(
        (Gen.Constant(0.0), 1),
        (from size in Gen.Double().Between(1, 10) from negative in Gen.Boolean() select negative ? -size : size, 4));

    /// <summary>Vectors of each scale from 1e-300 to 1e300: three components of unit scale, times one power of 10.</summary>
    public static IGen<Vector3> AnyScaleVector =>
        from exponent in Exponent
        from x in UnitScaleComponent
        from y in UnitScaleComponent
        from z in UnitScaleComponent
        select Math.Pow(10, exponent) * new Vector3(x, y, z);

    /// <summary>The values that IEEE 754 arithmetic treats in a special way.</summary>
    public static IGen<double> SpecialValue => Gen.Element(new[]
    {
        double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0.0, -0.0, double.Epsilon, double.MaxValue,
    });

    public static IGen<double> Between(double min, double max) => Gen.Double().Between(min, max);

    /// <summary>Everyday vectors, each component from -100 to 100.</summary>
    public static IGen<Vector3> Vector(double bound) =>
        from x in Between(-bound, bound)
        from y in Between(-bound, bound)
        from z in Between(-bound, bound)
        select new Vector3(x, y, z);

    /// <summary>A unit vector in a random direction.</summary>
    public static IGen<Vector3> Direction => Vector(1).Where(v => v.Length > 0.1).Select(v => v.Normalized());

    /// <summary>Two spheres of radius 0.5 that touch, with masses from 0.001 to 1000 and speeds up to 100.</summary>
    public static IGen<(Body A, Body B)> TouchingPair =>
        from centre in Vector(100)
        from direction in Direction
        from massA in Between(0.001, 1000)
        from massB in Between(0.001, 1000)
        from velocityA in Vector(100)
        from velocityB in Vector(100)
        select (new Body(centre + 0.5 * direction, velocityA, massA), new Body(centre - 0.5 * direction, velocityB, massB));
}
