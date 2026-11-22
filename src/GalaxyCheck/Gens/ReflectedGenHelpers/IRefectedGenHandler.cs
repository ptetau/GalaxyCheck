using System;

namespace GalaxyCheck.Gens.ReflectedGenHelpers
{
    internal interface IReflectedGenHandler
    {
        bool CanHandleGen(Type type, ReflectedGenHandlerContext context);

        IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context);
    }

    /// <summary>A handler of collection types, which can make an empty collection to end a recursive type.</summary>
    internal interface IEmptyCollectionGenHandler
    {
        /// <summary>Returns a generator of the empty collection, or null when the type is not a collection.</summary>
        IGen? CreateEmptyGen(Type type);
    }
}
