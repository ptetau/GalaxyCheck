namespace GalaxyCheck.Gens
{
    /// <summary>A generator of values in an inclusive range.</summary>
    public interface IRangeGen<T> : IGen<T>
    {
        /// <summary>Generates values at or after the minimum.</summary>
        IRangeGen<T> From(T min);

        /// <summary>Generates values at or before the maximum.</summary>
        IRangeGen<T> Until(T max);

        /// <summary>Generates values between the two bounds, both inclusive.</summary>
        IRangeGen<T> Within(T x, T y);
    }
}

namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using GalaxyCheck.Gens.Internal;

    public static partial class Gen
    {
        /// <summary>Generates <c>TimeSpan</c> values over the full range. Values shrink toward <c>TimeSpan.Zero</c>.</summary>
        public static IRangeGen<System.TimeSpan> TimeSpan() => new TimeSpanGen();

        /// <summary>
        /// Generates <c>DateTimeOffset</c> values over the full range, with offsets from -14 to +14 hours. Values
        /// shrink toward <c>2000-01-01T00:00:00+00:00</c>.
        /// </summary>
        public static IRangeGen<System.DateTimeOffset> DateTimeOffset() => new DateTimeOffsetGen();

        /// <summary>Generates <c>DateOnly</c> values over the full range. Values shrink toward 2000-01-01.</summary>
        public static IRangeGen<System.DateOnly> DateOnly() => new DateOnlyGen();

        /// <summary>Generates <c>TimeOnly</c> values over the full range. Values shrink toward 00:00.</summary>
        public static IRangeGen<System.TimeOnly> TimeOnly() => new TimeOnlyGen();
    }
}

namespace GalaxyCheck.Gens.Internal
{
    using GalaxyCheck.Gens.Parameters;
    using System;

    internal static class TimeCore
    {
        /// <summary>
        /// Generates a number of ticks (or days) from min to max. The origin is the default origin, or the bound
        /// nearest to it.
        /// </summary>
        public static long Generate(GenSource source, long min, long max, long defaultOrigin)
        {
            if (min > max)
            {
                throw new GenErrorSignal(
                    "'min' cannot be greater than 'max'" + Environment.NewLine +
                    "Fix: give a .From(...) bound that is not after the .Until(...) bound, or call .Within(x, y), which takes its two bounds in either order.");
            }

            var origin = Math.Clamp(defaultOrigin, min, max);
            return IntCore.Int64(source, min, max, origin, Gen.Bias.WithSize, boundsAreExtremes: true);
        }
    }

    internal sealed record TimeSpanGen(TimeSpan? Min = null, TimeSpan? Max = null) : BaseGen<TimeSpan>, IRangeGen<TimeSpan>
    {
        public IRangeGen<TimeSpan> From(TimeSpan min) => this with { Min = min };

        public IRangeGen<TimeSpan> Until(TimeSpan max) => this with { Max = max };

        public IRangeGen<TimeSpan> Within(TimeSpan x, TimeSpan y) => x > y
            ? this with { Min = y, Max = x }
            : this with { Min = x, Max = y };

        public override TimeSpan Generate(GenSource source) => new(TimeCore.Generate(
            source,
            (Min ?? TimeSpan.MinValue).Ticks,
            (Max ?? TimeSpan.MaxValue).Ticks,
            0));
    }

    internal sealed record DateOnlyGen(DateOnly? Min = null, DateOnly? Max = null) : BaseGen<DateOnly>, IRangeGen<DateOnly>
    {
        private static readonly long OriginDays = new DateOnly(2000, 1, 1).DayNumber;

        public IRangeGen<DateOnly> From(DateOnly min) => this with { Min = min };

        public IRangeGen<DateOnly> Until(DateOnly max) => this with { Max = max };

        public IRangeGen<DateOnly> Within(DateOnly x, DateOnly y) => x > y
            ? this with { Min = y, Max = x }
            : this with { Min = x, Max = y };

        public override DateOnly Generate(GenSource source) => DateOnly.FromDayNumber((int)TimeCore.Generate(
            source,
            (Min ?? DateOnly.MinValue).DayNumber,
            (Max ?? DateOnly.MaxValue).DayNumber,
            OriginDays));
    }

    internal sealed record TimeOnlyGen(TimeOnly? Min = null, TimeOnly? Max = null) : BaseGen<TimeOnly>, IRangeGen<TimeOnly>
    {
        public IRangeGen<TimeOnly> From(TimeOnly min) => this with { Min = min };

        public IRangeGen<TimeOnly> Until(TimeOnly max) => this with { Max = max };

        public IRangeGen<TimeOnly> Within(TimeOnly x, TimeOnly y) => x > y
            ? this with { Min = y, Max = x }
            : this with { Min = x, Max = y };

        public override TimeOnly Generate(GenSource source) => new(TimeCore.Generate(
            source,
            (Min ?? TimeOnly.MinValue).Ticks,
            (Max ?? TimeOnly.MaxValue).Ticks,
            0));
    }

    /// <summary>
    /// Generates the UTC ticks, then the offset in minutes. The offset range keeps the local time in the range of the
    /// type.
    /// </summary>
    internal sealed record DateTimeOffsetGen(DateTimeOffset? Min = null, DateTimeOffset? Max = null)
        : BaseGen<DateTimeOffset>, IRangeGen<DateTimeOffset>
    {
        private const long MaxOffsetMinutes = 14 * 60;

        private static readonly long OriginTicks = new DateTime(2000, 1, 1).Ticks;

        public IRangeGen<DateTimeOffset> From(DateTimeOffset min) => this with { Min = min };

        public IRangeGen<DateTimeOffset> Until(DateTimeOffset max) => this with { Max = max };

        public IRangeGen<DateTimeOffset> Within(DateTimeOffset x, DateTimeOffset y) => x > y
            ? this with { Min = y, Max = x }
            : this with { Min = x, Max = y };

        public override DateTimeOffset Generate(GenSource source)
        {
            var utcTicks = TimeCore.Generate(
                source,
                (Min ?? DateTimeOffset.MinValue).UtcTicks,
                (Max ?? DateTimeOffset.MaxValue).UtcTicks,
                OriginTicks);

            // The local ticks (UTC ticks plus the offset) must stay from 0 to DateTime.MaxValue.Ticks.
            // The choice has a fixed range, so its meaning does not depend on the ticks. The clamp comes after.
            var lowMinutes = Math.Max(-MaxOffsetMinutes, -(utcTicks / TimeSpan.TicksPerMinute));
            var highMinutes = Math.Min(MaxOffsetMinutes, (DateTime.MaxValue.Ticks - utcTicks) / TimeSpan.TicksPerMinute);

            var drawn = IntCore.Int64(
                source,
                -MaxOffsetMinutes,
                MaxOffsetMinutes,
                0,
                Gen.Bias.WithSize,
                boundsAreExtremes: true);

            var minutes = Math.Clamp(drawn, lowMinutes, highMinutes);

            var offset = TimeSpan.FromMinutes(minutes);
            return new DateTimeOffset(utcTicks + offset.Ticks, offset);
        }
    }
}
