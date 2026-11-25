namespace GalaxyCheck
{
    using GalaxyCheck.Gens;

    public static partial class Gen
    {
        /// <summary>
        /// Creates a generator that always produces the given value.
        /// </summary>
        /// <param name="value">The constant value the generator should produce.</param>
        /// <returns>The new generator.</returns>
        public static IGen<T> Constant<T>(T value) => new ConstantGen<T>(value);
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;

    [global::Verifiable.Contracts.Unverifiable]
    internal sealed record ConstantGen<T>(T Value) : BaseGen<T>
    {
        public override T Generate(GenSource source) => Value;
    }
}
