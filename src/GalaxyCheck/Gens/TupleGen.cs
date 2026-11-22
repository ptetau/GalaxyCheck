namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>
        /// Generates a tuple of two elements, where the values of the elements are sourced from the given generator.
        /// </para>
        /// <para>
        /// <code>
        /// Gen.Int32().Two(); // (0, 0), (0, 1), (1, 4) ...
        /// </code>
        /// </para>
        /// </summary>
        /// <param name="gen">The generator used to produce the elements of the tuple.</param>
        /// <returns>A new generator.</returns>
        public static IGen<(T, T)> Two<T>(this IGen<T> gen) => Gen.Zip(gen, gen);

        public static IGen<(T, T, T)> Three<T>(this IGen<T> gen) => Gen.Zip(gen, gen, gen);

        public static IGen<(T, T, T, T)> Four<T>(this IGen<T> gen) => Gen.Zip(gen, gen, gen, gen);
    }
}
