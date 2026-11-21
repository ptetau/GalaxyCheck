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
        public static IIntGen<sbyte> Between(this IIntGen<sbyte> gen, sbyte x, sbyte y) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<sbyte> GreaterThan(this IIntGen<sbyte> gen, sbyte minExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<sbyte> LessThan(this IIntGen<sbyte> gen, sbyte maxExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<ushort> Between(this IIntGen<ushort> gen, ushort x, ushort y) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<ushort> GreaterThan(this IIntGen<ushort> gen, ushort minExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<ushort> LessThan(this IIntGen<ushort> gen, ushort maxExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<uint> Between(this IIntGen<uint> gen, uint x, uint y) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<uint> GreaterThan(this IIntGen<uint> gen, uint minExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<uint> LessThan(this IIntGen<uint> gen, uint maxExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<ulong> Between(this IIntGen<ulong> gen, ulong x, ulong y) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<ulong> GreaterThan(this IIntGen<ulong> gen, ulong minExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<ulong> LessThan(this IIntGen<ulong> gen, ulong maxExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<nint> Between(this IIntGen<nint> gen, nint x, nint y) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<nint> GreaterThan(this IIntGen<nint> gen, nint minExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<nint> LessThan(this IIntGen<nint> gen, nint maxExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<nuint> Between(this IIntGen<nuint> gen, nuint x, nuint y) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<nuint> GreaterThan(this IIntGen<nuint> gen, nuint minExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<nuint> LessThan(this IIntGen<nuint> gen, nuint maxExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<System.Int128> Between(this IIntGen<System.Int128> gen, System.Int128 x, System.Int128 y) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<System.Int128> GreaterThan(this IIntGen<System.Int128> gen, System.Int128 minExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<System.Int128> LessThan(this IIntGen<System.Int128> gen, System.Int128 maxExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<System.UInt128> Between(this IIntGen<System.UInt128> gen, System.UInt128 x, System.UInt128 y) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<System.UInt128> GreaterThan(this IIntGen<System.UInt128> gen, System.UInt128 minExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<System.UInt128> LessThan(this IIntGen<System.UInt128> gen, System.UInt128 maxExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<System.Numerics.BigInteger> Between(this IIntGen<System.Numerics.BigInteger> gen, System.Numerics.BigInteger x, System.Numerics.BigInteger y) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<System.Numerics.BigInteger> GreaterThan(this IIntGen<System.Numerics.BigInteger> gen, System.Numerics.BigInteger minExclusive) => throw new System.NotImplementedException("not implemented");

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<System.Numerics.BigInteger> LessThan(this IIntGen<System.Numerics.BigInteger> gen, System.Numerics.BigInteger maxExclusive) => throw new System.NotImplementedException("not implemented");

    }
}
