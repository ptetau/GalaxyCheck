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
    using GalaxyCheck.Gens.Internal;

    public static partial class Gen
    {
        /// <summary>Generates <c>System.Half</c> values over the full finite range, with the special values.</summary>
        public static IRealGen<System.Half> Half() => new RealGen<System.Half>(BinaryRealFormat<System.Half>.Instance);

        /// <summary>Generates <c>float</c> values over the full finite range, with the special values.</summary>
        public static IRealGen<float> Single() => new RealGen<float>(BinaryRealFormat<float>.Instance);

        /// <summary>Generates <c>double</c> values over the full finite range, with the special values.</summary>
        public static IRealGen<double> Double() => new RealGen<double>(BinaryRealFormat<double>.Instance);

        /// <summary>Generates <c>decimal</c> values with a fraction part, with zero, the minimum, and the maximum.</summary>
        public static IRealGen<decimal> Decimal() => new RealGen<decimal>(DecimalRealFormat.Instance);

        /// <summary>Generates <c>System.Numerics.Complex</c> values whose parts come from <see cref="Double"/>.</summary>
        public static IComplexGen Complex() => new ComplexGen();
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System.Numerics;

    /// <summary>
    /// A generator of a real number type. A value is four choices: the kind, the sign, the exponent, and the mantissa.
    /// Kind 0 is a whole number, kind 1 is a mantissa times a power of the radix, and kind 2 is a special value when
    /// the exponent and the mantissa are at their top, else the same as kind 1. A special value shrinks to kind 1 with
    /// the largest exponent and mantissa, so that it becomes a large finite value that the shrinker can lower. The
    /// exponent choice 0 is the exponent that gives values from zero to about one, so that values with a fraction part
    /// near one are common. A value that the bounds do not hold is the bound, so that a larger choice never gives a
    /// value closer to the origin.
    /// </summary>
    internal sealed record RealGen<T>(
        RealFormat<T> Format,
        T? Min = null,
        T? Max = null,
        T? Origin = null,
        bool NoSpecials = false) : BaseGen<T>, IRealGen<T>
        where T : struct, INumber<T>
    {
        // A near exponent is at most this far from the exponent of the values near one, and the small distances are
        // the most common.
        private const ulong NearSpread = 32;

        public IRealGen<T> GreaterThanEqual(T min) => this with { Min = min };

        public IRealGen<T> LessThanEqual(T max) => this with { Max = max };

        public IRealGen<T> Between(T x, T y) => x > y
            ? this with { Min = y, Max = x }
            : this with { Min = x, Max = y };

        public IRealGen<T> ShrinkTowards(T origin) => this with { Origin = origin };

        public IRealGen<T> WithoutSpecialValues() => this with { NoSpecials = true };

        public override T Generate(GenSource source)
        {
            if (Min is { } min && T.IsNaN(min))
            {
                throw new GenErrorSignal("'min' must be a number");
            }

            if (Max is { } max && T.IsNaN(max))
            {
                throw new GenErrorSignal("'max' must be a number");
            }

            var lo = Min is { } l ? T.Clamp(l, Format.MinValue, Format.MaxValue) : Format.MinValue;
            var hi = Max is { } h ? T.Clamp(h, Format.MinValue, Format.MaxValue) : Format.MaxValue;

            if (lo > hi)
            {
                throw new GenErrorSignal("'min' cannot be greater than 'max'");
            }

            if (Origin is { } o && !(o >= lo && o <= hi))
            {
                throw new GenErrorSignal("'origin' must be between 'min' and 'max'");
            }

            var origin = Origin ?? (lo > T.Zero ? lo : hi < T.Zero ? hi : T.Zero);
            var isShifted = Min is not null || Max is not null || !T.IsZero(origin);
            var specialCount = SpecialCount(lo, hi);
            var top = (ulong)(Format.MaxExponent - Format.MinExponent);

            ulong kind, sign, exponent, mantissa;

            if (source.IsReplaying)
            {
                kind = source.Read(2);
                sign = source.Read(1);
                exponent = source.Read(kind == 0 ? 0 : top);
                mantissa = source.Read(kind == 0 ? Format.MaxWhole : Format.MaxMantissa);
            }
            else
            {
                var pick = source.NextBounded(7);
                sign = source.NextBounded(1);
                if (pick >= 6 && specialCount > 0)
                {
                    var index = source.NextBounded((ulong)specialCount - 1);
                    var special = Special((int)index, lo, hi);
                    kind = 2;
                    sign = special < origin || (special == origin && T.IsNegative(special)) ? 1UL : 0UL;
                    exponent = top;
                    mantissa = Format.MaxMantissa - index;
                }
                else
                {
                    var down = IsDown(sign, origin, lo, hi);
                    var width = Width(down ? origin : hi, down ? lo : origin);
                    if (pick < 2)
                    {
                        kind = 0;
                        exponent = 0;
                        mantissa = source.NextBounded(System.Math.Min(
                            IntCore.Limit(Format.MaxWhole, source.Size),
                            WholeLimit(width)));
                    }
                    else
                    {
                        kind = 1;
                        var maxExponent = MaxExponentWithin(width);
                        var e = pick == 5
                            ? Format.MinExponent + (int)source.NextBounded((ulong)(maxExponent - Format.MinExponent))
                            : NearExponent(source, maxExponent);
                        exponent = ChoiceOf(e);
                        mantissa = source.NextBounded(Format.MaxMantissa);
                    }
                }

                source.Record(kind);
                source.Record(sign);
                source.Record(exponent);
                source.Record(mantissa);
            }

            if (kind == 2 && exponent == top && Format.MaxMantissa - mantissa < (ulong)specialCount)
            {
                return Special((int)(Format.MaxMantissa - mantissa), lo, hi);
            }

            var value = kind == 0 ? Format.Whole(mantissa) : Format.Compose(mantissa, ExponentOf(exponent));
            if (!isShifted)
            {
                return sign == 1 ? -value : value;
            }

            var isDown = IsDown(sign, origin, lo, hi);
            if (Format.TryAdd(origin, isDown ? -value : value, out var shifted) && shifted >= lo && shifted <= hi)
            {
                return shifted;
            }

            return isDown ? lo : hi;
        }

        /// <summary>The side of the origin that a sign moves to. A side without values moves to the other side.</summary>
        private static bool IsDown(ulong sign, T origin, T lo, T hi) => sign == 1 ? origin != lo : origin == hi;

        /// <summary>The distance from y up to x, or the largest value if it overflows.</summary>
        private T Width(T x, T y) => Format.TryAdd(x, -y, out var width) ? width : Format.MaxValue;

        private ulong WholeLimit(T width) => width >= Format.Whole(Format.MaxWhole)
            ? Format.MaxWhole
            : ulong.CreateTruncating(width);

        /// <summary>The largest exponent whose values with any mantissa are at most the width.</summary>
        private int MaxExponentWithin(T width)
        {
            if (width >= Format.MaxValue) return Format.MaxExponent;

            var low = Format.MinExponent;
            var high = Format.MaxExponent;
            while (low < high)
            {
                var mid = low + (high - low + 1) / 2;
                if (Format.Compose(Format.MaxMantissa, mid) <= width)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return low;
        }

        /// <summary>An exponent near the exponent of the values near one, or near the largest exponent if that is lower.</summary>
        private int NearExponent(GenSource source, int maxExponent)
        {
            var center = System.Math.Min(Format.NearExponent, maxExponent);
            var distance = (int)source.NextBounded(source.NextBounded(source.NextBounded(NearSpread)));
            var e = source.NextBounded(1) == 0 ? center + distance : center - distance;
            return System.Math.Clamp(e, Format.MinExponent, maxExponent);
        }

        /// <summary>The choice of an exponent: the inverse of <see cref="ExponentOf"/>.</summary>
        private ulong ChoiceOf(int exponent)
        {
            var center = Format.NearExponent;
            var both = System.Math.Min(center - Format.MinExponent, Format.MaxExponent - center);
            var distance = System.Math.Abs(exponent - center);

            if (distance <= both)
            {
                return exponent > center ? (ulong)(2 * distance - 1) : (ulong)(2 * distance);
            }

            return (ulong)(2 * both + (distance - both));
        }

        /// <summary>
        /// The exponent of a choice, from the exponent of the values near one: 0, then 1, -1, 2, -2, and so on, then
        /// the rest of the longer side.
        /// </summary>
        private int ExponentOf(ulong choice)
        {
            var center = Format.NearExponent;
            var below = center - Format.MinExponent;
            var above = Format.MaxExponent - center;
            var both = (ulong)System.Math.Min(below, above);

            if (choice <= both * 2)
            {
                var step = (int)((choice + 1) / 2);
                return choice % 2 == 1 ? center + step : center - step;
            }

            var rest = (int)(choice - both * 2);
            return below > above ? center - ((int)both + rest) : center + (int)both + rest;
        }

        private bool IsSpecialAllowed(T special, T lo, T hi)
        {
            if (Min is null && Max is null) return true;
            return !T.IsNaN(special) && !T.IsInfinity(special) && special >= lo && special <= hi;
        }

        private int SpecialCount(T lo, T hi)
        {
            if (NoSpecials) return 0;

            var count = 0;
            foreach (var special in Format.Specials)
            {
                if (IsSpecialAllowed(special, lo, hi)) count++;
            }

            if (Min is not null) count++;
            if (Max is not null) count++;
            return count;
        }

        private T Special(int index, T lo, T hi)
        {
            foreach (var special in Format.Specials)
            {
                if (!IsSpecialAllowed(special, lo, hi)) continue;
                if (index == 0) return special;
                index--;
            }

            // The bounds are special values too.
            return Min is not null && index == 0 ? lo : hi;
        }
    }

    internal sealed record ComplexGen(bool NoSpecials = false) : BaseGen<Complex>, IComplexGen
    {
        private static readonly IGenAdvanced<double> Parts = Gen.Double().Advanced;

        private static readonly IGenAdvanced<double> FiniteParts = Gen.Double().WithoutSpecialValues().Advanced;

        public IComplexGen WithoutSpecialValues() => this with { NoSpecials = true };

        public override Complex Generate(GenSource source)
        {
            var parts = NoSpecials ? FiniteParts : Parts;
            var real = parts.Generate(source);
            var imaginary = parts.Generate(source);
            return new Complex(real, imaginary);
        }
    }
}
