namespace GalaxyCheck.Gens
{
    /// <summary>A generator of real numbers with bounds, a shrink origin, and special values.</summary>
    public interface IRealGen<T> : IGen<T>
    {
        IRealGen<T> GreaterThanEqual(T min);

        IRealGen<T> LessThanEqual(T max);

        IRealGen<T> Between(T x, T y);

        IRealGen<T> ShrinkTowards(T origin);

        IRealGen<T> WithoutSpecialValues();
    }

    /// <summary>A generator of complex numbers.</summary>
    public interface IComplexGen : IGen<System.Numerics.Complex>
    {
        IComplexGen WithoutSpecialValues();
    }
}

namespace GalaxyCheck
{
    using GalaxyCheck.Gens;

    public static partial class Gen
    {
        public static IRealGen<System.Half> Half() => throw new System.NotImplementedException("not implemented");

        public static IRealGen<float> Single() => throw new System.NotImplementedException("not implemented");

        public static IRealGen<double> Double() => throw new System.NotImplementedException("not implemented");

        public static IRealGen<decimal> Decimal() => throw new System.NotImplementedException("not implemented");

        public static IComplexGen Complex() => throw new System.NotImplementedException("not implemented");
    }
}
