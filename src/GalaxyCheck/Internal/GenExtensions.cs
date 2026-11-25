using System;
using System.Linq;

namespace GalaxyCheck.Internal
{
    [global::Verifiable.Contracts.Unverifiable]
    internal static class GenExtensions
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        public static Type ReflectGenTypeArgument(this IGen gen)
        {
            var reflectedGenType = gen
                .GetType()
                .GetInterfaces()
                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IGen<>))
                .Single();

            return reflectedGenType.GetGenericArguments().Single();
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        public static Type ReflectGenTypeArgument(this Func<IGen> gen)
        {
            var genType = gen.GetType().GetGenericArguments().Single();

            var reflectedGenType = genType
                .GetInterfaces()
                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IGen<>))
                .Single();

            return reflectedGenType.GetGenericArguments().Single();
        }
    }
}
