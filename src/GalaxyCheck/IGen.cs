using GalaxyCheck.Gens.Parameters;

namespace GalaxyCheck
{
    /// <summary>The advanced operations of a generator.</summary>
    public interface IGenAdvanced
    {
        /// <summary>
        /// Generates one value from the given source of choices. The runners create the source; it is recommended to
        /// use `Sample()`, `Check()` or `Minimum()` instead.
        /// </summary>
        /// <param name="source">The source of the choices of the generator.</param>
        /// <returns>The generated value.</returns>
        object? GenerateObject(GenSource source);
    }

    /// <summary>
    /// A random data generator.
    /// </summary>
    public interface IGen
    {
        /// <summary>The advanced operations of the generator, for example generating from a source of choices.</summary>
        IGenAdvanced Advanced { get; }
    }

    /// <summary>The advanced operations of a generator of <typeparamref name="T"/>.</summary>
    public interface IGenAdvanced<out T> : IGenAdvanced
    {
        /// <summary>
        /// Generates one value from the given source of choices. The runners create the source; it is recommended to
        /// use `Sample()`, `Check()` or `Minimum()` instead.
        /// </summary>
        /// <param name="source">The source of the choices of the generator.</param>
        /// <returns>The generated value.</returns>
        T Generate(GenSource source);
    }

    /// <summary>
    /// A random data generator.
    /// </summary>
    /// <typeparam name="T">The type of the generator's values.</typeparam>
    public interface IGen<out T> : IGen
    {
        /// <summary>The advanced operations of the generator, for example generating from a source of choices.</summary>
        new IGenAdvanced<T> Advanced { get; }
    }
}
