using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens.Generated;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers;

namespace GalaxyCheck.Gens.ReflectedGenHelpers
{
    [global::Verifiable.Contracts.Unverifiable]
    internal record ReflectedGenHandlerContext(
        ImmutableList<string> Members,
        ImmutableStack<Type> TypeHistory,
        NullabilityInfo? NullabilityInfo)
    {
        public static ReflectedGenHandlerContext Create(Type type, NullabilityInfo? nullabilityInfo) =>
            new ReflectedGenHandlerContext(ImmutableList.Create("$"), ImmutableStack.Create(type), nullabilityInfo);

        /// <summary>
        /// For each member of the path, true when a value can end there: the member can be null, or it is an
        /// element of a collection that can be empty. The root has no such step.
        /// </summary>
        public ImmutableList<bool> EndSteps { get; init; } = ImmutableList.Create(false);

        /// <param name="inCollection">True for an element of a collection that can be empty.</param>
        public ReflectedGenHandlerContext Next(
            string memberName,
            Type type,
            NullabilityInfo? nullabilityInfo,
            bool inCollection = false) =>
            new ReflectedGenHandlerContext(Members.Add(memberName), TypeHistory.Push(type), nullabilityInfo)
            {
                EndSteps = EndSteps.Add(inCollection || type.IsNullableStruct() || IsNullable(type, nullabilityInfo))
            };

        public ReflectedGenHandlerContext SuppressNullability() => this with
        {
            NullabilityInfo = null
        };

        public string MemberPath => string.Join(".", Members);

        /// <summary>The depth limit of a type that refers to itself. A value with no child of its type has depth 0.</summary>
        public const int MaxRecursionDepth = 5;

        /// <summary>The most elements of a collection whose element type refers to itself.</summary>
        public const int MaxRecursiveCollectionCount = 3;

        /// <summary>
        /// The number of times that the type is in the history, which is the depth of a value of the type that
        /// the context would make next.
        /// </summary>
        public int CountInHistory(Type type) => TypeHistory.Count(t => t == type);

        /// <summary>
        /// True when the current member is of a reference type that can be null.
        /// </summary>
        public bool IsNullableReference(Type type) => IsNullable(type, NullabilityInfo);

        /// <summary>
        /// True when the current member is declared as a reference that is never null. False when its nullability
        /// is unknown, for example in a nullable-oblivious project.
        /// </summary>
        public bool IsNonNullableReference() => NullabilityInfo is { ReadState: NullabilityState.NotNull };

        /// <summary>
        /// True when the current type is deeper than the depth limit: it is in the history more often than the root
        /// and <see cref="MaxRecursionDepth"/> levels below it.
        /// </summary>
        public bool IsBeyondDepthLimit(Type type) => CountInHistory(type) > MaxRecursionDepth + 1;

        /// <summary>
        /// True when a member between the current type and its last occurrence above it can end a value. Then the
        /// nearest such member stops the recursion with null or an empty collection.
        /// </summary>
        public bool CanEndCycle(Type type)
        {
            // The members, the end steps, and the types are in step: the root type has the member "$".
            var types = TypeHistory.Reverse().ToArray();
            var last = types.Length - 1;
            for (var i = last - 1; i >= 0; i--)
            {
                if (types[i] == type)
                {
                    for (var j = i + 1; j <= last; j++)
                    {
                        if (EndSteps[j])
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return false;
        }

        private static bool IsNullable(Type type, NullabilityInfo? nullabilityInfo) =>
            !type.IsValueType &&
            nullabilityInfo is { ReadState: NullabilityState.Nullable, WriteState: NullabilityState.Nullable };
    }

    [global::Verifiable.Contracts.Unverifiable]
    internal static class ReflectedGenHandlerContextExtensions
    {
        public static int CalculateStableSeed(this ReflectedGenHandlerContext context) => StableSeed(context.MemberPath);

        /// <summary>The seed of the random state of the member at the path. The generated code uses it too.</summary>
        public static int StableSeed(string memberPath) => Encoding.Unicode
            .GetBytes(memberPath)
            .Aggregate(0, (acc, curr) =>
            {
                unchecked
                {
                    return acc + curr;
                }
            });

        public static string ErrorMessage(this ReflectedGenHandlerContext context, string message)
        {
            var suffix = context.Members.Count == 1 ? "" : $" at path '{context.MemberPath}'";
            return message + suffix;
        }

        public static IGen<T> Error<T>(this ReflectedGenHandlerContext context, string message)
        {
            var suffix = context.Members.Count == 1 ? "" : $" at path '{context.MemberPath}'";
            return Gen.Advanced.Error<T>(message + suffix);
        }

        /// <summary>
        /// The error of a type that the factory cannot build, with a fix that registers a concrete type and creates
        /// the type that the factory was called with.
        /// </summary>
        [RequiresDynamicCode(ReflectionMessages.Path)]
        public static IGen UnresolvedTypeError(this ReflectedGenHandlerContext context, Type type, string name)
        {
            var typeName = global::GalaxyCheck.Internal.FixHint.TypeName(type);
            var rootName = global::GalaxyCheck.Internal.FixHint.TypeName(context.TypeHistory.Reverse().First());
            var registration = type.IsAbstract || type.IsInterface
                ? $"Gen.Factory().RegisterImplementations<{typeName}>(typeof(MyConcreteType)).Create<{rootName}>()"
                : $"Gen.Factory().RegisterType(myGenOf{SafeIdentifier(typeName)}).Create<{rootName}>()";
            return Gen.Advanced.Error(
                type,
                $"could not resolve type '{name}'{(context.Members.Count == 1 ? "" : $" at path '{context.MemberPath}'")}" +
                Environment.NewLine +
                $"Fix: register a generator or a concrete type for '{typeName}' on the factory, for example {registration}.");
        }

        private static string SafeIdentifier(string typeName) =>
            new string(typeName.Where(char.IsLetterOrDigit).ToArray());

        [RequiresDynamicCode(ReflectionMessages.Path)]
        public static IGen Error(this ReflectedGenHandlerContext context, Type type, string message)
        {
            var suffix = context.Members.Count == 1 ? "" : $" at path '{context.MemberPath}'";
            return Gen.Advanced.Error(type, message + suffix);
        }
    }
}
