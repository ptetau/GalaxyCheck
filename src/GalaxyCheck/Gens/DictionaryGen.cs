using System.Collections.Generic;

namespace GalaxyCheck.Gens
{
    /// <summary>A generator of dictionaries with distinct keys.</summary>
    public interface IDictionaryGen<TKey, TValue> : IGen<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
    {
        IDictionaryGen<TKey, TValue> WithCount(int count);

        IDictionaryGen<TKey, TValue> WithCountGreaterThanEqual(int minCount);

        IDictionaryGen<TKey, TValue> WithCountLessThanEqual(int maxCount);

        IDictionaryGen<TKey, TValue> WithCountBetween(int x, int y);
    }
}

namespace GalaxyCheck
{
    using GalaxyCheck.Gens;

    public static partial class Gen
    {
        public static IDictionaryGen<TKey, TValue> Dictionary<TKey, TValue>(IGen<TKey> keyGen, IGen<TValue> valueGen)
            where TKey : notnull => throw new System.NotImplementedException("not implemented");
    }
}
