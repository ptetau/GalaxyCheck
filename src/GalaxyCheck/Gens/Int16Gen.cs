namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using System;

    public static partial class Gen
    {
        /// <summary>
        /// Generates 16-bit integers (shorts). By default, the generator produces shorts from 0, that grow towards
        /// <see cref="short.MinValue"/> and <see cref="short.MaxValue"/>, as the generator size increases. However,
        /// short generation can be customised using the builder methods on <see cref="IIntGen{T}"/>.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IIntGen<short> Int16() => new Int16Gen();
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Constrains the generator so that it only produces values between the supplied range (inclusive).
        /// </summary>
        /// <param name="x">The first bound of the range.</param>
        /// <param name="y">The second bound of the range.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<short> Between(this IIntGen<short> gen, short x, short y)
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
        public static IIntGen<short> LessThan(this IIntGen<short> gen, short maxExclusive)
        {
            try
            {
                checked
                {
                    return gen.LessThanEqual((short)(maxExclusive - 1));
                }
            }
            catch (OverflowException ex)
            {
                return new FatalIntGen<short>(ex);
            }
        }

        /// <summary>
        /// Constrains the generator so that it only produces values greater than the supplied minimum.
        /// </summary>
        /// <param name="minExclusive">The minimum integer to generate (exclusive).</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<short> GreaterThan(this IIntGen<short> gen, short minExclusive)
        {
            try
            {
                checked
                {
                    return gen.GreaterThanEqual((short)(minExclusive + 1));
                }
            }
            catch (OverflowException ex)
            {
                return new FatalIntGen<short>(ex);
            }
        }
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;

    [global::Verifiable.Contracts.Unverifiable]
    internal record Int16Gen(
        short? Min = null,
        short? Max = null,
        short? Origin = null,
        Gen.Bias? Bias = null) : BaseGen<short>, IIntGen<short>, GalaxyCheck.Properties.IFiniteGen
    {
        private (long Min, long Max, long Origin) Bounds()
        {
            long min = Min ?? short.MinValue;
            long max = Max ?? short.MaxValue;
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

        public IIntGen<short> GreaterThanEqual(short min) => this with { Min = min };

        public IIntGen<short> LessThanEqual(short max) => this with { Max = max };

        public IIntGen<short> ShrinkTowards(short origin) => this with { Origin = origin };

        public IIntGen<short> WithBias(Gen.Bias bias) => this with { Bias = bias };

        public override short Generate(GenSource source)
        {
            long min = Min ?? short.MinValue;
            long max = Max ?? short.MaxValue;

            if (min > max)
            {
                throw new GenErrorSignal("'min' cannot be greater than 'max'" + global::GalaxyCheck.Internal.FixHint.NumericMinAboveMax);
            }

            if (Origin != null && (Origin < min || Origin > max))
            {
                throw new GenErrorSignal("'origin' must be between 'min' and 'max'");
            }

            var origin = Origin ?? IntCore.InferOrigin(min, max);
            return (short)IntCore.Int64(source, min, max, origin, Bias ?? Gen.Bias.WithSize);
        }
    }
}
