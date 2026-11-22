using GalaxyCheck.Configuration;
using System;
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
        public static PropertyInitializationResult Initialize(
            Type testClassType,
            MethodInfo testMethodInfo,
            object[] constructorArguments,
            object[]? testMethodArguments,
            IPropertyFactory propertyFactory,
            IGlobalPropertyConfiguration config)
        {
            var testClassInstance = Activator.CreateInstance(testClassType, constructorArguments)!;
            var propertyAttribute = testMethodInfo.GetCustomAttributes<PropertyAttribute>(inherit: true).Single();

            var genFactory = ReflectionHelpers.TryResolveGenFactory(testClassType, testMethodInfo);
            var customGens = ReflectionHelpers.ResolveCustomGens(testClassInstance, testMethodInfo);
            var property = propertyFactory.CreateProperty(testMethodInfo, testClassInstance, genFactory, customGens, testMethodArguments);
            var replay = testMethodInfo.GetCustomAttributes<ReplayAttribute>().SingleOrDefault()?.Replay;

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
                SuppressHealthChecks: SuppressedHealthChecks(testClassType, testMethodInfo));

            return new PropertyInitializationResult(
                propertyAttribute.Runner,
                propertyRunParameters,
                propertyAttribute.Skip?.Length > 0 ? propertyAttribute.Skip : null);
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
