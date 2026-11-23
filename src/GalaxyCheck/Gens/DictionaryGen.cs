using System.Collections.Generic;

namespace GalaxyCheck.Gens
{
    /// <summary>A generator of dictionaries with distinct keys.</summary>
    public interface IDictionaryGen<TKey, TValue> : IGen<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
    {
        /// <summary>Gives each dictionary exactly this count of entries.</summary>
        IDictionaryGen<TKey, TValue> WithCount(int count);

        /// <summary>Gives each dictionary at least this count of entries.</summary>
        IDictionaryGen<TKey, TValue> WithCountGreaterThanEqual(int minCount);

        /// <summary>Gives each dictionary at most this count of entries.</summary>
        IDictionaryGen<TKey, TValue> WithCountLessThanEqual(int maxCount);

        /// <summary>Gives each dictionary a count of entries between the two bounds, both inclusive.</summary>
        IDictionaryGen<TKey, TValue> WithCountBetween(int x, int y);
    }
}

namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using GalaxyCheck.Gens.Internal;

    public static partial class Gen
    {
        /// <summary>
        /// Generates dictionaries with distinct keys. By default the count is from 0 to 20. The count shrinks first,
        /// then the keys and the values.
        /// </summary>
        public static IDictionaryGen<TKey, TValue> Dictionary<TKey, TValue>(IGen<TKey> keyGen, IGen<TValue> valueGen)
            where TKey : notnull => new DictionaryGen<TKey, TValue>(keyGen, valueGen, RangeIntention.Unspecified());
    }
}

namespace GalaxyCheck.Gens.Internal
{
    using GalaxyCheck.Gens.Parameters;
    using System;
    using System.Collections.Generic;

    internal sealed record DictionaryGen<TKey, TValue>(
        IGen<TKey> KeyGen,
        IGen<TValue> ValueGen,
        RangeIntention Count) : BaseGen<IReadOnlyDictionary<TKey, TValue>>, IDictionaryGen<TKey, TValue>
        where TKey : notnull
    {
        private const int MaxAttemptsPerKey = 100;

        public IDictionaryGen<TKey, TValue> WithCount(int count) => this with { Count = RangeIntention.Exact(count) };

        public IDictionaryGen<TKey, TValue> WithCountGreaterThanEqual(int minCount) =>
            this with { Count = Count.WithMinimum(minCount) };

        public IDictionaryGen<TKey, TValue> WithCountLessThanEqual(int maxCount) =>
            this with { Count = Count.WithMaximum(maxCount) };

        public IDictionaryGen<TKey, TValue> WithCountBetween(int x, int y) =>
            WithCountGreaterThanEqual(Math.Min(x, y)).WithCountLessThanEqual(Math.Max(x, y));

        public override IReadOnlyDictionary<TKey, TValue> Generate(GenSource source)
        {
            var (minCount, maxCount) = CountRange.Resolve(Count, enableCountLimit: true, CountRange.DictionaryLimitError);
            var keyGen = KeyGen.Advanced;
            var valueGen = ValueGen.Advanced;

            var targetCount = source.IsReplaying ? 0 : IntCore.RandomInt32(source, minCount, maxCount, Gen.Bias.WithSize);
            var dictionary = new Dictionary<TKey, TValue>();

            var size = source.Size;
            try
            {
                while (dictionary.Count < maxCount)
                {
                    var entryStart = source.BeginSpan();
                    if (dictionary.Count >= minCount)
                    {
                        var more = source.IsReplaying
                            ? source.Read(1) == 1
                            : source.Record(dictionary.Count < targetCount ? 1UL : 0UL) == 1;

                        if (!more)
                        {
                            break;
                        }
                    }

                    TKey? key = default;
                    var added = false;
                    for (var attempt = 0; attempt < MaxAttemptsPerKey; attempt++)
                    {
                        var start = source.BeginSpan();
                        key = keyGen.Generate(source);
                        source.EndSpan(start);

                        if (!dictionary.ContainsKey(key))
                        {
                            added = true;
                            break;
                        }

                        if ((attempt + 1) % 5 == 0 && !source.IsReplaying)
                        {
                            // A bigger size may hold more distinct keys.
                            source.Size = Math.Min(100, source.Size + 5);
                        }
                    }

                    if (!added)
                    {
                        source.EndSpan(entryStart);

                        if (dictionary.Count < minCount)
                        {
                            throw new Exceptions.GenExhaustionException(
                                $"the key generator gave too few distinct keys: {dictionary.Count} found, but the minimum count is {minCount}");
                        }

                        // The key generator has no more distinct values. Stop here, as a "stop" choice would.
                        break;
                    }

                    // The value is generated at the size of the caller, not at a size that the key retries raised.
                    var valueSize = source.Size;
                    source.Size = size;
                    var value = valueGen.Generate(source);
                    source.Size = valueSize;

                    dictionary.Add(key!, value);
                    source.EndSpan(entryStart);
                }
            }
            finally
            {
                source.Size = size;
            }

            return dictionary;
        }
    }
}
