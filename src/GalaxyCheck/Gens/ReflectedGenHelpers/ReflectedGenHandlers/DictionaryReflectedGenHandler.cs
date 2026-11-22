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
    internal class DictionaryReflectedGenHandler : IReflectedGenHandler
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

            // A key is never null, so its generator ignores the nullability of the type argument.
            var keyGen = innerHandler.CreateGen(arguments[0], context.Next("[key]", arguments[0], null));
            var valueGen = innerHandler.CreateGen(
                arguments[1],
                context.Next("[*]", arguments[1], nullability is { Length: 2 } ? nullability[1] : null));

            var methodName = GenMethodByGenericTypeDefinition[type.GetGenericTypeDefinition()];
            var genericMethod = typeof(DictionaryReflectedGenHandler)
                .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(arguments);

            return (IGen)genericMethod.Invoke(null, new object[] { keyGen, valueGen })!;
        }

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
            IGen<TValue> valueGen)
            where TKey : notnull =>
            Gen.Dictionary(keyGen, valueGen).Select(x => new Dictionary<TKey, TValue>(x));

        private static IGen<ImmutableDictionary<TKey, TValue>> CreateImmutableDictionaryGen<TKey, TValue>(
            IGen<TKey> keyGen,
            IGen<TValue> valueGen)
            where TKey : notnull =>
            Gen.Dictionary(keyGen, valueGen).Select(x => x.ToImmutableDictionary());

        private static IGen<KeyValuePair<TKey, TValue>> CreateKeyValuePairGen<TKey, TValue>(
            IGen<TKey> keyGen,
            IGen<TValue> valueGen) => Gen.KeyValuePair(keyGen, valueGen);
    }
}
