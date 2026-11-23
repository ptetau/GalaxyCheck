using GalaxyCheck.Gens;
using GalaxyCheck.Gens.Parameters;
using System;
using System.Reflection;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>
        /// Generates instances of the given type, using the default <see cref="IGenFactory"/>. The auto-generator
        /// can not be configured as precisely as more specialized generators can be, but it can create complex types
        /// with minimal configuration through reflection.
        /// </summary>
        /// <returns>A generator for the given type.</returns>
        public static IReflectedGen<T> Create<T>(NullabilityInfo? nullabilityInfo = null) where T : notnull => Factory().Create<T>(nullabilityInfo);

        /// <summary>
        /// Generates instances of the given type, using the default <see cref="IGenFactory"/>. The auto-generator
        /// can not be configured as precisely as more specialized generators can be, but it can create complex types
        /// with minimal configuration through reflection.
        /// </summary>
        /// <returns>A generator for the given type.</returns>
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(Gens.Generated.ReflectionMessages.Path)]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode(Gens.Generated.ReflectionMessages.Path)]
        public static IGen<object> Create(Type type, NullabilityInfo? nullabilityInfo = null) => Factory().Create(type, nullabilityInfo);

        /// <summary>
        /// Creates a generator from a function of the generation parameters. The seed of the parameters is a choice
        /// that does not shrink, so the values of the generator do not shrink.
        /// </summary>
        /// <param name="func">The function that creates a value from the parameters.</param>
        /// <returns>The new generator.</returns>
        public static IGen<T> Create<T>(Func<GenParameters, (T value, GenParameters nextParameters)> func) =>
            new Gens.Internal.FunctionGen<T>(source =>
            {
                var seed = (int)(uint)source.DrawUniform(uint.MaxValue);
                source.FreezeLast();
                return func(GenParameters.Parse(seed, source.Size)).value;
            });
    }
}
