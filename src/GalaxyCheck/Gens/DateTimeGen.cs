using GalaxyCheck.Gens;
using GalaxyCheck.Gens.Internal;
using System;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>
        /// Creates a generator that produces datetimes. By default, it will generate values in the full range
        /// (<see cref="System.DateTime.MinValue"/> to <see cref="System.DateTime.MaxValue"/>), but the generator
        /// returned contains configuration methods to constrain the produced values further.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IDateTimeGen DateTime() => new DateTimeGen(
            FromDateTime: null,
            UntilDateTime: null);
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Constrains the generator so that it only produces values within the supplied datetime range (inclusive).
        /// </summary>
        /// <param name="x">The first bound of the range.</param>
        /// <param name="y">The second bound of the range.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        public static IDateTimeGen Within(this IDateTimeGen gen, DateTime x, DateTime y)
        {
            var fromDate = x > y ? y : x;
            var toDate = x > y ? x : y;
            return gen.From(fromDate).Until(toDate);
        }
    }
}

namespace GalaxyCheck.Gens
{
    /// <summary>A generator of DateTime values with optional bounds.</summary>
    public interface IDateTimeGen : IGen<DateTime>
    {
        /// <summary>
        /// Constrains the generator so that it only produces values after the given datetime.
        /// </summary>
        /// <param name="dateTime">The minimum (inclusive) datetime to generate.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        IDateTimeGen From(DateTime dateTime);

        /// <summary>
        /// Constrains the generator so that it only produces values before the given datetime.
        /// </summary>
        /// <param name="dateTime">The maximum (inclusive) datetime to generate.</param>
        /// <returns>A new generator with the constraint applied.</returns>
        IDateTimeGen Until(DateTime dateTime);
    }

    internal record DateTimeGen(
        DateTime? FromDateTime = null,
        DateTime? UntilDateTime = null) : Internal.BaseGen<DateTime>, IDateTimeGen
    {
        public IDateTimeGen From(DateTime dateTime) => this with { FromDateTime = dateTime };

        public IDateTimeGen Until(DateTime dateTime) => this with { UntilDateTime = dateTime };

        public override DateTime Generate(Parameters.GenSource source)
        {
            var min = FromDateTime ?? DateTime.MinValue;
            var max = UntilDateTime ?? DateTime.MaxValue;

            if (min > max)
            {
                throw new Internal.GenErrorSignal("'from' datetime cannot be after 'until' datetime");
            }

            var year = Int(source, min.Year, max.Year);

            var month = Int(
                source,
                year == min.Year ? min.Month : 1,
                year == max.Year ? max.Month : 12);

            var day = Int(
                source,
                year == min.Year && month == min.Month ? min.Day : 1,
                year == max.Year && month == max.Month ? max.Day : DateTime.DaysInMonth(year, month));

            var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);

            // Whole seconds are easier to read, and shrink faster. Ticks are only needed when a bound is on the same
            // day as the generated date.
            var wasConstrained = FromDateTime.HasValue || UntilDateTime.HasValue;
            var needsTicks = wasConstrained && (min.Date == date || max.Date == date);

            if (needsTicks)
            {
                var minTicks = min.Date == date ? min.TimeOfDay.Ticks : 0;
                var maxTicks = max.Date == date ? max.TimeOfDay.Ticks : TimeSpan.TicksPerDay - 1;
                var ticks = Internal.IntCore.Int64(source, minTicks, maxTicks, minTicks, Gen.Bias.WithSize);
                return date.AddTicks(ticks);
            }
            else
            {
                var minSeconds = min.Date == date ? (int)min.TimeOfDay.TotalSeconds : 0;
                var maxSeconds = max.Date == date ? (int)max.TimeOfDay.TotalSeconds : (int)(TimeSpan.FromHours(24).TotalSeconds - 1);
                var seconds = Int(source, minSeconds, maxSeconds);
                return date.AddSeconds(seconds);
            }
        }

        private static int Int(Parameters.GenSource source, int min, int max) =>
            Internal.IntCore.Int32(source, min, max, (int)Internal.IntCore.InferOrigin(min, max), Gen.Bias.WithSize);
    }
}
