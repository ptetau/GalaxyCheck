using GalaxyCheck.Gens.Parameters;

namespace GalaxyCheck
{
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
        IGenAdvanced Advanced { get; }
    }

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
        new IGenAdvanced<T> Advanced { get; }
    }
}
