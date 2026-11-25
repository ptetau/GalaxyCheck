using System;

namespace GalaxyCheck.Gens.ReflectedGenHelpers
{
    [global::Verifiable.Contracts.Unverifiable]
    internal static class ReflectedGenHandlerExtensions
    {
        public static IGen CreateGen(
            this IReflectedGenHandler innerHandler,
            Type type,
            ReflectedGenHandlerContext context) =>
                innerHandler.CreateGen(innerHandler, type, context);
    }
}
