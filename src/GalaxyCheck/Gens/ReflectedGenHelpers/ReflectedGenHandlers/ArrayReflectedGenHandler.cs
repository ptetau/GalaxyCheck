using System;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    internal class ArrayReflectedGenHandler : IReflectedGenHandler
    {
        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context) => type.IsArray;

        public IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            var elementType = type.GetElementType()!;
            var elementNullabilityInfo = context.NullabilityInfo?.ElementType;
            var depth = context.CountInHistory(elementType);
            if (depth > ReflectedGenHandlerContext.MaxRecursionDepth)
            {
                // The element would be deeper than the limit, so the collection is empty.
                return CreateEmpty(type, elementType);
            }

            var elementGen = innerHandler.CreateGen(elementType, context.Next("[*]", elementType, elementNullabilityInfo));

            // An array of a type that refers to itself has few elements, so that a tree stays small.
            int? maxCount = depth > 0 ? ReflectedGenHandlerContext.MaxRecursiveCollectionCount : null;
            return Build(elementType, elementGen, maxCount);
        }

        private static IGen CreateEmpty(Type type, Type elementType) =>
            (IGen)typeof(ArrayReflectedGenHandler)
                .GetMethod(nameof(CreateEmptyArrayGen), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(elementType)
                .Invoke(null!, Array.Empty<object>())!;

        private static IGen Build(Type elementType, IGen elementGen, int? maxCount)
        {
            var methodInfo = typeof(ArrayReflectedGenHandler).GetMethod(
                nameof(CreateArrayGen),
                BindingFlags.Static | BindingFlags.NonPublic)!;

            var genericMethodInfo = methodInfo.MakeGenericMethod(elementType);

            return (IGen)genericMethodInfo.Invoke(null!, new object?[] { elementGen, maxCount })!;
        }

        private static IGen<T[]> CreateEmptyArrayGen<T>() => Gen.Constant(Array.Empty<T>());

        private static IGen<T[]> CreateArrayGen<T>(IGen<T> elementGen, int? maxCount) =>
            (maxCount is int max ? elementGen.ListOf().WithCountLessThanEqual(max) : elementGen.ListOf()).Select(x => x.ToArray());
    }
}
