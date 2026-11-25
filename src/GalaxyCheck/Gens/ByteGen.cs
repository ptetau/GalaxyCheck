namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using System;

    public static partial class Gen
    {
        /// <summary>
        /// Generates bytes. By default, the generator produces bytes from 0x00 that will grow towards 0xFF, as the
        /// generator size increases. However, byte generation can be customised using the builder methods on
        /// <see cref="IIntGen{T}"/>.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IIntGen<byte> Byte() => new ByteGen();
    }

    /// <summary>The operators and runners of generators and properties, for example <c>Select</c>, <c>Sample</c>, and <c>Check</c>.</summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Constrains the generator so that it only produces values between the supplied range (inclusive).
        /// </summary>
        /// <param name="x">The first bound of the range.</param>
        /// <param name="y">The second bound of the range.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<byte> Between(this IIntGen<byte> gen, byte x, byte y)
        {
            var min = x > y ? y : x;
            var max = x > y ? x : y;
            return gen.GreaterThanEqual(min).LessThanEqual(max);
        }

        /// <summary>
        /// Constrains the generator so that it only produces values less than the supplied maximum. 
        /// </summary>
        /// <param name="maxExclusive">The maximum byte to generate (exclusive).</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<byte> LessThan(this IIntGen<byte> gen, byte maxExclusive)
        {
            try
            {
                checked
                {
                    return gen.LessThanEqual((byte)(maxExclusive - 1));
                }
            }
            catch (OverflowException ex)
            {
                return new FatalIntGen<byte>(ex);
            }
        }

        /// <summary>
        /// Constrains the generator so that it only produces values greater than the supplied minimum.
        /// </summary>
        /// <param name="minExclusive">The minimum byte to generate (exclusive).</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<byte> GreaterThan(this IIntGen<byte> gen, byte minExclusive)
        {
            try
            {
                checked
                {
                    return gen.GreaterThanEqual((byte)(minExclusive + 1));
                }
            }
            catch (OverflowException ex)
            {
                return new FatalIntGen<byte>(ex);
            }
        }
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;

    [global::Verifiable.Contracts.Unverifiable]
    internal record ByteGen(
        byte? Min = null,
        byte? Max = null,
        byte? Origin = null,
        Gen.Bias? Bias = null) : BaseGen<byte>, IIntGen<byte>, GalaxyCheck.Properties.IFiniteGen
    {
        private (long Min, long Max, long Origin) Bounds()
        {
            long min = Min ?? byte.MinValue;
            long max = Max ?? byte.MaxValue;
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

        public IIntGen<byte> GreaterThanEqual(byte min) => this with { Min = min };

        public IIntGen<byte> LessThanEqual(byte max) => this with { Max = max };

        public IIntGen<byte> ShrinkTowards(byte origin) => this with { Origin = origin };

        public IIntGen<byte> WithBias(Gen.Bias bias) => this with { Bias = bias };

        public override byte Generate(GenSource source)
        {
            long min = Min ?? byte.MinValue;
            long max = Max ?? byte.MaxValue;

            if (min > max)
            {
                throw new GenErrorSignal("'min' cannot be greater than 'max'" + global::GalaxyCheck.Internal.FixHint.NumericMinAboveMax);
            }

            if (Origin != null && (Origin < min || Origin > max))
            {
                throw new GenErrorSignal("'origin' must be between 'min' and 'max'");
            }

            var origin = Origin ?? IntCore.InferOrigin(min, max);
            return (byte)IntCore.Int64(source, min, max, origin, Bias ?? Gen.Bias.WithSize);
        }
    }
}
