using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GalaxyCheck.Gens.ReflectedGenHelpers
{
    internal record ReflectedGenHandlerContext(
        ImmutableList<string> Members,
        ImmutableStack<Type> TypeHistory,
        NullabilityInfo? NullabilityInfo)
    {
        public static ReflectedGenHandlerContext Create(Type type, NullabilityInfo? nullabilityInfo) =>
            new ReflectedGenHandlerContext(ImmutableList.Create("$"), ImmutableStack.Create(type), nullabilityInfo);

        public ReflectedGenHandlerContext Next(string memberName, Type type, NullabilityInfo? nullabilityInfo) =>
            new ReflectedGenHandlerContext(Members.Add(memberName), TypeHistory.Push(type), nullabilityInfo);

        public ReflectedGenHandlerContext SuppressNullability() => this with
        {
            NullabilityInfo = null
        };

        public string MemberPath => string.Join(".", Members);

        /// <summary>The depth limit of a type that refers to itself through a collection.</summary>
        public const int MaxRecursionDepth = 5;

        /// <summary>The most elements of a collection whose element type refers to itself.</summary>
        public const int MaxRecursiveCollectionCount = 3;

        /// <summary>
        /// The number of times that the type is in the history, which is the depth of a value of the type that
        /// the context would make next.
        /// </summary>
        public int CountInHistory(Type type) => TypeHistory.Count(t => t == type);

        /// <summary>
        /// True when the current type is in the history above it and no collection is between the two. A type can
        /// only refer to itself through a collection, which can be empty at the depth limit.
        /// </summary>
        public bool IsCircularWithoutCollection(Type type)
        {
            // The members and the types are in step: the root type has the member "$".
            var types = TypeHistory.Reverse().ToArray();
            var last = types.Length - 1;
            for (var i = last - 1; i >= 0; i--)
            {
                if (types[i] == type)
                {
                    for (var j = i + 1; j <= last; j++)
                    {
                        if (Members[j] == "[*]")
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }
    }

    internal static class ReflectedGenHandlerContextExtensions
    {
        public static int CalculateStableSeed(this ReflectedGenHandlerContext context) => Encoding.Unicode
            .GetBytes(context.MemberPath)
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

        public static IGen Error(this ReflectedGenHandlerContext context, Type type, string message)
        {
            var suffix = context.Members.Count == 1 ? "" : $" at path '{context.MemberPath}'";
            return Gen.Advanced.Error(type, message + suffix);
        }
    }
}
