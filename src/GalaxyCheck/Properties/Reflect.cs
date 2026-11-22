using System.Diagnostics.CodeAnalysis;
using GalaxyCheck.Gens;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GalaxyCheck
{
    public partial class Property
    {
        [RequiresUnreferencedCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        [RequiresDynamicCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        public static Property Reflect(
            MethodInfo methodInfo,
            object? target,
            IGenFactory? genFactory = null,
            IReadOnlyDictionary<int, IGen>? customGens = null,
            object?[]? controlData = null)
        {
            if (ReflectedHandlers.MethodPropertyHandlers.TryGetValue(methodInfo.ReturnType, out var handler) == false)
            {
                var supportedReturnTypesFormatted = FormatSupportReturnTypes(ReflectedHandlers.MethodPropertyHandlers.Keys);

                var message = $"Return type is not supported by {nameof(Property)}.{nameof(Reflect)}.";
                if (ReflectedHandlers.AsyncMethodPropertyHandlers.ContainsKey(methodInfo.ReturnType))
                {
                    message +=
                        $" Did you mean to use {nameof(Property)}.{nameof(ReflectAsync)}? Otherwise, please use one of: {supportedReturnTypesFormatted}.";
                }
                else
                {
                    message += $" Please use one of: {supportedReturnTypesFormatted}.";
                }

                message += $" Return type was: {methodInfo.ReturnType}.";

                throw new Exception(message);
            }

            return handler(methodInfo, target, genFactory, customGens, controlData);
        }

        [RequiresUnreferencedCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        [RequiresDynamicCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        public static AsyncProperty ReflectAsync(
            MethodInfo methodInfo,
            object? target,
            IGenFactory? genFactory = null,
            IReadOnlyDictionary<int, IGen>? customGens = null,
            object?[]? controlData = null)
        {
            if (ReflectedHandlers.MethodPropertyHandlers.TryGetValue(methodInfo.ReturnType, out var handler) == false)
            {
                if (ReflectedHandlers.AsyncMethodPropertyHandlers.TryGetValue(methodInfo.ReturnType, out var asyncHandler) == false)
                {
                    var supportedReturnTypesFormatted =
                        FormatSupportReturnTypes(Enumerable.Concat(ReflectedHandlers.MethodPropertyHandlers.Keys, ReflectedHandlers.AsyncMethodPropertyHandlers.Keys));
                    var message =
                        $"Return type is not supported by {nameof(Property)}.{nameof(ReflectAsync)}. Please use one of: {supportedReturnTypesFormatted}. Return type was: {methodInfo.ReturnType}.";
                    throw new Exception(message);
                }

                return asyncHandler(methodInfo, target, genFactory, customGens, controlData);
            }
            else
            {
                var asyncHandler = ToAsyncPropertyHandler(handler);
                return asyncHandler(methodInfo, target, genFactory, customGens, controlData);
            }
        }

        private static string FormatSupportReturnTypes(IEnumerable<Type> types)
        {
            return string.Join(", ", types.OrderBy(x => x.FullName));
        }

        private delegate Property ReflectedPropertyHandler(
            MethodInfo methodInfo,
            object? target,
            IGenFactory? genFactory,
            IReadOnlyDictionary<int, IGen>? customGens,
            object?[]? controlData);

        private delegate AsyncProperty AsyncReflectedPropertyHandler(
            MethodInfo methodInfo,
            object? target,
            IGenFactory? genFactory,
            IReadOnlyDictionary<int, IGen>? customGens,
            object?[]? controlData);

        private static AsyncReflectedPropertyHandler ToAsyncPropertyHandler(ReflectedPropertyHandler handler)
        {
            return (methodInfo, target, genFactory, customGens, controlData) =>
            {
                var property = handler(methodInfo, target, genFactory, customGens, controlData);
                return new AsyncProperty(property.Select(test => test.AsAsync()));
            };
        }

        /// <summary>The handlers of each return type. They read the parameters of a method by reflection.</summary>
        [RequiresUnreferencedCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        [RequiresDynamicCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        private static class ReflectedHandlers
        {
            internal readonly static ImmutableDictionary<Type, ReflectedPropertyHandler> MethodPropertyHandlers =
                new Dictionary<Type, ReflectedPropertyHandler>
                {
                    { typeof(void), ToVoidProperty },
                    { typeof(bool), ToBooleanProperty },
                    { typeof(Property), ToReturnedProperty },
                }.ToImmutableDictionary();

            internal readonly static ImmutableDictionary<Type, AsyncReflectedPropertyHandler> AsyncMethodPropertyHandlers =
                new Dictionary<Type, AsyncReflectedPropertyHandler>
                {
                    { typeof(Task), ToTaskProperty },
                    { typeof(Task<bool>), ToTaskBooleanProperty },
                    { typeof(AsyncProperty), ToReturnedAsyncProperty },
                }.ToImmutableDictionary();

            private static ReflectedPropertyHandler ToVoidProperty => (methodInfo, target, genFactory, customGens, controlData) =>
            {
                return new Property(ForAllParameters(methodInfo, Gen.Parameters(methodInfo, genFactory, customGens), parameters =>
                {
                    SafeInvoke(() =>
                    {
                        methodInfo.Invoke(target, parameters);
                        return 0;
                    });
                    return true;
                }));
            };

            private static ReflectedPropertyHandler ToBooleanProperty => (methodInfo, target, genFactory, customGens, controlData) =>
            {
                return new Property(ForAllParameters(methodInfo, Gen.Parameters(methodInfo, genFactory, customGens), parameters =>
                {
                    try
                    {
                        return (bool)methodInfo.Invoke(target, parameters)!;
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.InnerException!;
                    }
                }));
            };

            private static ReflectedPropertyHandler ToReturnedProperty => (methodInfo, target, genFactory, customGens, controlData) =>
            {
                if (methodInfo.GetParameters().Any() && controlData is null)
                {
                    throw new Exception(
                        $"Parameters are not support for methods returning properties, unless control data is injected. Violating signature: \"{methodInfo}\"");
                }

                try
                {
                    return (Property)methodInfo.Invoke(target, controlData ?? Array.Empty<object>())!;
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException!;
                }
            };

            private static AsyncReflectedPropertyHandler ToTaskProperty => (methodInfo, target, genFactory, customGens, controlData) =>
            {
                Func<object[], Task> func = parameters =>
                {
                    try
                    {
                        return (Task)methodInfo.Invoke(target, WithToken(methodInfo, parameters))!;
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.InnerException!;
                    }
                };

                return new AsyncProperty(ForAllParametersAsync(methodInfo, Gen.Parameters(methodInfo, genFactory, customGens), Properties.ActionExtensions.AsTrueFunc(func)));
            };

            private static AsyncReflectedPropertyHandler ToTaskBooleanProperty => (methodInfo, target, genFactory, customGens, controlData) =>
            {
                return new AsyncProperty(ForAllParametersAsync(methodInfo, Gen.Parameters(methodInfo, genFactory, customGens), parameters =>
                {
                    try
                    {
                        return (Task<bool>)methodInfo.Invoke(target, WithToken(methodInfo, parameters))!;
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.InnerException!;
                    }
                }));
            };

            private static AsyncReflectedPropertyHandler ToReturnedAsyncProperty => (methodInfo, target, genFactory, customGens, controlData) =>
            {
                if (methodInfo.GetParameters().Any() && controlData is null)
                {
                    throw new Exception(
                        $"Parameters are not support for methods returning properties, unless control data is injected. Violating signature: \"{methodInfo}\"");
                }

                try
                {
                    return (AsyncProperty)methodInfo.Invoke(target, controlData ?? Array.Empty<object>())!;
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException!;
                }
            };
        }

        private static Property<object[]> ForAllParameters(MethodInfo methodInfo, IGen<object[]> gen, Func<object[], bool> func) =>
            new Property<object[]>(new Properties.ForAllGen<object[]>(gen, func, Presenter(methodInfo)));

        private static AsyncProperty<object[]> ForAllParametersAsync(MethodInfo methodInfo, IGen<object[]> gen, Func<object[], Task<bool>> func)
        {
            var present = Presenter(methodInfo);
            return new AsyncProperty<object[]>(gen.Select(parameters => Properties.TestFactory.Create<object[]>(
                parameters,
                () => new ValueTask<bool>(func(parameters)),
                present(parameters))));
        }

        /// <summary>
        /// The counterexample lists each parameter as its own value. A token parameter is not generated, so it is
        /// left out.
        /// </summary>
        private static Func<object[], IReadOnlyList<object?>> Presenter(MethodInfo methodInfo)
        {
            var isToken = methodInfo.GetParameters()
                .Select(parameter => parameter.ParameterType == typeof(System.Threading.CancellationToken))
                .ToArray();

            if (!isToken.Any(token => token)) return parameters => parameters;

            return parameters => parameters.Where((_, index) => index >= isToken.Length || !isToken[index]).ToArray();
        }

        /// <summary>Replaces each token parameter with the token of the test case that runs now.</summary>
        private static object[] WithToken(MethodInfo methodInfo, object[] parameters)
        {
            var parameterInfos = methodInfo.GetParameters();
            object[]? copy = null;
            for (var i = 0; i < parameterInfos.Length && i < parameters.Length; i++)
            {
                if (parameterInfos[i].ParameterType == typeof(System.Threading.CancellationToken))
                {
                    copy ??= (object[])parameters.Clone();
                    copy[i] = Properties.TestContext.Token;
                }
            }

            return copy ?? parameters;
        }

        private static T SafeInvoke<T>(Func<T> invoke)
        {
            try
            {
                return invoke();
            }
            catch (TargetInvocationException ex)
            {
                throw Unwrap(ex);
            }
        }

        private static Exception Unwrap(TargetInvocationException ex)
        {
            var nextEx = ex.InnerException;
            if (nextEx is null)
            {
                return ex;
            }

            if (nextEx is TargetInvocationException nextTargetInvocationEx)
            {
                return Unwrap(nextTargetInvocationEx);
            }

            return nextEx;
        }
    }
}
