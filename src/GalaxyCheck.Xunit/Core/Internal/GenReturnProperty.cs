using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GalaxyCheck.Internal
{
    /// <summary>
    /// Builds the property of a method that returns a generator of tests, for example <c>IGen&lt;Property.Test&gt;</c>,
    /// <c>Property&lt;int&gt;</c>, or <c>IGen&lt;Property.AsyncTest&gt;</c>. <see cref="Property.ReflectAsync"/> builds
    /// each other form.
    /// </summary>
    internal static class GenReturnProperty
    {
        private static readonly MethodInfo FromTestsMethod =
            typeof(GenReturnProperty).GetMethod(nameof(FromTests), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo FromAsyncTestsMethod =
            typeof(GenReturnProperty).GetMethod(nameof(FromAsyncTests), BindingFlags.NonPublic | BindingFlags.Static)!;

        /// <returns>The property, or null when the method does not return a generator of tests.</returns>
        public static AsyncProperty? TryCreate(MethodInfo methodInfo, object? target, object?[]? controlData)
        {
            var returnType = methodInfo.ReturnType;
            if (returnType == typeof(Property) || returnType == typeof(AsyncProperty))
            {
                return null;
            }

            var testType = TryGetGenElementType(returnType);
            if (testType == null)
            {
                return null;
            }

            var (factory, inputType) =
                TryGetTestInputType(testType, typeof(Property.Test<>)) is { } syncInput ? (FromTestsMethod, syncInput) :
                TryGetTestInputType(testType, typeof(Property.AsyncTest<>)) is { } asyncInput ? (FromAsyncTestsMethod, asyncInput) :
                (null, null);
            if (factory == null)
            {
                return null;
            }

            if (methodInfo.GetParameters().Any() && controlData is null)
            {
                throw new Exception(
                    $"Parameters are not support for methods returning properties, unless control data is injected. Violating signature: \"{methodInfo}\"");
            }

            object gen;
            try
            {
                gen = methodInfo.Invoke(target, controlData ?? Array.Empty<object>())!;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }

            return (AsyncProperty)factory.MakeGenericMethod(inputType!).Invoke(null, new[] { gen })!;
        }

        private static Type? TryGetGenElementType(Type type)
        {
            var genInterfaces = (type.IsInterface ? new[] { type } : Array.Empty<Type>())
                .Concat(type.GetInterfaces())
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IGen<>))
                .ToList();

            return genInterfaces.Count == 1 ? genInterfaces[0].GetGenericArguments()[0] : null;
        }

        private static Type? TryGetTestInputType(Type testType, Type genericTestDefinition)
        {
            var testInterfaces = (testType.IsInterface ? new[] { testType } : Array.Empty<Type>())
                .Concat(testType.GetInterfaces())
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericTestDefinition)
                .ToList();

            return testInterfaces.Count == 1 ? testInterfaces[0].GetGenericArguments()[0] : null;
        }

        private static AsyncProperty FromTests<T>(IGen<Property.Test<T>> gen) =>
            new AsyncProperty<T>(gen.Select(test => (Property.AsyncTest<T>)new AsyncTestOfTest<T>(test)));

        private static AsyncProperty FromAsyncTests<T>(IGen<Property.AsyncTest<T>> gen) =>
            new AsyncProperty<T>(gen);

        private sealed class AsyncTestOfTest<T> : Property.AsyncTest<T>
        {
            private readonly Property.Test<T> _test;

            public AsyncTestOfTest(Property.Test<T> test)
            {
                _test = test;
                Output = new Lazy<ValueTask<Property.TestOutput>>(() => new ValueTask<Property.TestOutput>(_test.Output.Value));
            }

            public T Input => _test.Input;

            public IReadOnlyList<object?> PresentedInput => _test.PresentedInput;

            public Lazy<ValueTask<Property.TestOutput>> Output { get; }
        }
    }
}
