using Verifiable.Contracts;

namespace GalaxyCheck.Gens.Internal
{
    internal enum RangeIntentionKind : byte
    {
        Unspecified = 0,
        Exact = 1,
        Bounded = 2,
    }

    [Verifiable]
    internal readonly record struct RangeIntention(
        RangeIntentionKind Kind,
        int ExactValue,
        int? Minimum,
        int? Maximum)
    {
        public static RangeIntention Unspecified() => new(RangeIntentionKind.Unspecified, 0, null, null);

        public static RangeIntention Exact(int value) => new(RangeIntentionKind.Exact, value, null, null);

        public static RangeIntention Bounded(int? minimum, int? maximum) => new(RangeIntentionKind.Bounded, 0, minimum, maximum);
    }

    [Verifiable]
    internal static class RangeIntentionExtensions
    {
        public static RangeIntention WithMinimum(this RangeIntention r, int minimum) => r.Kind switch
        {
            RangeIntentionKind.Bounded => RangeIntention.Bounded(minimum, r.Maximum),
            _ => RangeIntention.Bounded(minimum, null),
        };

        public static RangeIntention WithMaximum(this RangeIntention r, int maximum) => r.Kind switch
        {
            RangeIntentionKind.Bounded => RangeIntention.Bounded(r.Minimum, maximum),
            _ => RangeIntention.Bounded(null, maximum),
        };
    }
}
