using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens.Generated;
using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    /// <summary>
    /// Handles an interface or an abstract class. The values come from the candidate types that the user registered,
    /// or else from the concrete types in the assembly of the type that are assignable to it.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionMessages.Path)]
    [RequiresDynamicCode(ReflectionMessages.Path)]
    internal class ImplementationReflectedGenHandler : IReflectedGenHandler
    {
        private readonly ImmutableDictionary<Type, ImmutableArray<Type>> _candidateTypesByType;

        public ImplementationReflectedGenHandler(ImmutableDictionary<Type, ImmutableArray<Type>> candidateTypesByType)
        {
            _candidateTypesByType = candidateTypesByType;
        }

        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context) =>
            (type.IsInterface || type.IsAbstract) && !type.IsGenericTypeDefinition && !type.IsGenericParameter;

        public IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            var name = type.FullName ?? type.Name;
            ImmutableArray<Type> candidates;

            if (_candidateTypesByType.TryGetValue(type, out var registered) && !registered.IsEmpty)
            {
                var invalid = registered.FirstOrDefault(c => c.IsAbstract || c.IsInterface || !type.IsAssignableFrom(c));
                if (invalid != null)
                {
                    return context.Error(
                        type,
                        $"candidate type '{invalid.FullName ?? invalid.Name}' is not a concrete type that is assignable to '{name}'");
                }

                candidates = registered;
            }
            else
            {
                candidates = FindConcreteTypes(type);
            }

            if (candidates.IsEmpty)
            {
                // The same message as an unresolvable type, with the full name of the type.
                return context.UnresolvedTypeError(type, name);
            }

            var isRegistered = _candidateTypesByType.TryGetValue(type, out var r) && !r.IsEmpty;

            var gens = candidates
                .Select(candidate => innerHandler.CreateGen(candidate, context.Next($"<{candidate.Name}>", candidate, null)))
                .ToArray();

            if (!isRegistered)
            {
                // A found type that cannot be built is left out.
                gens = gens.Where(g => !IsError(g)).ToArray();

                if (gens.Length == 0)
                {
                    return context.UnresolvedTypeError(type, name);
                }
            }

            return OneOf.Create(type, gens.Select(g => g.Advanced).ToArray());
        }

        private static bool IsError(IGen gen) =>
            gen.GetType() is { IsGenericType: true } t && t.GetGenericTypeDefinition() == typeof(ErrorGen<>);

        private static ImmutableArray<Type> FindConcreteTypes(Type type)
        {
            Type?[] types;
            try
            {
                types = type.Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            return types
                .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
                            && !t.IsDefined(typeof(CompilerGeneratedAttribute), false)
                            && type.IsAssignableFrom(t))
                .Select(t => t!)
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToImmutableArray();
        }
    }

    [RequiresUnreferencedCode(ReflectionMessages.Path)]
    [RequiresDynamicCode(ReflectionMessages.Path)]
    internal static class OneOf
    {
        /// <summary>Makes a pick generator that is typed to the given type.</summary>
        public static IGen Create(Type type, IGenAdvanced[] gens) =>
            (IGen)Activator.CreateInstance(typeof(OneOfGen<>).MakeGenericType(type), new object[] { gens })!;
    }

    /// <summary>Picks one of the generators with equal chances. The pick shrinks toward the first generator.</summary>
    internal sealed record OneOfGen<T>(IGenAdvanced[] Gens) : BaseGen<T>
    {
        public override T Generate(GenSource source)
        {
            var index = IntCore.Index(source, Gens.Length - 1, Gen.Bias.None);
            return (T)Gens[index].GenerateObject(source)!;
        }
    }
}
