using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    internal class CollectionReflectedGenHandler : IReflectedGenHandler
    {
        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context)
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
            var elementGen = innerHandler.CreateGen(elementType, context.Next("[*]", elementType, elementNullabilityInfo));

            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var methodName = GenMethodByGenericTypeDefinition[genericTypeDefinition];

            var methodInfo = typeof(CollectionReflectedGenHandler).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic)!;

            var genericMethodInfo = methodInfo.MakeGenericMethod(elementType);

            return (IGen)genericMethodInfo.Invoke(null!, new object[] { elementGen })!;
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

        private static IGen<IReadOnlyList<T>> CreateIReadOnlyListGen<T>(IGen<T> elementGen) => elementGen.ListOf();

        private static IGen<List<T>> CreateListGen<T>(IGen<T> elementGen) => CreateIReadOnlyListGen(elementGen).Select(x => x.ToList());

        private static IGen<ImmutableList<T>> CreateImmutableListGen<T>(IGen<T> elementGen) => CreateIReadOnlyListGen(elementGen).Select(x => x.ToImmutableList());

        private static IGen<HashSet<T>> CreateHashSetGen<T>(IGen<T> elementGen) => elementGen.SetOf().Select(s => new HashSet<T>(s));

        private static IGen<ImmutableHashSet<T>> CreateImmutableHashSetGen<T>(IGen<T> elementGen) => CreateHashSetGen(elementGen).Select(x => x.ToImmutableHashSet());

        private static IGen<Queue<T>> CreateQueueGen<T>(IGen<T> elementGen) =>
            CreateIReadOnlyListGen(elementGen).Select(x => new Queue<T>(x));

        // The last element of the list is the top of the stack.
        private static IGen<Stack<T>> CreateStackGen<T>(IGen<T> elementGen) =>
            CreateIReadOnlyListGen(elementGen).Select(x => new Stack<T>(x));

        private static IGen<SortedSet<T>> CreateSortedSetGen<T>(IGen<T> elementGen) =>
            elementGen.SetOf().Select(s => new SortedSet<T>(s));

        private static IGen<LinkedList<T>> CreateLinkedListGen<T>(IGen<T> elementGen) =>
            CreateIReadOnlyListGen(elementGen).Select(x => new LinkedList<T>(x));

        private static IGen<ImmutableArray<T>> CreateImmutableArrayGen<T>(IGen<T> elementGen) =>
            CreateIReadOnlyListGen(elementGen).Select(x => x.ToImmutableArray());

        private static IGen<ReadOnlyCollection<T>> CreateReadOnlyCollectionGen<T>(IGen<T> elementGen) =>
            CreateIReadOnlyListGen(elementGen).Select(x => new ReadOnlyCollection<T>(x.ToList()));

        private static IGen<Memory<T>> CreateMemoryGen<T>(IGen<T> elementGen) =>
            CreateIReadOnlyListGen(elementGen).Select(x => new Memory<T>(x.ToArray()));

        private static IGen<ReadOnlyMemory<T>> CreateReadOnlyMemoryGen<T>(IGen<T> elementGen) =>
            CreateIReadOnlyListGen(elementGen).Select(x => new ReadOnlyMemory<T>(x.ToArray()));
    }
}
