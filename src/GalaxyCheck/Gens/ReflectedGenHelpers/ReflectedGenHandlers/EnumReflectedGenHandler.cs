using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens.Generated;
using System;
using System.Reflection;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    [RequiresUnreferencedCode(ReflectionMessages.Path)]
    [RequiresDynamicCode(ReflectionMessages.Path)]
    internal class EnumReflectedGenHandler : IReflectedGenHandler
    {
        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context) => type.IsEnum;

        public IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            var genMethodInfo = typeof(Gen).GetMethod(nameof(Gen.Enum), BindingFlags.Public | BindingFlags.Static)!;
            var genericGenMethodInfo = genMethodInfo.MakeGenericMethod(type);
            return (IGen)genericGenMethodInfo.Invoke(null!, new object[] { })!;
        }
    }
}
