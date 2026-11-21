using GalaxyCheck.Gens;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>Generates <c>sbyte</c> values over the full range of the type.</summary>
        public static IIntGen<sbyte> SByte() => throw new System.NotImplementedException("not implemented");

        /// <summary>Generates <c>ushort</c> values over the full range of the type.</summary>
        public static IIntGen<ushort> UInt16() => throw new System.NotImplementedException("not implemented");

        /// <summary>Generates <c>uint</c> values over the full range of the type.</summary>
        public static IIntGen<uint> UInt32() => throw new System.NotImplementedException("not implemented");

        /// <summary>Generates <c>ulong</c> values over the full range of the type.</summary>
        public static IIntGen<ulong> UInt64() => throw new System.NotImplementedException("not implemented");

        /// <summary>Generates <c>nint</c> values over the full range of the type.</summary>
        public static IIntGen<nint> IntPtr() => throw new System.NotImplementedException("not implemented");

        /// <summary>Generates <c>nuint</c> values over the full range of the type.</summary>
        public static IIntGen<nuint> UIntPtr() => throw new System.NotImplementedException("not implemented");

        /// <summary>Generates <c>System.Int128</c> values over the full range of the type.</summary>
        public static IIntGen<System.Int128> Int128() => throw new System.NotImplementedException("not implemented");

        /// <summary>Generates <c>System.UInt128</c> values over the full range of the type.</summary>
        public static IIntGen<System.UInt128> UInt128() => throw new System.NotImplementedException("not implemented");

        /// <summary>Generates <c>System.Numerics.BigInteger</c> values over the full range of the type.</summary>
        public static IIntGen<System.Numerics.BigInteger> BigInteger() => throw new System.NotImplementedException("not implemented");

    }

    public static partial class Extensions
    {
        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<T> Between<T>(this IIntGen<T> gen, T x, T y)
            where T : System.Numerics.INumber<T> => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<T> GreaterThan<T>(this IIntGen<T> gen, T minExclusive)
            where T : System.Numerics.INumber<T> => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<T> LessThan<T>(this IIntGen<T> gen, T maxExclusive)
            where T : System.Numerics.INumber<T> => throw new System.NotImplementedException("not implemented");
    }
}
