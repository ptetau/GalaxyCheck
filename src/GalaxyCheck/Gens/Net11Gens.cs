#if NET11_0_OR_GREATER
using GalaxyCheck.Gens;
using GalaxyCheck.Gens.Internal;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>Generates <c>Decimal32</c> values over the full finite range, with the special values.</summary>
        public static IRealGen<System.Numerics.Decimal32> Decimal32() =>
            new RealGen<System.Numerics.Decimal32>(DecimalIeeeRealFormats.Decimal32);

        /// <summary>Generates <c>Decimal64</c> values over the full finite range, with the special values.</summary>
        public static IRealGen<System.Numerics.Decimal64> Decimal64() =>
            new RealGen<System.Numerics.Decimal64>(DecimalIeeeRealFormats.Decimal64);

        /// <summary>Generates <c>Decimal128</c> values over the full finite range, with the special values.</summary>
        public static IRealGen<System.Numerics.Decimal128> Decimal128() =>
            new RealGen<System.Numerics.Decimal128>(DecimalIeeeRealFormats.Decimal128);

        /// <summary>Generates <c>BFloat16</c> values over the full finite range, with the special values.</summary>
        public static IRealGen<System.Numerics.BFloat16> BFloat16() =>
            new RealGen<System.Numerics.BFloat16>(BinaryRealFormat<System.Numerics.BFloat16>.Instance);

        /// <summary>Generates <c>Complex&lt;T&gt;</c> values from a generator of each part.</summary>
        public static IGen<System.Numerics.Complex<T>> ComplexOf<T>(IGen<T> real, IGen<T> imaginary)
            where T : System.Numerics.IFloatingPointIeee754<T>, System.Numerics.IMinMaxValue<T>
        {
            var (r, i) = (real.Advanced, imaginary.Advanced);
            return new FunctionGen<System.Numerics.Complex<T>>(s =>
            {
                var re = r.Generate(s);
                return new System.Numerics.Complex<T>(re, i.Generate(s));
            });
        }
    }
}

namespace GalaxyCheck.Gens.Internal
{
    using System.Numerics;

    internal static class DecimalIeeeRealFormats
    {
        public static readonly DecimalIeeeRealFormat<Decimal32> Decimal32 = new(9_999_999UL, -101, 90, -7);

        public static readonly DecimalIeeeRealFormat<Decimal64> Decimal64 =
            new(9_999_999_999_999_999UL, -398, 369, -16);

        // The mantissa has 19 digits, as a ulong. The largest exponent keeps the largest value below the maximum.
        public static readonly DecimalIeeeRealFormat<Decimal128> Decimal128 = new(ulong.MaxValue, -6176, 6125, -19);
    }

    /// <summary>
    /// The format of an IEEE decimal floating-point type. A value is a mantissa times a power of ten, built with the
    /// exact <c>ScaleB</c>.
    /// </summary>
    internal sealed class DecimalIeeeRealFormat<T> : RealFormat<T>
        where T : struct, IDecimalFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public DecimalIeeeRealFormat(ulong maxMantissa, int minExponent, int maxExponent, int nearExponent)
        {
            MaxMantissa = maxMantissa;
            MinExponent = minExponent;
            MaxExponent = maxExponent;
            NearExponent = nearExponent;
            Specials = new[]
            {
                T.NaN,
                T.PositiveInfinity,
                T.NegativeInfinity,
                T.NegativeZero,
                T.Epsilon,
                T.MinValue,
                T.MaxValue,
            };
        }

        public override ulong MaxWhole => MaxMantissa;

        public override ulong MaxMantissa { get; }

        public override int MinExponent { get; }

        public override int MaxExponent { get; }

        public override int NearExponent { get; }

        public override T MinValue => T.MinValue;

        public override T MaxValue => T.MaxValue;

        public override T[] Specials { get; }

        public override T Whole(ulong n) => T.CreateTruncating(n);

        public override T Compose(ulong mantissa, int exponent) => T.ScaleB(T.CreateTruncating(mantissa), exponent);
    }
}
#endif
