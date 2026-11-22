namespace GalaxyCheck.Gens.Parameters
{
    /// <summary>
    /// The seed and the size of a run, or of the test case of a counterexample.
    /// </summary>
    public record GenParameters(int Seed, Size Size)
    {
        public static GenParameters Parse(int seed, int size) => new GenParameters(seed, new Size(size));

        public override string ToString() => $"Seed={Seed},Size={Size}";
    }
}
