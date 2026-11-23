using GalaxyCheck.Gens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Internal
{
    internal static class ReflectionHelpers
    {
        public static IGenFactory? TryResolveGenFactory(Type testClassType, MethodInfo testMethodInfo)
        {
            var genFactoryAttribute =
                testMethodInfo.GetCustomAttributes<GenFactoryAttribute>(inherit: true).FirstOrDefault() ??
                testClassType.GetCustomAttributes<GenFactoryAttribute>(inherit: true).FirstOrDefault();

            return genFactoryAttribute?.Value;
        }

        public static IReadOnlyDictionary<int, IGen> ResolveCustomGens(object testClassInstance, MethodInfo methodInfo) =>
            ResolveCustomGens(testClassInstance.GetType(), testClassInstance, methodInfo);

        /// <param name="testClassInstance">The instance that holds the members, or null to read only static members.</param>
        public static IReadOnlyDictionary<int, IGen> ResolveCustomGens(Type testClassType, object? testClassInstance, MethodInfo methodInfo)
        {
            return methodInfo
                .GetParameters()
                .Select((parameter, i) =>
                {
                    var memberGenAttribute = parameter.GetCustomAttribute<MemberGenAttribute>();
                    return memberGenAttribute == null
                        ? null :
                        new { gen = ResolveMemberGen(testClassType, testClassInstance, memberGenAttribute.MemberName, parameter), parameterIndex = i };
                })
                .Where(x => x != null)
                .ToDictionary(x => x!.parameterIndex, x => x!.gen);
        }

        private static IGen ResolveMemberGen(Type targetType, object? target, string memberName, ParameterInfo parameter)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                (target == null ? BindingFlags.Default : BindingFlags.Instance);

            var className = targetType.Name;
            var genType = $"IGen<{CSharpName(parameter.ParameterType)}>";
            var declaration = $"public static {genType} {memberName} => {ExampleGen(parameter.ParameterType)};";

            var property = targetType.GetProperty(memberName, bindingFlags);
            if (property != null)
            {
                return CheckGen(property.GetValue(target));
            }

            var field = targetType.GetField(memberName, bindingFlags);
            if (field != null)
            {
                return CheckGen(field.GetValue(target));
            }

            var existing = targetType
                .GetProperties(bindingFlags).Select(p => (p.Name, p.PropertyType))
                .Concat(targetType.GetFields(bindingFlags).Select(f => (f.Name, f.FieldType)))
                .Where(member => GenValueType(member.Item2) is { } valueType && parameter.ParameterType.IsAssignableFrom(valueType))
                .Select(member => member.Name)
                .ToList();
            var suggestion = existing.Count == 0
                ? $"add the property '{declaration}' to '{className}', or name an existing member with [MemberGen(nameof(...))]."
                : $"name a member that exists, for example [MemberGen(nameof({existing[0]}))], or add a static property '{memberName}' of type {genType} to '{className}'.";

            throw new PropertyConfigurationException(
                $"[MemberGen(\"{memberName}\")] on parameter '{parameter.Name}' names no property or field of '{className}'." +
                Environment.NewLine +
                "Fix: " + suggestion);

            IGen CheckGen(object? value)
            {
                if (value is not IGen gen)
                {
                    throw new PropertyConfigurationException(
                        $"Expected member '{memberName}' to be an instance of '{typeof(IGen).FullName}', but it had a value of type '{value?.GetType().FullName ?? "null"}'" +
                        Environment.NewLine +
                        $"Fix: make '{memberName}' a generator for parameter '{parameter.Name}', for example '{declaration}'");
                }

                var valueType = GenValueType(gen);
                if (valueType != null && !parameter.ParameterType.IsAssignableFrom(valueType))
                {
                    throw new PropertyConfigurationException(
                        $"[MemberGen(\"{memberName}\")] on parameter '{parameter.Name}' gives a generator of type 'IGen<{CSharpName(valueType)}>', which is not compatible with parameter of type '{CSharpName(parameter.ParameterType)}'." +
                        Environment.NewLine +
                        $"Fix: make '{memberName}' generate the type of the parameter, for example '{declaration}', or change the type of parameter '{parameter.Name}' to {CSharpName(valueType)}.");
                }

                return gen;
            }
        }

        /// <summary>The value type of a generator, from its <c>IGen&lt;T&gt;</c> interface, or null.</summary>
        private static Type? GenValueType(IGen gen) => GenValueType(gen.GetType());

        /// <summary>The value type of a generator type, from its <c>IGen&lt;T&gt;</c> interface, or null.</summary>
        private static Type? GenValueType(Type genType) =>
            genType
                .GetInterfaces()
                .Concat(new[] { genType })
                .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGen<>))
                .Select(type => type.GetGenericArguments()[0])
                .FirstOrDefault();

        /// <summary>A generator call for an example declaration of a type.</summary>
        private static string ExampleGen(Type type) => type switch
        {
            _ when type == typeof(int) => "Gen.Int32()",
            _ when type == typeof(long) => "Gen.Int64()",
            _ when type == typeof(short) => "Gen.Int16()",
            _ when type == typeof(byte) => "Gen.Byte()",
            _ when type == typeof(bool) => "Gen.Boolean()",
            _ when type == typeof(string) => "Gen.String()",
            _ when type == typeof(double) => "Gen.Double()",
            _ when type == typeof(decimal) => "Gen.Decimal()",
            _ when type == typeof(Guid) => "Gen.Guid()",
            _ => $"Gen.Create<{CSharpName(type)}>()",
        };

        private static readonly Dictionary<Type, string> Keywords = new()
        {
            [typeof(bool)] = "bool", [typeof(byte)] = "byte", [typeof(sbyte)] = "sbyte", [typeof(char)] = "char",
            [typeof(short)] = "short", [typeof(ushort)] = "ushort", [typeof(int)] = "int", [typeof(uint)] = "uint",
            [typeof(long)] = "long", [typeof(ulong)] = "ulong", [typeof(float)] = "float", [typeof(double)] = "double",
            [typeof(decimal)] = "decimal", [typeof(string)] = "string", [typeof(object)] = "object",
        };

        /// <summary>The C# name of a type, for example <c>int</c>, <c>List&lt;int&gt;</c>, or <c>int[]</c>.</summary>
        internal static string CSharpName(Type type)
        {
            if (type.IsArray)
            {
                return $"{CSharpName(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
            }

            if (Nullable.GetUnderlyingType(type) is { } underlying)
            {
                return CSharpName(underlying) + "?";
            }

            if (Keywords.TryGetValue(type, out var keyword))
            {
                return keyword;
            }

            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
            {
                name = name.Substring(0, tick);
            }

            var outerArguments = 0;
            if (type.IsNested && type.DeclaringType != null && !type.IsGenericParameter)
            {
                name = $"{CSharpName(type.DeclaringType)}.{name}";
                outerArguments = type.DeclaringType.GetGenericArguments().Length;
            }

            var ownArguments = type.GetGenericArguments().Skip(outerArguments).ToArray();
            return tick < 0 || ownArguments.Length == 0
                ? name
                : $"{name}<{string.Join(", ", ownArguments.Select(CSharpName))}>";
        }
    }
}
