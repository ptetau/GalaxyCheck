using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens.Generated;
#if NET11_0_OR_GREATER
using System;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    /// <summary>
    /// Handles a C# union. A union type has the union attribute and one public constructor for each case type.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionMessages.Path)]
    [RequiresDynamicCode(ReflectionMessages.Path)]
    internal class UnionReflectedGenHandler : IReflectedGenHandler
    {
        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context) =>
            type.CustomAttributes.Any(a => a.AttributeType == typeof(System.Runtime.CompilerServices.UnionAttribute));

        public IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            var constructors = type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Where(c => c.GetParameters().Length == 1)
                .ToArray();

            if (constructors.Length == 0)
            {
                return context.Error(type, $"the union '{type.FullName ?? type.Name}' has no case");
            }

            var caseGens = constructors
                .Select(constructor =>
                {
                    var caseType = constructor.GetParameters()[0].ParameterType;
                    var caseGen = innerHandler.CreateGen(caseType, context.Next($"<{caseType.Name}>", caseType, null));
                    return (IGenAdvanced)new CaseGen(source => constructor.Invoke(new[] { caseGen.Advanced.GenerateObject(source) }));
                })
                .ToArray();

            return OneOf.Create(type, caseGens);
        }

        private sealed record CaseGen(Func<Parameters.GenSource, object> Func) : Internal.BaseGen<object>
        {
            public override object Generate(Parameters.GenSource source) => Func(source);
        }
    }
}
#endif
