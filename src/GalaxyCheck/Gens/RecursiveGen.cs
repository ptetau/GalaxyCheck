using System;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>Generates values of a recursive type. A leaf has depth 0.</summary>
        public static IGen<T> Recursive<T>(IGen<T> leafGen, Func<IGen<T>, IGen<T>> wrap, int maxDepth = 5) => throw new System.NotImplementedException("not implemented");
    }
}
