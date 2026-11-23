using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens.Generated;
using GalaxyCheck.Gens.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Gens.ReflectedGenHelpers.ReflectedGenHandlers
{
    [RequiresUnreferencedCode(ReflectionMessages.Path)]
    [RequiresDynamicCode(ReflectionMessages.Path)]
    internal class NonDefaultConstructorReflectedGenHandler : IReflectedGenHandler
    {
        public bool CanHandleGen(Type type, ReflectedGenHandlerContext context) =>
            TryFindConstructor(type) != null;

        public IGen CreateGen(IReflectedGenHandler innerHandler, Type type, ReflectedGenHandlerContext context)
        {
            var methodInfo = typeof(NonDefaultConstructorReflectedGenHandler).GetMethod(
                nameof(CreateGenGeneric),
                BindingFlags.Static | BindingFlags.NonPublic)!;

            var genericMethodInfo = methodInfo.MakeGenericMethod(type);

            return (IGen)genericMethodInfo.Invoke(null!, new object[] { innerHandler, context })!;
        }

        private static IGen<T> CreateGenGeneric<T>(IReflectedGenHandler innerHandler, ReflectedGenHandlerContext constructorContext)
        {
            var nullabilityInfoContext = new NullabilityInfoContext();
            var constructor = TryFindConstructor(typeof(T))!;

            var parameterGens = constructor
                .GetParameters()
                .Select(parameter =>
                {
                    // TODO: Indicate it's a ctor param in the path
                    var parameterContext = constructorContext.Next(parameter.Name ?? "<unknown>", parameter.ParameterType,
                        nullabilityInfoContext.Create(parameter));

                    return innerHandler
                        .CreateGen(parameter.ParameterType, parameterContext)
                        .Cast<object>()
                        .Advanced.ReferenceRngWaypoint(parameterContext.CalculateStableSeed());
                });

            return Gen
                .Zip(parameterGens)
                .Select(parameters =>
                {
                    try
                    {
                        return (T)constructor.Invoke(parameters as object[] ?? parameters.ToArray());
                    }
                    catch (TargetInvocationException ex)
                    {
                        var innerEx = ex.InnerException;
                        var message = $"'{innerEx!.GetType()}' was thrown while calling constructor with message '{innerEx.Message}'";
                        throw new GenErrorSignal(constructorContext.ErrorMessage(message));
                    }
                });
        }

        private static ConstructorInfo? TryFindConstructor(Type type) => (
            from constructor in type.GetConstructors()
            where constructor.IsPublic
            let parameters = constructor.GetParameters()
            where parameters.Length > 0
            orderby parameters.Length descending
            select constructor).FirstOrDefault();
    }
}
