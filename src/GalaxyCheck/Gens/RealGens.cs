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
    /// A generator of a real number type. A value is four choices: the kind (0 is a whole number, 1 is a mantissa
    /// times a power of the radix, or a special value), the sign, the exponent, and the mantissa. The smallest
    /// choices are zero, then the small whole numbers, then the values with a fraction part. A special value is the
    /// exponent after the largest one, so that a smaller exponent turns it into a finite value.
    /// </summary>
    internal sealed record RealGen<T>(
        RealFormat<T> Format,
        T? Min = null,
        T? Max = null,
        T? Origin = null,
        bool NoSpecials = false) : BaseGen<T>, IRealGen<T>
        where T : struct, INumber<T>
    {
        // The random exponents come from this many of the simplest exponents half the time, so that values near one
        // appear often.
        private const ulong NearExponents = 64;

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
            var specialCount = SpecialCount(lo, hi);
            var exponentChoices = (ulong)(Format.MaxExponent - Format.MinExponent);
            var specialChoice = exponentChoices + 1;

            ulong kind, sign, exponent, mantissa;

            if (source.IsReplaying)
            {
                kind = source.Read(1);
                sign = source.Read(1);
                if (kind == 0)
                {
                    exponent = source.Read(0);
                    mantissa = source.Read(Format.MaxWhole);
                }
                else
                {
                    exponent = source.Read(specialCount > 0 ? specialChoice : exponentChoices);
                    mantissa = source.Read(Format.MaxMantissa);
                }
            }
            else
            {
                var pick = source.NextBounded(5);
                sign = source.NextBounded(1);
                if (pick == 0)
                {
                    kind = 0;
                    exponent = 0;
                    mantissa = source.NextBounded(IntCore.Limit(Format.MaxWhole, source.Size));
                }
                else
                {
                    kind = 1;
                    exponent = pick >= 4 && specialCount > 0
                        ? specialChoice
                        : source.NextBounded(source.NextBounded(1) == 0
                            ? System.Math.Min(NearExponents, exponentChoices)
                            : exponentChoices);
                    mantissa = source.NextBounded(Format.MaxMantissa);
                }

                source.Record(kind);
                source.Record(sign);
                source.Record(exponent);
                source.Record(mantissa);
            }

            if (kind == 1 && exponent == specialChoice)
            {
                return Special((int)(mantissa % (ulong)specialCount), lo, hi);
            }

            var value = kind == 0 ? Format.Whole(mantissa) : Format.Compose(mantissa, ExponentOf(exponent));
            if (sign == 1)
            {
                value = -value;
            }

            if (Min is null && Max is null && T.IsZero(origin))
            {
                return value;
            }

            if (Format.TryAdd(origin, value, out var shifted) && shifted >= lo && shifted <= hi)
            {
                return shifted;
            }

            // The value is outside the bounds: move from the origin toward a bound, by the fraction that the mantissa
            // is of its maximum.
            var fraction = T.CreateTruncating(mantissa) / T.CreateTruncating(kind == 0 ? Format.MaxWhole : Format.MaxMantissa);
            var target = sign == 0 ? hi : lo;
            var between = origin * (T.One - fraction) + target * fraction;
            return T.Clamp(between, lo, hi);
        }

        /// <summary>
        /// The exponent of a choice: 0, then 1, -1, 2, -2, and so on, then the rest of the longer side.
        /// </summary>
        private int ExponentOf(ulong choice)
        {
            var below = -Format.MinExponent;
            var above = Format.MaxExponent;
            var both = (ulong)System.Math.Min(below, above);

            if (choice <= both * 2)
            {
                var step = (int)((choice + 1) / 2);
                return choice % 2 == 1 ? step : -step;
            }

            var rest = (int)(choice - both * 2);
            return below > above ? -((int)both + rest) : (int)both + rest;
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
