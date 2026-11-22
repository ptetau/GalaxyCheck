using System.Collections.Generic;

namespace GalaxyCheck.Internal
{
    internal static class EnumerableExtensions
    {
        public static void Deconstruct<T>(this IEnumerable<T> source, out T? head, out IEnumerable<T> tail)
        {
            var enumerator = source.GetEnumerator();
            // Current throws on some runtimes if the source is empty.
            head = enumerator.MoveNext() ? enumerator.Current : default;
            tail = EnumerateTail(enumerator);
        }

        private static IEnumerable<T> EnumerateTail<T>(IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext()) yield return enumerator.Current;
        }
    }
}
