namespace GalaxyCheck.Gens
{
    /// <summary>A generator of values in an inclusive range.</summary>
    public interface IRangeGen<T> : IGen<T>
    {
        IRangeGen<T> From(T min);

        IRangeGen<T> Until(T max);

        IRangeGen<T> Within(T x, T y);
    }
}

namespace GalaxyCheck
{
    using GalaxyCheck.Gens;

    public static partial class Gen
    {
        public static IRangeGen<System.TimeSpan> TimeSpan() => throw new System.NotImplementedException("not implemented");

        public static IRangeGen<System.DateTimeOffset> DateTimeOffset() => throw new System.NotImplementedException("not implemented");

        public static IRangeGen<System.DateOnly> DateOnly() => throw new System.NotImplementedException("not implemented");

        public static IRangeGen<System.TimeOnly> TimeOnly() => throw new System.NotImplementedException("not implemented");

    }
}
