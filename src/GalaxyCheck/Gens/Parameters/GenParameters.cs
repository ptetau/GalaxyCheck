namespace GalaxyCheck.Gens.Parameters
{
    /// <summary>
    /// The seed and the size of a run, or of the test case of a counterexample.
    /// </summary>
    /// <param name="Seed">The seed of the random choices.</param>
    /// <param name="Size">The size, from 0 to 100.</param>
    public record GenParameters(int Seed, Size Size)
    {
        /// <summary>Creates the parameters from a seed and a size.</summary>
        public static GenParameters Parse(int seed, int size) => new GenParameters(seed, new Size(size));

        /// <summary>Returns the seed and the size as text.</summary>
        public override string ToString() => $"Seed={Seed},Size={Size}";
    }
}
