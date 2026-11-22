namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using System;

    public static partial class Gen
    {
        /// <summary>
        /// Creates a generator that produces 64-bit integers. By default, it will generate integers in the full range
        /// (<see cref="long.MinValue"/> to <see cref="long.MaxValue"/>), but the generator returned contains
        /// configuration methods to constrain the produced integers further.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IIntGen<long> Int64() => new Int64Gen();
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Constrains the generator so that it only produces values between the supplied range (inclusive).
        /// </summary>
        /// <param name="x">The first bound of the range.</param>
        /// <param name="y">The second bound of the range.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<long> Between(this IIntGen<long> gen, long x, long y)
        {
            var min = x > y ? y : x;
            var max = x > y ? x : y;
            return gen.GreaterThanEqual(min).LessThanEqual(max);
        }

        /// <summary>
        /// Constrains the generator so that it only produces values less than the supplied maximum. 
        /// </summary>
        /// <param name="maxExclusive">The maximum int to generate (exclusive).</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<long> LessThan(this IIntGen<long> gen, long maxExclusive)
        {
            try
            {
                checked
                {
                    return gen.LessThanEqual((long)(maxExclusive - 1));
                }
            }
            catch (OverflowException ex)
            {
                return new FatalIntGen<long>(ex);
            }
        }

        /// <summary>
        /// Constrains the generator so that it only produces values greater than the supplied minimum.
        /// </summary>
        /// <param name="minExclusive">The minimum integer to generate (exclusive).</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<long> GreaterThan(this IIntGen<long> gen, long minExclusive)
        {
            try
            {
                checked
                {
                    return gen.GreaterThanEqual((long)(minExclusive + 1));
                }
            }
            catch (OverflowException ex)
            {
                return new FatalIntGen<long>(ex);
            }
        }
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;

    internal record Int64Gen(
        long? Min = null,
        long? Max = null,
        long? Origin = null,
        Gen.Bias? Bias = null) : BaseGen<long>, IIntGen<long>, GalaxyCheck.Properties.IFiniteGen
    {
        private (long Min, long Max, long Origin) Bounds()
        {
            long min = Min ?? long.MinValue;
            long max = Max ?? long.MaxValue;
            return (min, max, Origin ?? IntCore.InferOrigin(min, max));
        }

        public long? Count(long limit)
        {
            var (min, max, origin) = Bounds();
            return GalaxyCheck.Properties.Finite.CountInt(min, max, origin, limit);
        }

        public void WriteChoices(long index, System.Collections.Generic.List<ulong> choices)
        {
            var (min, max, origin) = Bounds();
            GalaxyCheck.Properties.Finite.WriteInt(min, max, origin, index, choices);
        }

        public IIntGen<long> GreaterThanEqual(long min) => this with { Min = min };

        public IIntGen<long> LessThanEqual(long max) => this with { Max = max };

        public IIntGen<long> ShrinkTowards(long origin) => this with { Origin = origin };

        public IIntGen<long> WithBias(Gen.Bias bias) => this with { Bias = bias };

        public override long Generate(GenSource source)
        {
            long min = Min ?? long.MinValue;
            long max = Max ?? long.MaxValue;

            if (min > max)
            {
                throw new GenErrorSignal("'min' cannot be greater than 'max'");
            }

            if (Origin != null && (Origin < min || Origin > max))
            {
                throw new GenErrorSignal("'origin' must be between 'min' and 'max'");
            }

            var origin = Origin ?? IntCore.InferOrigin(min, max);
            return (long)IntCore.Int64(source, min, max, origin, Bias ?? Gen.Bias.WithSize);
        }
    }
}
