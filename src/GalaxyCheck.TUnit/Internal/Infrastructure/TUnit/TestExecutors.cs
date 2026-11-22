using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace GalaxyCheck.Internal.Infrastructure.TUnit
{
    internal static class TUnitAdapter
    {
        /// <summary>
        /// One row with no values: the property generates each parameter when the test runs. A method with another data
        /// source ([Arguments], [MethodDataSource]) gets no row from GalaxyCheck, so that each of its rows is one test.
        /// </summary>
        public static async IAsyncEnumerable<Func<Task<object?[]?>>> PlaceholderRow(DataGeneratorMetadata dataGeneratorMetadata)
        {
            await Task.CompletedTask;

            var method = dataGeneratorMetadata.TestInformation;
            var hasOtherDataSource = method != null && FindMethod(method.Type, method.Name, method.Parameters)?
                .GetCustomAttributes(inherit: true)
                .Any(attribute => attribute is IDataSourceAttribute && attribute is not PropertyAttribute && attribute is not GenSnapshotAttribute) == true;

            if (!hasOtherDataSource)
            {
                yield return () => Task.FromResult<object?[]?>(null);
            }
        }

        public static MethodInfo GetTestMethod(TestDetails testDetails) =>
            FindMethod(testDetails.ClassType, testDetails.MethodName, testDetails.MethodMetadata.Parameters)
                ?? throw new InvalidOperationException($"Could not find method '{testDetails.MethodName}' on type '{testDetails.ClassType.FullName}'");

        private static MethodInfo? FindMethod(Type type, string name, ParameterMetadata[] parameters) =>
            type.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                parameters.Select(parameter => parameter.Type).ToArray());
    }

    /// <summary>Runs the property of a [Property] or [Sample] method in place of one call of the method.</summary>
    internal sealed class PropertyTestExecutor : ITestExecutor
    {
        public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
        {
            var testDetails = context.Metadata.TestDetails;
            var testClassType = testDetails.ClassType;
            var arguments = testDetails.TestMethodArguments;

            var initialization = PropertyInitializer.InitializeWithInstance(
                testClassType,
                TUnitAdapter.GetTestMethod(testDetails),
                testDetails.ClassInstance,
                arguments.Length == 0 ? null : arguments,
                new DefaultPropertyFactory(),
                GlobalConfiguration.GetInstance(testClassType.Assembly).Properties);

            await initialization.Runner.Run(initialization.Parameters);
        }
    }

    /// <summary>Compares the value of each seed of a [GenSnapshot] method with the stored snapshot.</summary>
    internal sealed class GenSnapshotTestExecutor : ITestExecutor
    {
        public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
        {
            var testDetails = context.Metadata.TestDetails;
            var testClassType = testDetails.ClassType;
            var testMethod = TUnitAdapter.GetTestMethod(testDetails);

            var initialization = GenSnapshotInitializer.InitializeWithInstance(
                testClassType,
                testMethod,
                testDetails.ClassInstance,
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
