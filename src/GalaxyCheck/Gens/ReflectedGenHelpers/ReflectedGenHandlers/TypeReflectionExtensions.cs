using System;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    [global::Verifiable.Contracts.Unverifiable]
    internal static class TypeReflectionExtensions
    {
        public static bool IsNullableStruct(this Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                return genericTypeDefinition == typeof(Nullable<>);
            }

            return false;
        }
    }
}
