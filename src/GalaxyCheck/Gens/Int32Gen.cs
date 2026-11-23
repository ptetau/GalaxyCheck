namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using System;

    public static partial class Gen
    {
        /// <summary>
        /// Generates 32-bit integers (ints). By default, the generator produces ints from 0, that grow towards
        /// <see cref="int.MinValue"/> and <see cref="int.MaxValue"/>, as the generator size increases. However,
        /// int generation can be customised using the builder methods on <see cref="IIntGen{T}"/>.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IIntGen<int> Int32() => new Int32Gen();
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Constrains the generator so that it only produces values between the supplied range (inclusive).
        /// </summary>
        /// <param name="x">The first bound of the range.</param>
        /// <param name="y">The second bound of the range.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<int> Between(this IIntGen<int> gen, int x, int y)
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
        public static IIntGen<int> LessThan(this IIntGen<int> gen, int maxExclusive)
        {
            try
            {
                checked
                {
                    return gen.LessThanEqual(maxExclusive - 1);
                }
            }
            catch (OverflowException ex)
            {
                return new FatalIntGen<int>(ex);
            }
        }

        /// <summary>
        /// Constrains the generator so that it only produces values greater than the supplied minimum.
        /// </summary>
        /// <param name="minExclusive">The minimum integer to generate (exclusive).</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IIntGen<int> GreaterThan(this IIntGen<int> gen, int minExclusive)
        {
            try
            {
                checked
                {
                    return gen.GreaterThanEqual(minExclusive + 1);
                }
            }
            catch (OverflowException ex)
            {
                return new FatalIntGen<int>(ex);
            }
        }
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;

    internal record Int32Gen(
        int? Min = null,
        int? Max = null,
        int? Origin = null,
        Gen.Bias? Bias = null) : BaseGen<int>, IIntGen<int>, GalaxyCheck.Properties.IFiniteGen
    {
        private (long Min, long Max, long Origin) Bounds()
        {
            long min = Min ?? int.MinValue;
            long max = Max ?? int.MaxValue;
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

        public IIntGen<int> GreaterThanEqual(int min) => this with { Min = min };

        public IIntGen<int> LessThanEqual(int max) => this with { Max = max };

        public IIntGen<int> ShrinkTowards(int origin) => this with { Origin = origin };

        public IIntGen<int> WithBias(Gen.Bias bias) => this with { Bias = bias };

        public override int Generate(GenSource source)
        {
            long min = Min ?? int.MinValue;
            long max = Max ?? int.MaxValue;

            if (min > max)
            {
                throw new GenErrorSignal("'min' cannot be greater than 'max'" + global::GalaxyCheck.Internal.FixHint.NumericMinAboveMax);
            }

            if (Origin != null && (Origin < min || Origin > max))
            {
                throw new GenErrorSignal("'origin' must be between 'min' and 'max'");
            }

            var origin = Origin ?? IntCore.InferOrigin(min, max);
            return (int)IntCore.Int64(source, min, max, origin, Bias ?? Gen.Bias.WithSize);
        }
    }
}
