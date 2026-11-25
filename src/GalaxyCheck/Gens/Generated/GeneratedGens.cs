using System;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace GalaxyCheck.Gens.Generated
{
    /// <summary>
    /// The generators that the GalaxyCheck.SourceGeneration package writes at compile time. The generated code of a
    /// project registers each one when the project loads. Then <see cref="Gen.Create{T}"/> uses it in place of the
    /// reflection path. Do not call it from your own code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [global::Verifiable.Contracts.Unverifiable]
    public static class GeneratedGens
    {
        private static readonly ConcurrentDictionary<Type, Func<IGen>> CreatesByType = new();

        /// <summary>Registers the generated generator of the type <typeparamref name="T"/>.</summary>
        /// <param name="create">The function that makes the generator.</param>
        public static void Register<T>(Func<IGen<T>> create)
        {
            if (create is null) throw new ArgumentNullException(nameof(create));
            CreatesByType[typeof(T)] = create;
        }

        /// <summary>Gives the generated generator of the type, if a project registered one.</summary>
        internal static bool TryGet<T>(out IGen<T> gen)
        {
            if (CreatesByType.TryGetValue(typeof(T), out var create) && create() is IGen<T> typed)
            {
                gen = typed;
                return true;
            }

            gen = null!;
            return false;
        }
    }

    /// <summary>
    /// Helpers that the generated code calls, so that it makes the same values as the reflection path.
    /// Do not call them from your own code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [global::Verifiable.Contracts.Unverifiable]
    public static class GeneratedGenHelpers
    {
        /// <summary>
        /// Gives the generator of a member at the path, with the random state of the reflection path for that path.
        /// </summary>
        /// <param name="gen">The generator of the member.</param>
        /// <param name="memberPath">The path of the member, for example "$.Id".</param>
        public static IGen<object> Member<T>(IGen<T> gen, string memberPath) =>
            gen.Cast<object>().Advanced.ReferenceRngWaypoint(ReflectedGenHelpers.ReflectedGenHandlerContextExtensions.StableSeed(memberPath));

        /// <summary>Zips the generators of the members, in the order of the parameters of the constructor.</summary>
        /// <param name="gens">The generators of the members.</param>
        public static IGen<object[]> Members(params IGen<object>[] gens) =>
            Gen.Zip(gens).Select(values => values as object[] ?? System.Linq.Enumerable.ToArray(values));
    }
}
