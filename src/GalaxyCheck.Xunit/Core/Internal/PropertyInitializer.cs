using GalaxyCheck.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Internal
{
    internal record PropertyInitializationResult(
        IPropertyRunner Runner,
        PropertyRunParameters Parameters,
        string? SkipReason)
    {
        public bool ShouldSkip => SkipReason != null;
    }

    internal static class PropertyInitializer
    {
        private static readonly MethodInfo ConstantMethod = typeof(Gen)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(Gen.Constant) && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);

        public static PropertyInitializationResult Initialize(
            Type testClassType,
            MethodInfo testMethodInfo,
            object[] constructorArguments,
            object?[]? testMethodArguments,
            IPropertyFactory propertyFactory,
            IGlobalPropertyConfiguration config,
            IGlobalFailureMemoryConfiguration? failureMemory = null,
            string? rowIndex = null) =>
                InitializeWithInstance(
                    testClassType,
                    testMethodInfo,
                    Activator.CreateInstance(testClassType, constructorArguments)!,
                    testMethodArguments,
                    propertyFactory,
                    config,
                    failureMemory,
                    rowIndex: rowIndex);

        /// <summary>Builds the property of a test method on an instance of its class that the framework made.</summary>
        /// <param name="testClassInstance">The instance of the test class, or null for a static test method.</param>
        /// <param name="testMethodArguments">The values of a data row for the first parameters, or null.</param>
        /// <param name="failureMemory">The failure memory of the test assembly, or null for no failure memory.</param>
        /// <param name="testClassArguments">The arguments of a parameterized test class, or null.</param>
        /// <param name="rowIndex">The stable index of the data row among the rows of the test, or null.</param>
        /// <param name="fixtureIndex">The stable index of the fixture among the fixtures of the class, or null.</param>
        /// <param name="rerunStyle">How the framework runs one test from the command line.</param>
        public static PropertyInitializationResult InitializeWithInstance(
            Type testClassType,
            MethodInfo testMethodInfo,
            object? testClassInstance,
            object?[]? testMethodArguments,
            IPropertyFactory propertyFactory,
            IGlobalPropertyConfiguration config,
            IGlobalFailureMemoryConfiguration? failureMemory = null,
            object?[]? testClassArguments = null,
            string? rowIndex = null,
            string? fixtureIndex = null,
            RerunStyle rerunStyle = RerunStyle.DotnetTestFilter)
        {
            var propertyAttribute = testMethodInfo.GetCustomAttributes<PropertyAttribute>(inherit: true).Single();

            var genFactory = ReflectionHelpers.TryResolveGenFactory(testClassType, testMethodInfo);
            var customGens = WithDataRow(
                ReflectionHelpers.ResolveCustomGens(testClassType, testClassInstance, testMethodInfo),
                testMethodInfo,
                testMethodArguments);
            var property = propertyFactory.CreateProperty(testMethodInfo, testClassInstance, genFactory, customGens, testMethodArguments);
            var replay = testMethodInfo.GetCustomAttributes<ReplayAttribute>().SingleOrDefault()?.Replay;
            var examples = PropertyExamples.Create(
                testMethodInfo,
                testClassInstance,
                testMethodArguments,
                propertyFactory,
                genFactory);

            // A [Replay] attribute takes precedence over a saved failure.
            var savedFailure = replay == null && failureMemory != null
                ? SavedFailure.For(testClassType, testMethodInfo, testMethodArguments, failureMemory, testClassArguments, rowIndex, fixtureIndex)
                : null;
            Func<string?>? takeFailureMemoryNote = replay == null && savedFailure == null && failureMemory?.Enabled == true
                ? () => FailureFile.TakeMissingFolderNote(testClassType.Assembly)
                : null;

            var propertyRunParameters = new PropertyRunParameters(
                Property: property,
                Iterations: propertyAttribute.NullableIterations ?? config.DefaultIterations,
                ShrinkLimit: propertyAttribute.NullableShrinkLimit ?? config.DefaultShrinkLimit,
                Replay: replay,
                Seed: replay == null ? propertyAttribute.NullableSeed ?? config.Seed : null,
                Size: replay == null ? propertyAttribute.NullableSize : null,
                Timeout: propertyAttribute.NullableTimeoutMilliseconds is > 0 and var timeoutMs
                    ? TimeSpan.FromMilliseconds(timeoutMs)
                    : null,
                Exhaustive: propertyAttribute.NullableExhaustive ?? true,
                SuppressHealthChecks: SuppressedHealthChecks(testClassType, testMethodInfo),
                Examples: examples,
                SavedFailure: savedFailure,
                TakeFailureMemoryNote: takeFailureMemoryNote,
                Identity: FailureMessage.Identify(
                    rerunStyle, testClassType, testMethodInfo, testMethodArguments, testClassArguments, rowIndex, fixtureIndex));

            return new PropertyInitializationResult(
                propertyAttribute.Runner,
                propertyRunParameters,
                propertyAttribute.SkipReason);
        }

        /// <summary>
        /// Fixes the parameters that a data row gives, so that the property generates only the rest. A method that
        /// returns a property gets the row as its arguments instead.
        /// </summary>
        private static IReadOnlyDictionary<int, IGen> WithDataRow(
            IReadOnlyDictionary<int, IGen> customGens,
            MethodInfo testMethodInfo,
            object?[]? testMethodArguments)
        {
            if (testMethodArguments == null || testMethodArguments.Length == 0)
            {
                return customGens;
            }

            var parameters = testMethodInfo.GetParameters();
            var result = new Dictionary<int, IGen>(customGens);
            for (var i = 0; i < testMethodArguments.Length && i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var input = testMethodArguments[i];
                if (!PropertyExamples.TryConvert(input, parameter.ParameterType, out var value))
                {
                    throw new PropertyConfigurationException(
                        $"The data row gives the value {PropertyExamples.Format(input)} ({input?.GetType().Name ?? "null"}) for parameter '{parameter.Name}', which has the type {parameter.ParameterType.Name}." +
                        Environment.NewLine +
                        $"Fix: give a value of type {parameter.ParameterType.Name} for parameter '{parameter.Name}' in the row.");
                }

                result[i] = (IGen)ConstantMethod
                    .MakeGenericMethod(parameter.ParameterType)
                    .Invoke(null, new[] { value })!;
            }

            return result;
        }

        /// <summary>The health checks that the method or its class suppresses, or null when none does.</summary>
        private static HealthCheck[]? SuppressedHealthChecks(Type testClassType, MethodInfo testMethodInfo)
        {
            var checks = testMethodInfo.GetCustomAttributes<SuppressHealthCheckAttribute>(inherit: true)
                .Concat(testClassType.GetCustomAttributes<SuppressHealthCheckAttribute>(inherit: true))
                .SelectMany(attribute => attribute.Checks)
                .Distinct()
                .ToArray();

            return checks.Length == 0 ? null : checks;
        }
    }
}
