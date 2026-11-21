#if NET11_0_OR_GREATER
using GalaxyCheck.Gens;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        public static IRealGen<System.Numerics.Decimal32> Decimal32() => throw new System.NotImplementedException("not implemented");

        public static IRealGen<System.Numerics.Decimal64> Decimal64() => throw new System.NotImplementedException("not implemented");

        public static IRealGen<System.Numerics.Decimal128> Decimal128() => throw new System.NotImplementedException("not implemented");

        public static IRealGen<System.Numerics.BFloat16> BFloat16() => throw new System.NotImplementedException("not implemented");

        public static IGen<System.Numerics.Complex<T>> ComplexOf<T>(IGen<T> real, IGen<T> imaginary)
            where T : System.Numerics.IFloatingPointIeee754<T>, System.Numerics.IMinMaxValue<T> => throw new System.NotImplementedException("not implemented");
    }
}
#endif
