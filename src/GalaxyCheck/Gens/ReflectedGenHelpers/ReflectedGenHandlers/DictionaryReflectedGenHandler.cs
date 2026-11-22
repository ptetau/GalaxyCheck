using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens.Generated;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    /// <summary>
    /// Handles the dictionary types, <see cref="KeyValuePair{TKey, TValue}"/>, and the tuple types of 1 to 7 elements.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionMessages.Path)]
    [RequiresDynamicCode(ReflectionMessages.Path)]
    internal class DictionaryReflectedGenHandler : IReflectedGenHandler, IEmptyCollectionGenHandler
    {
        private static readonly IReadOnlyDictionary<Type, string> GenMethodByGenericTypeDefinition =
            new Dictionary<Type, string>
            {
                { typeof(Dictionary<,>), nameof(CreateDictionaryGen) },
                { typeof(IDictionary<,>), nameof(CreateDictionaryGen) },
                { typeof(IReadOnlyDictionary<,>), nameof(CreateDictionaryGen) },
                { typeof(ImmutableDictionary<,>), nameof(CreateImmutableDictionaryGen) },
                { typeof(KeyValuePair<,>), nameof(CreateKeyValuePairGen) },
            };

        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context)
        {
            if (!type.IsGenericType) return false;

            var definition = type.GetGenericTypeDefinition();
            return GenMethodByGenericTypeDefinition.ContainsKey(definition) || IsTuple(type, out _);
        }

        public IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            var arguments = type.GetGenericArguments();
            var nullability = context.NullabilityInfo?.GenericTypeArguments;

            if (IsTuple(type, out var tupleMethodName))
            {
                var elementGens = arguments
                    .Select((argument, i) => innerHandler.CreateGen(
                        argument,
                        context.Next($"Item{i + 1}", argument, nullability is { Length: > 0 } n && i < n.Length ? n[i] : null)))
                    .ToArray();

                var method = typeof(Gen)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m => m.Name == tupleMethodName && m.GetGenericArguments().Length == arguments.Length)
                    .MakeGenericMethod(arguments);

                return (IGen)method.Invoke(null, elementGens)!;
            }

            // A dictionary can be empty, so it can end a recursive type. A key value pair cannot.
            var inCollection = IsDictionary(type);

            // A key is never null, so its generator ignores the nullability of the type argument.
            var keyGen = innerHandler.CreateGen(arguments[0], context.Next("[key]", arguments[0], null, inCollection));
            var valueGen = innerHandler.CreateGen(
                arguments[1],
                context.Next("[*]", arguments[1], nullability is { Length: 2 } ? nullability[1] : null, inCollection));

            // A dictionary of a type that refers to itself has few entries, so that a tree stays small.
            int? maxCount = inCollection && arguments.Any(argument => context.CountInHistory(argument) > 0)
                ? ReflectedGenHandlerContext.MaxRecursiveCollectionCount
                : null;

            return Build(type, keyGen, valueGen, maxCount);
        }

        public IGen? CreateEmptyGen(Type type)
        {
            if (!IsDictionary(type))
            {
                return null;
            }

            var arguments = type.GetGenericArguments();
            var placeholderGens = arguments
                .Select(argument => typeof(DictionaryReflectedGenHandler)
                    .GetMethod(nameof(CreatePlaceholderGen), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(argument)
                    .Invoke(null, Array.Empty<object>())!)
                .ToArray();

            return Build(type, (IGen)placeholderGens[0], (IGen)placeholderGens[1], 0);
        }

        private static bool IsDictionary(Type type) =>
            type.IsGenericType &&
            type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>) &&
            GenMethodByGenericTypeDefinition.ContainsKey(type.GetGenericTypeDefinition());

        private static IGen Build(Type type, IGen keyGen, IGen valueGen, int? maxCount)
        {
            var methodName = GenMethodByGenericTypeDefinition[type.GetGenericTypeDefinition()];
            var genericMethod = typeof(DictionaryReflectedGenHandler)
                .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(type.GetGenericArguments());

            var parameters = genericMethod.GetParameters().Length == 3
                ? new object?[] { keyGen, valueGen, maxCount }
                : new object?[] { keyGen, valueGen };
            return (IGen)genericMethod.Invoke(null, parameters)!;
        }

        // The key or value generator of an empty dictionary, which never runs.
        private static IGen<T> CreatePlaceholderGen<T>() => Gen.Constant(default(T)!);

        private static IDictionaryGen<TKey, TValue> DictionaryOf<TKey, TValue>(
            IGen<TKey> keyGen,
            IGen<TValue> valueGen,
            int? maxCount)
            where TKey : notnull =>
            maxCount is int max ? Gen.Dictionary(keyGen, valueGen).WithCountLessThanEqual(max) : Gen.Dictionary(keyGen, valueGen);

        private static bool IsTuple(Type type, out string methodName)
        {
            methodName = "";
            if (!type.IsGenericType) return false;

            var definition = type.GetGenericTypeDefinition();
            var count = type.GetGenericArguments().Length;
            if (count is < 1 or > 7) return false;

            if (definition.FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal))
            {
                methodName = nameof(Gen.ValueTuple);
                return true;
            }

            if (definition.FullName.StartsWith("System.Tuple`", StringComparison.Ordinal))
            {
                methodName = nameof(Gen.Tuple);
                return true;
            }

            return false;
        }

        private static IGen<Dictionary<TKey, TValue>> CreateDictionaryGen<TKey, TValue>(
            IGen<TKey> keyGen,
            IGen<TValue> valueGen,
            int? maxCount)
            where TKey : notnull =>
            DictionaryOf(keyGen, valueGen, maxCount).Select(x => new Dictionary<TKey, TValue>(x));

        private static IGen<ImmutableDictionary<TKey, TValue>> CreateImmutableDictionaryGen<TKey, TValue>(
            IGen<TKey> keyGen,
            IGen<TValue> valueGen,
            int? maxCount)
            where TKey : notnull =>
            DictionaryOf(keyGen, valueGen, maxCount).Select(x => x.ToImmutableDictionary());

        private static IGen<KeyValuePair<TKey, TValue>> CreateKeyValuePairGen<TKey, TValue>(
            IGen<TKey> keyGen,
            IGen<TValue> valueGen) => Gen.KeyValuePair(keyGen, valueGen);
    }
}
