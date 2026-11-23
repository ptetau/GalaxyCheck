namespace GalaxyCheck
{
    using GalaxyCheck.Gens.Enum;
    using System;
    using System.Linq;
    using System.Reflection;

    public static partial class Gen
    {
        public static IGen<T> Enum<T>()
            where T : struct
        {
            var type = typeof(T);
            if (!type.IsEnum)
            {
                return Advanced.Error<T>($"Type '{type}' is not an enum");
            }

            var isFlagsEnum = type.GetCustomAttributes<FlagsAttribute>().Any();
            return isFlagsEnum
                ? EnumGenHelpers.GenFlagsEnum<T>()
                : EnumGenHelpers.GenNonFlagsEnum<T>();
        }
    }
}

namespace GalaxyCheck.Gens.Enum
{
    using System.Linq;

    internal static class EnumGenHelpers
    {
        public static IGen<T> GenFlagsEnum<T>()
        {
            var values = System.Enum.GetValuesAsUnderlyingType(typeof(T)).Cast<object>().Select(System.Convert.ToInt32);
            var gen = Gen
                .Element(values)
                .SetOf()
                .WithCountBetween(1, values.Count())
                .Select(xs => xs.Aggregate(0, (acc, curr) => acc | curr))
                .Cast<T>();

            return new FlagsEnumGen<T>(gen, values.ToArray());
        }

        public static IGen<T> GenNonFlagsEnum<T>() => Gen.Element(System.Enum
            .GetValuesAsUnderlyingType(typeof(T))
            .Cast<object>()
            .Select(value => (T)System.Enum.ToObject(typeof(T), value))
            .ToArray());
    }
}

namespace GalaxyCheck.Gens.Enum
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The generator of a flags enum: a non-empty set of its values, combined with "or". It is finite: its values are
    /// the different combinations, and the choices of each are the choices of the first set that makes it.
    /// </summary>
    internal sealed record FlagsEnumGen<T>(IGen<T> Source, int[] Values) : BaseGen<T>, GalaxyCheck.Properties.IFiniteGen
    {
        private const int MaxValues = 16;

        private List<ulong[]>? _combinations;

        public override T Generate(GenSource source) => Source.Advanced.Generate(source);

        public long? Count(long limit)
        {
            var combinations = Combinations();
            return combinations != null && combinations.Count <= limit ? combinations.Count : null;
        }

        public void WriteChoices(long index, List<ulong> choices) => choices.AddRange(Combinations()![(int)index]);

        /// <summary>
        /// The choices of each different combination, in the order of the first set that makes it. The set
        /// generator reads the index of the first element, then for each next element a "more" choice of 1 and its
        /// index, and a "more" choice of 0 when the set can grow further.
        /// </summary>
        private List<ulong[]>? Combinations()
        {
            if (_combinations != null) return _combinations;

            var n = Values.Length;
            if (n == 0 || n > MaxValues || Values.Distinct().Count() != n) return null;

            var seen = new HashSet<int>();
            var combinations = new List<ulong[]>();
            for (var mask = 1; mask < 1 << n; mask++)
            {
                var combined = 0;
                var choices = new List<ulong>();
                var count = 0;
                for (var i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) == 0) continue;
                    combined |= Values[i];
                    if (count > 0) choices.Add(1);
                    if (n > 1) choices.Add((ulong)i);
                    count++;
                }

                if (count < n) choices.Add(0);
                if (seen.Add(combined)) combinations.Add(choices.ToArray());
            }

            _combinations = combinations;
            return combinations;
        }
    }
}
