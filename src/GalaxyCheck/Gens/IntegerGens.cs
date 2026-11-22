using GalaxyCheck.Gens;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>Generates <c>sbyte</c> values over the full range of the type.</summary>
        public static IIntGen<sbyte> SByte() => new IntegerGen<sbyte>(sbyte.MinValue, sbyte.MaxValue);

        /// <summary>Generates <c>ushort</c> values over the full range of the type.</summary>
        public static IIntGen<ushort> UInt16() => new IntegerGen<ushort>(ushort.MinValue, ushort.MaxValue);

        /// <summary>Generates <c>uint</c> values over the full range of the type.</summary>
        public static IIntGen<uint> UInt32() => new IntegerGen<uint>(uint.MinValue, uint.MaxValue);

        /// <summary>Generates <c>ulong</c> values over the full range of the type.</summary>
        public static IIntGen<ulong> UInt64() => new IntegerGen<ulong>(ulong.MinValue, ulong.MaxValue);

        /// <summary>Generates <c>nint</c> values over the full range of the type.</summary>
        public static IIntGen<nint> IntPtr() => new IntegerGen<nint>(nint.MinValue, nint.MaxValue);

        /// <summary>Generates <c>nuint</c> values over the full range of the type.</summary>
        public static IIntGen<nuint> UIntPtr() => new IntegerGen<nuint>(nuint.MinValue, nuint.MaxValue);

        /// <summary>Generates <c>System.Int128</c> values over the full range of the type.</summary>
        public static IIntGen<System.Int128> Int128() => new IntegerGen<System.Int128>(System.Int128.MinValue, System.Int128.MaxValue);

        /// <summary>Generates <c>System.UInt128</c> values over the full range of the type.</summary>
        public static IIntGen<System.UInt128> UInt128() => new IntegerGen<System.UInt128>(System.UInt128.MinValue, System.UInt128.MaxValue);

        /// <summary>Generates <c>System.Numerics.BigInteger</c> values over the full range of the type.</summary>
        public static IIntGen<System.Numerics.BigInteger> BigInteger() =>
            new IntegerGen<System.Numerics.BigInteger>(-(System.Numerics.BigInteger.One << 127), System.Numerics.BigInteger.One << 127);

    }

    public static partial class Extensions
    {
        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<sbyte> Between(this IIntGen<sbyte> gen, sbyte x, sbyte y) => IntegerGenExtensions.Between(gen, x, y);

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<sbyte> GreaterThan(this IIntGen<sbyte> gen, sbyte minExclusive) => IntegerGenExtensions.GreaterThan(gen, minExclusive);

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<sbyte> LessThan(this IIntGen<sbyte> gen, sbyte maxExclusive) => IntegerGenExtensions.LessThan(gen, maxExclusive);

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<ushort> Between(this IIntGen<ushort> gen, ushort x, ushort y) => IntegerGenExtensions.Between(gen, x, y);

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<ushort> GreaterThan(this IIntGen<ushort> gen, ushort minExclusive) => IntegerGenExtensions.GreaterThan(gen, minExclusive);

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<ushort> LessThan(this IIntGen<ushort> gen, ushort maxExclusive) => IntegerGenExtensions.LessThan(gen, maxExclusive);

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<uint> Between(this IIntGen<uint> gen, uint x, uint y) => IntegerGenExtensions.Between(gen, x, y);

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<uint> GreaterThan(this IIntGen<uint> gen, uint minExclusive) => IntegerGenExtensions.GreaterThan(gen, minExclusive);

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<uint> LessThan(this IIntGen<uint> gen, uint maxExclusive) => IntegerGenExtensions.LessThan(gen, maxExclusive);

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<ulong> Between(this IIntGen<ulong> gen, ulong x, ulong y) => IntegerGenExtensions.Between(gen, x, y);

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<ulong> GreaterThan(this IIntGen<ulong> gen, ulong minExclusive) => IntegerGenExtensions.GreaterThan(gen, minExclusive);

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<ulong> LessThan(this IIntGen<ulong> gen, ulong maxExclusive) => IntegerGenExtensions.LessThan(gen, maxExclusive);

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<nint> Between(this IIntGen<nint> gen, nint x, nint y) => IntegerGenExtensions.Between(gen, x, y);

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<nint> GreaterThan(this IIntGen<nint> gen, nint minExclusive) => IntegerGenExtensions.GreaterThan(gen, minExclusive);

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<nint> LessThan(this IIntGen<nint> gen, nint maxExclusive) => IntegerGenExtensions.LessThan(gen, maxExclusive);

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<nuint> Between(this IIntGen<nuint> gen, nuint x, nuint y) => IntegerGenExtensions.Between(gen, x, y);

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<nuint> GreaterThan(this IIntGen<nuint> gen, nuint minExclusive) => IntegerGenExtensions.GreaterThan(gen, minExclusive);

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<nuint> LessThan(this IIntGen<nuint> gen, nuint maxExclusive) => IntegerGenExtensions.LessThan(gen, maxExclusive);

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<System.Int128> Between(this IIntGen<System.Int128> gen, System.Int128 x, System.Int128 y) => IntegerGenExtensions.Between(gen, x, y);

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<System.Int128> GreaterThan(this IIntGen<System.Int128> gen, System.Int128 minExclusive) => IntegerGenExtensions.GreaterThan(gen, minExclusive);

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<System.Int128> LessThan(this IIntGen<System.Int128> gen, System.Int128 maxExclusive) => IntegerGenExtensions.LessThan(gen, maxExclusive);

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<System.UInt128> Between(this IIntGen<System.UInt128> gen, System.UInt128 x, System.UInt128 y) => IntegerGenExtensions.Between(gen, x, y);

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<System.UInt128> GreaterThan(this IIntGen<System.UInt128> gen, System.UInt128 minExclusive) => IntegerGenExtensions.GreaterThan(gen, minExclusive);

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<System.UInt128> LessThan(this IIntGen<System.UInt128> gen, System.UInt128 maxExclusive) => IntegerGenExtensions.LessThan(gen, maxExclusive);

        /// <summary>Limits the values to the range from x to y, both inclusive.</summary>
        public static IIntGen<System.Numerics.BigInteger> Between(this IIntGen<System.Numerics.BigInteger> gen, System.Numerics.BigInteger x, System.Numerics.BigInteger y) => IntegerGenExtensions.Between(gen, x, y);

        /// <summary>Limits the values to those above the given value.</summary>
        public static IIntGen<System.Numerics.BigInteger> GreaterThan(this IIntGen<System.Numerics.BigInteger> gen, System.Numerics.BigInteger minExclusive) => IntegerGenExtensions.GreaterThan(gen, minExclusive);

        /// <summary>Limits the values to those below the given value.</summary>
        public static IIntGen<System.Numerics.BigInteger> LessThan(this IIntGen<System.Numerics.BigInteger> gen, System.Numerics.BigInteger maxExclusive) => IntegerGenExtensions.LessThan(gen, maxExclusive);

    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System;
    using System.Numerics;

    /// <summary>
    /// A generator of an integer type. A type of at most 64 bits uses the choices of <see cref="IntCore"/>, and a
    /// wider type uses the choices of <see cref="WideIntCore"/>.
    /// </summary>
    internal sealed record IntegerGen<T>(
        T DefaultMin,
        T DefaultMax,
        T? Min = null,
        T? Max = null,
        T? Origin = null,
        Gen.Bias? Bias = null) : BaseGen<T>, IIntGen<T>
        where T : struct, IBinaryInteger<T>
    {
        private const ulong SignBit = 0x8000000000000000UL;

        private static readonly bool IsWide =
            typeof(T) == typeof(Int128) || typeof(T) == typeof(UInt128) || typeof(T) == typeof(BigInteger);

        private static readonly bool IsSigned = T.IsNegative(T.AllBitsSet);

        public IIntGen<T> GreaterThanEqual(T min) => this with { Min = min };

        public IIntGen<T> LessThanEqual(T max) => this with { Max = max };

        public IIntGen<T> ShrinkTowards(T origin) => this with { Origin = origin };

        public IIntGen<T> WithBias(Gen.Bias bias) => this with { Bias = bias };

        public override T Generate(GenSource source)
        {
            var min = Min ?? DefaultMin;
            var max = Max ?? DefaultMax;

            if (min > max)
            {
                throw new GenErrorSignal("'min' cannot be greater than 'max'");
            }

            if (Origin is { } o && (o < min || o > max))
            {
                throw new GenErrorSignal("'origin' must be between 'min' and 'max'");
            }

            var origin = Origin ?? (min > T.Zero ? min : max < T.Zero ? max : T.Zero);
            var bias = Bias ?? Gen.Bias.WithSize;

            if (IsWide)
            {
                var value = WideIntCore.Generate(
                    source,
                    BigInteger.CreateTruncating(min),
                    BigInteger.CreateTruncating(max),
                    BigInteger.CreateTruncating(origin),
                    bias);
                return T.CreateTruncating(value);
            }

            return FromInt64(IntCore.Int64(
                source, ToInt64(min), ToInt64(max), ToInt64(origin), bias, boundsAreExtremes: true));
        }

        // An unsigned value moves by the sign bit, so that the order of the values is the order of the longs.
        private static long ToInt64(T value) => IsSigned
            ? long.CreateTruncating(value)
            : unchecked((long)(ulong.CreateTruncating(value) ^ SignBit));

        private static T FromInt64(long value) => IsSigned
            ? T.CreateTruncating(value)
            : T.CreateTruncating(unchecked((ulong)value ^ SignBit));
    }

    internal static class IntegerGenExtensions
    {
        public static IIntGen<T> Between<T>(IIntGen<T> gen, T x, T y)
            where T : IBinaryInteger<T>
        {
            var min = x > y ? y : x;
            var max = x > y ? x : y;
            return gen.GreaterThanEqual(min).LessThanEqual(max);
        }

        public static IIntGen<T> GreaterThan<T>(IIntGen<T> gen, T minExclusive)
            where T : IBinaryInteger<T>
        {
            try
            {
                return gen.GreaterThanEqual(checked(minExclusive + T.One));
            }
            catch (OverflowException)
            {
                return new FatalIntGen<T>(new ArgumentException(
                    "'minExclusive' must be below the maximum of the type"));
            }
        }

        public static IIntGen<T> LessThan<T>(IIntGen<T> gen, T maxExclusive)
            where T : IBinaryInteger<T>
        {
            try
            {
                return gen.LessThanEqual(checked(maxExclusive - T.One));
            }
            catch (OverflowException)
            {
                return new FatalIntGen<T>(new ArgumentException(
                    "'maxExclusive' must be above the minimum of the type"));
            }
        }
    }
}
