using Verifiable.Contracts;

namespace GalaxyCheck.Gens.Internal
{
    /// <summary>
    /// Chooses the size of each retry after a discard, for a filter in a generator and for a precondition. The
    /// retries try three sizes near the sizes where values passed (the good size) for each step of a sweep that steps
    /// round the whole range 0 to 100, so that a filter passes whether it favours small, large, or middle sizes. The
    /// good size comes from the earlier retries of the same run, so a filter that passes at few sizes finds them again
    /// at once. A search with no good size only sweeps.
    /// </summary>
    [Verifiable]
    internal static class SizeSearch
    {
        public const int NoGoodSize = -1;

        // A step that is coprime with 101 visits each size of the cycle, and a step near 101 / golden ratio spreads
        // the first sizes over the whole range.
        private const int SweepStep = 38;

        private const int NearStep = 2;

        /// <summary>The size of a retry. The first retry has the number 1.</summary>
        public static int Next(int baseSize, int retry, int goodSize)
        {
            Spec.Requires(
                baseSize >= 0 && baseSize <= 100 && retry >= 1 && (goodSize == NoGoodSize || (goodSize >= 0 && goodSize <= 100)),
                "Base size and learned good size stay in 0..100 (or NoGoodSize) and retry is positive.");
            Spec.Ensures<int>(r => r >= 0 && r <= 100, "Every retry size stays within [0, 100].");

            if (goodSize == NoGoodSize)
            {
                return Sweep(baseSize, retry);
            }

            // Three retries near the good size, then one step of the sweep.
            if (retry % 4 == 0)
            {
                return Sweep(baseSize, retry / 4);
            }

            var near = retry - 1 - retry / 4;
            var offset = (near + 1) / 2 * NearStep;
            var size = near % 2 == 1 ? goodSize + offset : goodSize - offset;
            return size < 0 ? 0 : size > 100 ? 100 : size;
        }

        /// <summary>
        /// The good size after a value passed at a retry size. The good size moves a quarter of the way to that
        /// size, so that it settles in the middle of the sizes where values pass.
        /// </summary>
        public static int Learn(int goodSize, int size)
        {
            Spec.Requires(
                size >= 0 && size <= 100 && (goodSize == NoGoodSize || (goodSize >= 0 && goodSize <= 100)),
                "Observed retry size is within 0..100 and goodSize is either NoGoodSize or within 0..100.");
            Spec.Ensures<int>(r => r >= 0 && r <= 100, "Learned good size remains within [0, 100].");

            return goodSize == NoGoodSize ? size : (3 * goodSize + size + 2) / 4;
        }

        private static int Sweep(int baseSize, int step)
        {
            Spec.Requires(baseSize >= 0 && baseSize <= 100 && step >= 1, "Sweep base size is within 0..100 and step is positive.");
            Spec.Ensures<int>(r => r >= 0 && r <= 100, "Sweep modulo 101 stays within [0, 100].");
            return (int)((baseSize + (long)step * SweepStep) % 101);
        }
    }
}
