using System;
using System.Numerics;

namespace GalaxyCheck.Gens.Internal
{
    /// <summary>
    /// Describes how a real number type is built from choices. A finite value is a whole number, or a mantissa times
    /// a power of the radix. Each value is built only with exact operations, so that a seed gives the same values on
    /// each runtime. A new real number type needs only a new format.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal abstract class RealFormat<T>
        where T : struct, INumber<T>
    {
        /// <summary>The largest whole number that <see cref="Whole"/> takes.</summary>
        public abstract ulong MaxWhole { get; }

        /// <summary>The largest mantissa that <see cref="Compose"/> takes.</summary>
        public abstract ulong MaxMantissa { get; }

        /// <summary>The smallest exponent that <see cref="Compose"/> takes. It is at most zero.</summary>
        public abstract int MinExponent { get; }

        /// <summary>The largest exponent that <see cref="Compose"/> takes. It is at least zero.</summary>
        public abstract int MaxExponent { get; }

        /// <summary>The exponent that gives values from zero to about one with the full mantissa.</summary>
        public abstract int NearExponent { get; }

        /// <summary>The smallest finite value.</summary>
        public abstract T MinValue { get; }

        /// <summary>The largest finite value.</summary>
        public abstract T MaxValue { get; }

        /// <summary>The special values, in a fixed order.</summary>
        public abstract T[] Specials { get; }

        /// <summary>The whole number n, exactly.</summary>
        public abstract T Whole(ulong n);

        /// <summary>The mantissa times the radix to the power of the exponent, exactly and finite.</summary>
        public abstract T Compose(ulong mantissa, int exponent);

        /// <summary>The sum of x and y, or false if it overflows.</summary>
        public virtual bool TryAdd(T x, T y, out T sum)
        {
            sum = x + y;
            return T.IsFinite(sum);
        }
    }

    /// <summary>
    /// The format of a binary floating-point type: <see cref="Half"/>, <see cref="float"/>, <see cref="double"/>, and
    /// on .NET 11 BFloat16.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal sealed class BinaryRealFormat<T> : RealFormat<T>
        where T : struct, IBinaryFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        // A narrower type than double builds each value in double, where each value is exact, so that the value
        // does not depend on how the runtime implements the narrow type.
        private static readonly bool ViaDouble = typeof(T) != typeof(double);

        public static readonly BinaryRealFormat<T> Instance = new();

        private BinaryRealFormat()
        {
            // A narrower type than double reads its exponents in double, because ILogB of Half is wrong for a
            // subnormal value on .NET 10.
            var precision = T.MaxValue.GetSignificandBitLength();
            var minExponent = ViaDouble ? double.ILogB(double.CreateTruncating(T.Epsilon)) : T.ILogB(T.Epsilon);
            var maxExponent = ViaDouble ? double.ILogB(double.CreateTruncating(T.MaxValue)) : T.ILogB(T.MaxValue);
            if (ViaDouble && (precision > 53 || maxExponent > 1023 || minExponent < -1074))
            {
                throw new NotSupportedException($"The type {typeof(T)} does not fit in a double.");
            }

            MaxMantissa = precision >= 64 ? ulong.MaxValue : (1UL << precision) - 1;
            MaxWhole = MaxMantissa;
            MinExponent = minExponent;
            MaxExponent = maxExponent - precision + 1;
            NearExponent = 1 - precision;
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

        public override ulong MaxWhole { get; }

        public override ulong MaxMantissa { get; }

        public override int MinExponent { get; }

        public override int MaxExponent { get; }

        public override int NearExponent { get; }

        public override T MinValue => T.MinValue;

        public override T MaxValue => T.MaxValue;

        public override T[] Specials { get; }

        public override T Whole(ulong n) => ViaDouble ? T.CreateTruncating((double)n) : T.CreateTruncating(n);

        public override T Compose(ulong mantissa, int exponent) => ViaDouble
            ? T.CreateTruncating(double.ScaleB(mantissa, exponent))
            : T.ScaleB(T.CreateTruncating(mantissa), exponent);

        public override bool TryAdd(T x, T y, out T sum)
        {
            sum = ViaDouble
                ? T.CreateTruncating(double.CreateTruncating(x) + double.CreateTruncating(y))
                : x + y;
            return T.IsFinite(sum);
        }
    }

    /// <summary>
    /// The format of <see cref="decimal"/>. Its special values are zero, the minimum, and the maximum.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal sealed class DecimalRealFormat : RealFormat<decimal>
    {
        public static readonly DecimalRealFormat Instance = new();

        private static readonly decimal[] PowersOfTen =
        {
            1m, 10m, 100m, 1_000m, 10_000m, 100_000m, 1_000_000m, 10_000_000m, 100_000_000m, 1_000_000_000m,
        };

        private DecimalRealFormat()
        {
        }

        public override ulong MaxWhole => ulong.MaxValue;

        public override ulong MaxMantissa => ulong.MaxValue;

        public override int MinExponent => -28;

        // ulong.MaxValue * 10^9 is below decimal.MaxValue, so that no composed value overflows.
        public override int MaxExponent => 9;

        // ulong.MaxValue is about 1.8 times 10^19.
        public override int NearExponent => -19;

        public override decimal MinValue => decimal.MinValue;

        public override decimal MaxValue => decimal.MaxValue;

        public override decimal[] Specials { get; } = { 0m, decimal.MinValue, decimal.MaxValue };

        public override decimal Whole(ulong n) => n;

        public override decimal Compose(ulong mantissa, int exponent)
        {
            if (exponent >= 0)
            {
                return mantissa * PowersOfTen[exponent];
            }

            return new decimal(
                unchecked((int)(uint)mantissa),
                unchecked((int)(uint)(mantissa >> 32)),
                0,
                false,
                (byte)-exponent);
        }

        public override bool TryAdd(decimal x, decimal y, out decimal sum)
        {
            try
            {
                sum = x + y;
                return true;
            }
            catch (OverflowException)
            {
                sum = 0m;
                return false;
            }
        }
    }
}
