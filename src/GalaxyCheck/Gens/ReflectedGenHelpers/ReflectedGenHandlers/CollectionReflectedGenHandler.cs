using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens.Generated;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    [RequiresUnreferencedCode(ReflectionMessages.Path)]
    [RequiresDynamicCode(ReflectionMessages.Path)]
    internal class CollectionReflectedGenHandler : IReflectedGenHandler, IEmptyCollectionGenHandler
    {
        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context) => IsCollection(type);

        private static bool IsCollection(Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                return GenMethodByGenericTypeDefinition.ContainsKey(genericTypeDefinition);
            }

            return false;
        }

        public IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            var elementType = type.GetGenericArguments().Single();
            var elementNullabilityInfo = context.NullabilityInfo?.GenericTypeArguments.SingleOrDefault();
            var elementGen = innerHandler.CreateGen(
                elementType,
                context.Next("[*]", elementType, elementNullabilityInfo, inCollection: true));

            // A collection of a type that refers to itself has few elements, so that a tree stays small.
            int? maxCount = context.CountInHistory(elementType) > 0 ? ReflectedGenHandlerContext.MaxRecursiveCollectionCount : null;
            return Build(type, elementType, elementGen, maxCount);
        }

        public IGen? CreateEmptyGen(Type type)
        {
            if (!IsCollection(type))
            {
                return null;
            }

            var elementType = type.GetGenericArguments().Single();
            var placeholderGen = typeof(CollectionReflectedGenHandler)
                .GetMethod(nameof(CreatePlaceholderGen), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(elementType)
                .Invoke(null!, Array.Empty<object>())!;

            return Build(type, elementType, (IGen)placeholderGen, 0);
        }

        // The element generator of an empty collection, which never runs.
        private static IGen<T> CreatePlaceholderGen<T>() => Gen.Constant(default(T)!);

        private static IGen Build(Type type, Type elementType, IGen elementGen, int? maxCount)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var methodName = GenMethodByGenericTypeDefinition[genericTypeDefinition];

            var methodInfo = typeof(CollectionReflectedGenHandler).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic)!;

            var genericMethodInfo = methodInfo.MakeGenericMethod(elementType);

            return (IGen)genericMethodInfo.Invoke(null!, new object?[] { elementGen, maxCount })!;
        }

        private static readonly IReadOnlyDictionary<Type, string> GenMethodByGenericTypeDefinition = new Dictionary<Type, string>
        {
            { typeof(IReadOnlyCollection<>), nameof(CreateListGen) },
            { typeof(IReadOnlyList<>), nameof(CreateListGen) },
            { typeof(List<>), nameof(CreateListGen) },
            { typeof(ImmutableList<>), nameof(CreateImmutableListGen) },
            { typeof(IList<>), nameof(CreateListGen) },
            { typeof(IEnumerable<>), nameof(CreateListGen) },
            { typeof(IReadOnlySet<>), nameof(CreateHashSetGen) },
            { typeof(HashSet<>), nameof(CreateHashSetGen) },
            { typeof(ISet<>), nameof(CreateHashSetGen) },
            { typeof(ImmutableHashSet<>), nameof(CreateImmutableHashSetGen) },
            { typeof(Queue<>), nameof(CreateQueueGen) },
            { typeof(Stack<>), nameof(CreateStackGen) },
            { typeof(SortedSet<>), nameof(CreateSortedSetGen) },
            { typeof(LinkedList<>), nameof(CreateLinkedListGen) },
            { typeof(ImmutableArray<>), nameof(CreateImmutableArrayGen) },
            { typeof(ReadOnlyCollection<>), nameof(CreateReadOnlyCollectionGen) },
            { typeof(Memory<>), nameof(CreateMemoryGen) },
            { typeof(ReadOnlyMemory<>), nameof(CreateReadOnlyMemoryGen) },
        };

        private static IGen<IReadOnlyCollection<T>> SetOf<T>(IGen<T> elementGen, int? maxCount) =>
            maxCount is int max ? elementGen.SetOf().WithCountLessThanEqual(max) : elementGen.SetOf();

        private static IGen<IReadOnlyList<T>> CreateIReadOnlyListGen<T>(IGen<T> elementGen, int? maxCount) =>
            maxCount is int max ? elementGen.ListOf().WithCountLessThanEqual(max) : elementGen.ListOf();

        private static IGen<List<T>> CreateListGen<T>(IGen<T> elementGen, int? maxCount) => CreateIReadOnlyListGen(elementGen, maxCount).Select(x => x.ToList());

        private static IGen<ImmutableList<T>> CreateImmutableListGen<T>(IGen<T> elementGen, int? maxCount) => CreateIReadOnlyListGen(elementGen, maxCount).Select(x => x.ToImmutableList());

        private static IGen<HashSet<T>> CreateHashSetGen<T>(IGen<T> elementGen, int? maxCount) => SetOf(elementGen, maxCount).Select(s => new HashSet<T>(s));

        private static IGen<ImmutableHashSet<T>> CreateImmutableHashSetGen<T>(IGen<T> elementGen, int? maxCount) => CreateHashSetGen(elementGen, maxCount).Select(x => x.ToImmutableHashSet());

        private static IGen<Queue<T>> CreateQueueGen<T>(IGen<T> elementGen, int? maxCount) =>
            CreateIReadOnlyListGen(elementGen, maxCount).Select(x => new Queue<T>(x));

        // The last element of the list is the top of the stack.
        private static IGen<Stack<T>> CreateStackGen<T>(IGen<T> elementGen, int? maxCount) =>
            CreateIReadOnlyListGen(elementGen, maxCount).Select(x => new Stack<T>(x));

        private static IGen<SortedSet<T>> CreateSortedSetGen<T>(IGen<T> elementGen, int? maxCount) =>
            SetOf(elementGen, maxCount).Select(s => new SortedSet<T>(s));

        private static IGen<LinkedList<T>> CreateLinkedListGen<T>(IGen<T> elementGen, int? maxCount) =>
            CreateIReadOnlyListGen(elementGen, maxCount).Select(x => new LinkedList<T>(x));

        private static IGen<ImmutableArray<T>> CreateImmutableArrayGen<T>(IGen<T> elementGen, int? maxCount) =>
            CreateIReadOnlyListGen(elementGen, maxCount).Select(x => x.ToImmutableArray());

        private static IGen<ReadOnlyCollection<T>> CreateReadOnlyCollectionGen<T>(IGen<T> elementGen, int? maxCount) =>
            CreateIReadOnlyListGen(elementGen, maxCount).Select(x => new ReadOnlyCollection<T>(x.ToList()));

        private static IGen<Memory<T>> CreateMemoryGen<T>(IGen<T> elementGen, int? maxCount) =>
            CreateIReadOnlyListGen(elementGen, maxCount).Select(x => new Memory<T>(x.ToArray()));

        private static IGen<ReadOnlyMemory<T>> CreateReadOnlyMemoryGen<T>(IGen<T> elementGen, int? maxCount) =>
            CreateIReadOnlyListGen(elementGen, maxCount).Select(x => new ReadOnlyMemory<T>(x.ToArray()));
    }
}
