using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens.Generated;
using GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GalaxyCheck.Gens.ReflectedGenHelpers
{
    [RequiresUnreferencedCode(ReflectionMessages.Path)]
    [RequiresDynamicCode(ReflectionMessages.Path)]
    internal static class ReflectedGenBuilder
    {
        public static IGen Build(
            Type type,
            IReadOnlyDictionary<Type, Func<IGen>> registeredGensByType,
            IReadOnlyList<ReflectedGenMemberOverride> memberOverrides,
            ImmutableDictionary<Type, ImmutableArray<Type>> candidateTypesByType,
            ReflectedGenHandlerContext context)
        {
            var genFactoriesByPriority = new List<IReflectedGenHandler>
            {
                new MemberOverrideReflectedGenHandler(memberOverrides),
                new NullableGenHandler(),
                new RegistryReflectedGenHandler(registeredGensByType),
#if NET11_0_OR_GREATER
                new UnionReflectedGenHandler(),
#endif
                new CollectionReflectedGenHandler(),
                new DictionaryReflectedGenHandler(),
                new ArrayReflectedGenHandler(),
                new EnumReflectedGenHandler(),
                new ImplementationReflectedGenHandler(candidateTypesByType),
                new DefaultConstructorReflectedGenHandler(),
                new NonDefaultConstructorReflectedGenHandler(),
            };

            var compositeReflectedGenFactory = new CompositeReflectedGenHandler(genFactoriesByPriority);

            try
            {
                return compositeReflectedGenFactory.CreateGen(type, context);
            }
            catch (Exception ex)
            {
                return Gen.Advanced.Error(type, $"{ex.Message} \n {ex.StackTrace}");
            }
        }
    }
}
