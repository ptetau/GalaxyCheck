using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.v3;

namespace GalaxyCheck.Internal.Infrastructure.XunitV3
{
    /// <summary>The body that runs in place of the reflection call of a test method.</summary>
    internal interface ITestBody
    {
        /// <param name="rowIndex">The stable index of the data row among the rows of the method, or null.</param>
        Task Run(IXunitTest test, object? testClassInstance, string? rowIndex);
    }

    /// <summary>Runs the property of a [Property] or [Sample] method. A data row gives the first parameters.</summary>
    internal sealed class PropertyBody : ITestBody
    {
        public static PropertyBody Instance { get; } = new();

        public async Task Run(IXunitTest test, object? testClassInstance, string? rowIndex)
        {
            var testClassType = test.TestMethod.TestClass.Class;
            var arguments = test.TestMethodArguments;

            var initialization = PropertyInitializer.InitializeWithInstance(
                testClassType,
                test.TestMethod.Method,
                testClassInstance,
                arguments.Length == 0 ? null : arguments,
                new DefaultPropertyFactory(),
                GlobalConfiguration.GetInstance(testClassType.Assembly).Properties,
                GlobalConfiguration.GetInstance(testClassType.Assembly).FailureMemory,
                rowIndex: rowIndex);

            await initialization.Runner.Run(initialization.Parameters);
        }
    }

    /// <summary>Compares the value of each seed of a [GenSnapshot] method with the stored snapshot.</summary>
    internal sealed class GenSnapshotBody : ITestBody
    {
        public static GenSnapshotBody Instance { get; } = new();

        public async Task Run(IXunitTest test, object? testClassInstance, string? rowIndex)
        {
            var testClassType = test.TestMethod.TestClass.Class;
            var testMethod = test.TestMethod.Method;

            var initialization = GenSnapshotInitializer.InitializeWithInstance(
                testClassType,
                testMethod,
                testClassInstance,
                new DefaultParametersGenFactory(),
                GlobalConfiguration.GetInstance(testClassType.Assembly).GenSnapshots);

            var failures = new List<Exception>();
            for (var seedIndex = 0; seedIndex < initialization.Seeds.Count; seedIndex++)
            {
                var (exception, _) = await GenSnapshotRunner.Run(initialization, testClassType, testMethod, seedIndex);
                if (exception != null)
                {
                    failures.Add(exception);
                }
            }

            if (failures.Count == 1)
            {
                throw failures[0];
            }

            if (failures.Count > 1)
            {
                throw new AggregateException(failures);
            }
        }
    }
}
