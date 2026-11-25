using GalaxyCheck.Gens.Parameters;
using System.Numerics;

namespace GalaxyCheck.Gens.Internal
{
    /// <summary>
    /// Generates integers that can be wider than 64 bits, with the same choices as <see cref="IntCore"/>: a side
    /// (above or below the origin) and a distance from the origin. The distance is one choice for each 64 bits, the
    /// highest part first, so that the smallest choices are the values nearest to the origin.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal static class WideIntCore
    {
        private static readonly BigInteger WordMask = ulong.MaxValue;

        /// <summary>
        /// Generates an integer from min to max. The caller checks that min &lt;= origin &lt;= max.
        /// </summary>
        public static BigInteger Generate(GenSource source, BigInteger min, BigInteger max, BigInteger origin, Gen.Bias bias)
        {
            var up = max - origin;
            var down = origin - min;

            if (up.IsZero && down.IsZero) return origin;

            var words = WordCount(BigInteger.Max(up, down));
            bool isDown;
            BigInteger distance;

            if (source.IsReplaying)
            {
                isDown = !up.IsZero && !down.IsZero ? source.Read(1) == 1 : up.IsZero;
                distance = ReadDistance(source, isDown ? down : up, words);
            }
            else
            {
                (isDown, distance) = bias == Gen.Bias.None
                    ? Uniform(source, up, down)
                    : Biased(source, up, down);

                if (!up.IsZero && !down.IsZero)
                {
                    source.Record(isDown ? 1UL : 0UL);
                }

                RecordDistance(source, distance, words);
            }

            return isDown ? origin - distance : origin + distance;
        }

        private static int WordCount(BigInteger value) => (int)System.Math.Max(1, (value.GetBitLength() + 63) / 64);

        private static ulong WordOf(BigInteger value, int word) => (ulong)((value >> (64 * word)) & WordMask);

        private static BigInteger ReadDistance(GenSource source, BigInteger limit, int words)
        {
            var distance = BigInteger.Zero;
            var tight = true;

            for (var w = words - 1; w >= 0; w--)
            {
                var limitWord = WordOf(limit, w);
                var choice = source.Read(tight ? limitWord : ulong.MaxValue);
                if (w > 0) source.MarkCarry();
                if (choice < limitWord) tight = false;
                distance = (distance << 64) | choice;
            }

            return distance;
        }

        private static void RecordDistance(GenSource source, BigInteger distance, int words)
        {
            for (var w = words - 1; w >= 0; w--)
            {
                source.Record(WordOf(distance, w));
                if (w > 0) source.MarkCarry();
            }
        }

        private static (bool, BigInteger) Uniform(GenSource source, BigInteger up, BigInteger down)
        {
            var value = NextBounded(source, up + down);
            return value < down ? (true, down - value) : (false, value - down);
        }

        private static (bool, BigInteger) Biased(GenSource source, BigInteger up, BigInteger down)
        {
            var size = source.Size;

            if (size > 50)
            {
                // At the bigger sizes, the bounds appear more often, as they do in IntCore.
                var normalWeight = size switch
                {
                    <= 60 => 16,
                    <= 70 => 8,
                    <= 80 => 4,
                    <= 90 => 2,
                    _ => 1
                };

                var pick = source.NextBounded((ulong)(normalWeight * 2 + 1));
                if (pick == 0)
                {
                    return (true, down);
                }

                if (pick == 1)
                {
                    return (false, up);
                }
            }

            var upLimit = Limit(up, size);
            var downLimit = Limit(down, size);
            var value = NextBounded(source, upLimit + downLimit);
            return value < downLimit ? (true, downLimit - value) : (false, value - downLimit);
        }

        /// <summary>
        /// The distance that a size allows: the number of bits grows linearly with the size.
        /// </summary>
        private static BigInteger Limit(BigInteger full, int size)
        {
            if (full.IsZero || size >= 100) return full;
            var bits = full.GetBitLength();
            var allowedBits = (int)((size * bits + 99) / 100);
            var limit = (BigInteger.One << allowedBits) - 1;
            return limit < full ? limit : full;
        }

        /// <summary>A uniform random integer from 0 to max, both inclusive.</summary>
        private static BigInteger NextBounded(GenSource source, BigInteger max)
        {
            if (max <= ulong.MaxValue) return source.NextBounded((ulong)max);

            var bits = (int)max.GetBitLength();
            var words = (bits + 63) / 64;
            var topBits = bits - (words - 1) * 64;
            var topMask = topBits == 64 ? ulong.MaxValue : (1UL << topBits) - 1;

            while (true)
            {
                var value = BigInteger.Zero;
                for (var w = words - 1; w >= 0; w--)
                {
                    var word = source.NextRandom();
                    if (w == words - 1) word &= topMask;
                    value = (value << 64) | word;
                }

                if (value <= max) return value;
            }
        }
    }
}
