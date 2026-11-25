using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens.Generated;
using GalaxyCheck.Internal;
using System;
using System.Collections.Generic;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    [RequiresUnreferencedCode(ReflectionMessages.Path)]
    [RequiresDynamicCode(ReflectionMessages.Path)]
    [global::Verifiable.Contracts.Unverifiable]
    internal class RegistryReflectedGenHandler : IReflectedGenHandler
    {
        private readonly IReadOnlyDictionary<Type, Func<IGen>> _registeredGensByType;

        public RegistryReflectedGenHandler(IReadOnlyDictionary<Type, Func<IGen>> registeredGensByType)
        {
            _registeredGensByType = registeredGensByType;
        }

        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context) =>
            _registeredGensByType.ContainsKey(type);

        public IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            var gen = _registeredGensByType[type]();

            var genTypeArgument = gen.ReflectGenTypeArgument();
            if (type.IsAssignableFrom(genTypeArgument) == false)
            {
                return context.Error(type, $"type '{genTypeArgument}' was not assignable to the type it was registered to, '{type}'");
            }

            return gen;
        }
    }
}
