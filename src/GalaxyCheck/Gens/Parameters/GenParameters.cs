using Verifiable.Contracts;

namespace GalaxyCheck.Gens.Parameters
{
    /// <summary>
    /// The seed and the size of a run, or of the test case of a counterexample.
    /// </summary>
    /// <param name="Seed">The seed of the random choices.</param>
    /// <param name="Size">The size, from 0 to 100.</param>
    [Verifiable]
    public record GenParameters(int Seed, Size Size)
    {
        /// <summary>Creates the parameters from a seed and a size.</summary>
        [Unverifiable]
        public static GenParameters Parse(int seed, int size) => new GenParameters(seed, new Size(size));

        /// <summary>Creates the parameters from a seed and a verified <see cref="BoundedSize"/>.</summary>
        public static GenParameters FromBounded(int seed, BoundedSize size) => new GenParameters(seed, Size.From(size));

        /// <summary>Returns the seed and the size as text.</summary>
        public override string ToString() => $"Seed={Seed},Size={Size}";
    }
}
