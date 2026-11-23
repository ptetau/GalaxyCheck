using GalaxyCheck.Gens.Parameters;
using System.Numerics;

namespace GalaxyCheck.Gens.Internal
{
    /// <summary>
    /// Generates integers as choices. An integer is a side (above or below the origin) and a distance from the
    /// origin, so that the smallest choices are the values nearest to the origin.
    /// </summary>
    internal static class IntCore
    {
        /// <summary>
        /// Generates an integer from min to max. The caller checks that min &lt;= origin &lt;= max. With
        /// boundsAreExtremes, a biased value is min or max more often even when one of them is the origin.
        /// </summary>
        public static long Int64(GenSource source, long min, long max, long origin, Gen.Bias bias, bool boundsAreExtremes = false)
        {
            var up = unchecked((ulong)max - (ulong)origin);
            var down = unchecked((ulong)origin - (ulong)min);

            if (up == 0 && down == 0) return origin;

            bool isDown;
            ulong distance;

            // The distance comes before the side, so that a value nearer to the origin is simpler on either side.
            if (source.IsReplaying)
            {
                var distanceIndex = source.Length;
                distance = source.Read(up > down ? up : down);
                isDown = up != 0 && down != 0 ? ReadSide(source, distance) : up == 0;

                // A distance past the end of its side is the end of the side, and is recorded as the end.
                var sideMax = isDown ? down : up;
                if (distance > sideMax)
                {
                    distance = sideMax;
                    source.Rewrite(distanceIndex, distance);
                }
            }
            else
            {
                (isDown, distance) = bias == Gen.Bias.None
                    ? Uniform(source, up, down)
                    : Biased(source, up, down, boundsAreExtremes);

                source.Record(distance);
                if (up != 0 && down != 0)
                {
                    source.Record(isDown && distance != 0 ? 1UL : 0UL);
                }
            }

            return isDown
                ? unchecked((long)((ulong)origin - distance))
                : unchecked((long)((ulong)origin + distance));
        }

        /// <summary>
        /// Reads the side of a value. The side of the origin is always up, so that the origin has one sequence of
        /// choices.
        /// </summary>
        private static bool ReadSide(GenSource source, ulong distance)
        {
            var sideIndex = source.Length;
            var isDown = source.Read(1) == 1;
            if (isDown && distance == 0)
            {
                source.Rewrite(sideIndex, 0);
                return false;
            }

            return isDown;
        }

        public static int Int32(GenSource source, int min, int max, int origin, Gen.Bias bias) =>
            (int)Int64(source, min, max, origin, bias);

        /// <summary>
        /// Generates an index from 0 to max, which shrinks to 0. Uses the same choices as Int32(0, max).
        /// </summary>
        public static int Index(GenSource source, int max, Gen.Bias bias) =>
            max <= 0 ? 0 : (int)Int64(source, 0, max, 0, bias);

        /// <summary>
        /// Draws an integer from min to max with the distribution of the bias, and records no choice.
        /// </summary>
        public static int RandomInt32(GenSource source, int min, int max, Gen.Bias bias)
        {
            if (max <= min) return min;
            var (_, distance) = bias == Gen.Bias.None
                ? Uniform(source, (ulong)(max - min), 0)
                : Biased(source, (ulong)(max - min), 0, false);
            return min + (int)distance;
        }

        private static (bool, ulong) Uniform(GenSource source, ulong up, ulong down)
        {
            var value = source.NextBounded(up + down);
            return value < down ? (true, down - value) : (false, value - down);
        }

        private static (bool, ulong) Biased(GenSource source, ulong up, ulong down, bool boundsAreExtremes)
        {
            var size = source.Size;

            if (size > 50)
            {
                // At the bigger sizes, the extremes appear more often, as the old generators did.
                var normalWeight = size switch
                {
                    <= 60 => 16,
                    <= 70 => 8,
                    <= 80 => 4,
                    <= 90 => 2,
                    _ => 1
                };

                var pick = source.NextBounded((ulong)(normalWeight * 2 + 1));
                if (pick <= 1)
                {
                    var low = pick == 0 ? down != 0 || boundsAreExtremes : !(up != 0 || boundsAreExtremes);
                    var bound = low ? 1 : 2;

                    // The retries of a filter that rejected this bound draw a normal value instead.
                    if ((source.AvoidExtremes & bound) == 0)
                    {
                        source.ExtremesMade |= bound;
                        return low ? (true, down) : (false, up);
                    }
                }
            }

            var upLimit = Limit(up, size);
            var downLimit = Limit(down, size);
            var value = source.NextBounded(upLimit + downLimit);
            return value < downLimit ? (true, downLimit - value) : (false, value - downLimit);
        }

        /// <summary>
        /// The distance that a size allows: the number of bits grows linearly with the size.
        /// </summary>
        public static ulong Limit(ulong full, int size)
        {
            if (full == 0 || size >= 100) return full;
            var bits = 64 - BitOperations.LeadingZeroCount(full);
            var allowedBits = (size * bits + 99) / 100;
            var limit = allowedBits >= 64 ? ulong.MaxValue : (1UL << allowedBits) - 1;
            return limit < full ? limit : full;
        }

        public static long InferOrigin(long min, long max)
        {
            if (min > 0) return min;
            if (max < 0) return max;
            return 0;
        }
    }
}
