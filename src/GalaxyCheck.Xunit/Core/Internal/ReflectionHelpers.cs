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

            var property = targetType.GetProperty(memberName, bindingFlags);
            if (property != null)
            {
                return CastToGen(property.GetValue(target), memberName);
            }

            var field = targetType.GetField(memberName, bindingFlags);
            if (field != null)
            {
                return CastToGen(field.GetValue(target), memberName);
            }

            var className = targetType.Name;
            var genType = $"IGen<{parameter.ParameterType.Name}>";
            var existing = targetType
                .GetProperties(bindingFlags).Select(p => (p.Name, p.PropertyType))
                .Concat(targetType.GetFields(bindingFlags).Select(f => (f.Name, f.FieldType)))
                .Where(member => typeof(IGen).IsAssignableFrom(member.Item2))
                .Select(member => member.Name)
                .ToList();
            var suggestion = existing.Count == 0
                ? $"add the property 'public static {genType} {memberName} => Gen.Create<{parameter.ParameterType.Name}>();' to '{className}', or name an existing member with [MemberGen(nameof(...))]."
                : $"name a member that exists, for example [MemberGen(nameof({existing[0]}))], or add a static property '{memberName}' of type {genType} to '{className}'.";

            throw new PropertyConfigurationException(
                $"[MemberGen(\"{memberName}\")] on parameter '{parameter.Name}' names no property or field of '{className}'." +
                Environment.NewLine +
                "Fix: " + suggestion);

            static IGen CastToGen(object? value, string memberName)
            {
                if (value is IGen gen)
                {
                    return gen;
                }

                // The text of this message is a contract of the tests of GalaxyCheck.Xunit, so it has no fix line.
                throw new Exception($"Expected member '{memberName}' to be an instance of '{typeof(IGen).FullName}', but it had a value of type '{value?.GetType().FullName ?? "null"}'");
            }
        }
    }
}
