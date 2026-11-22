using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    internal class CompositeReflectedGenHandler : IReflectedGenHandler
    {
        private readonly IReadOnlyList<IReflectedGenHandler> _genHandlersByPriority;

        public CompositeReflectedGenHandler(IReadOnlyList<IReflectedGenHandler> genHandlersByPriority)
        {
            _genHandlersByPriority = genHandlersByPriority;
        }

        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context) =>
            _genHandlersByPriority.Any(genFactory => genFactory.CanHandleGen(type, context));

        /// <summary>
        /// Builds the generator of the type, and applies the depth limit of a type that refers to itself, for each
        /// handler. Beyond the limit, the nearest member that can end the value (a member that can be null, or a
        /// collection) gives null or an empty collection. A cycle with no such member gives an error when the member
        /// is a non-nullable reference, and null at the limit when its nullability is unknown (an oblivious project).
        /// </summary>
        public IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            try
            {
                if (context.IsBeyondDepthLimit(type))
                {
                    if (context.CanEndCycle(type))
                    {
                        throw new DepthLimitException(type);
                    }

                    return type.IsValueType || context.IsNonNullableReference()
                        ? context.Error(type, $"detected circular reference on type '{type}'")
                        : CreateNullGen(type);
                }

                return CreateGenUnlimited(innerHandler, type, context);
            }
            catch (DepthLimitException ex) when (context.TypeHistory.Contains(ex.Type) && TryCreateEndGen(type, context, out var endGen))
            {
                return endGen;
            }
        }

        private bool TryCreateEndGen(Type type, ReflectedGenHandlerContext context, out IGen gen)
        {
            gen = _genHandlersByPriority
                .OfType<IEmptyCollectionGenHandler>()
                .Select(handler => handler.CreateEmptyGen(type))
                .FirstOrDefault(emptyGen => emptyGen != null)!;
            if (gen != null)
            {
                return true;
            }

            if (type.IsNullableStruct() || context.IsNullableReference(type))
            {
                gen = CreateNullGen(type);
                return true;
            }

            return false;
        }

        private static IGen CreateNullGen(Type type) =>
            (IGen)typeof(CompositeReflectedGenHandler)
                .GetMethod(nameof(CreateDefaultGen), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(type)
                .Invoke(null, Array.Empty<object>())!;

        private static IGen<T> CreateDefaultGen<T>() => Gen.Constant(default(T)!);

        /// <summary>Stops the build of a value beyond the depth limit, up to the nearest member that can end it.</summary>
        private sealed class DepthLimitException : Exception
        {
            public DepthLimitException(Type type) : base($"the type '{type}' is deeper than the depth limit")
            {
                Type = type;
            }

            public Type Type { get; }
        }

        private IGen CreateGenUnlimited(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            var gen = _genHandlersByPriority
                .Where(genFactory => genFactory.CanHandleGen(type, context))
                .Select(genFactory => genFactory.CreateGen(innerHandler, type, context))
                .FirstOrDefault();

            if (gen == null)
            {
                return context.Error(type, $"could not resolve type '{type}'");
            }

            return gen;
        }
    }
}
