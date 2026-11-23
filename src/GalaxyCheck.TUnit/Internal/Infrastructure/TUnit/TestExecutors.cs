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
        /// The rows of a property. Each [PropertyArguments] is one row for the first parameters, and the property
        /// generates the rest. With no [PropertyArguments], one row with no values: the property generates each
        /// parameter. A method with another data source ([Arguments], [MethodDataSource]) gets no row from
        /// GalaxyCheck, so that each of its rows is one test.
        /// </summary>
        public static async IAsyncEnumerable<Func<Task<object?[]?>>> PropertyRows(DataGeneratorMetadata dataGeneratorMetadata)
        {
            await Task.CompletedTask;

            var method = dataGeneratorMetadata.TestInformation;
            var attributes = method == null
                ? Array.Empty<object>()
                : FindMethod(method.Type, method.Name, method.Parameters)?.GetCustomAttributes(inherit: true) ?? Array.Empty<object>();

            var hasOtherDataSource = attributes.Any(attribute =>
                attribute is IDataSourceAttribute && attribute is not PropertyAttribute && attribute is not GenSnapshotAttribute);
            if (hasOtherDataSource)
            {
                yield break;
            }

            var rows = attributes.OfType<PropertyArgumentsAttribute>().ToList();
            if (rows.Count == 0)
            {
                yield return () => Task.FromResult<object?[]?>(null);
                yield break;
            }

            foreach (var row in rows)
            {
                var values = row.Values;
                yield return () => Task.FromResult<object?[]?>((object?[])values.Clone());
            }
        }

        /// <summary>
        /// The index of the method data row, from the test id of TUnit, for example <c>0.1</c> for the second row of
        /// the first data source. The id has the form
        /// <c>Class(Types).{class source}.{class row}.Method(Types).{method source}.{method row}.{repeat}</c>, with
        /// indexes from 1 in the declared order, so the index is stable between runs. Null when the id has another form.
        /// </summary>
        public static string? RowIndex(TestDetails testDetails) =>
            ParseTestId(testDetails) is { } indexes ? indexes.Row : null;

        /// <summary>The index of the class data row, from the test id of TUnit, or null.</summary>
        public static string? ClassDataIndex(TestDetails testDetails) =>
            ParseTestId(testDetails) is { } indexes ? indexes.Class : null;

        private static readonly System.Text.RegularExpressions.Regex TrailingMethodIndexes =
            new(@"\.(\d+)\.(\d+)\.(\d+)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        private static readonly System.Text.RegularExpressions.Regex TrailingClassIndexes =
            new(@"\.(\d+)\.(\d+)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        private static (string Class, string Row)? ParseTestId(TestDetails testDetails)
        {
            var id = testDetails.TestId;
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            var method = TrailingMethodIndexes.Match(id);
            if (!method.Success)
            {
                return null;
            }

            // The rest ends with the method name and its parameter types: drop them to reach the class indexes.
            var rest = StripTrailingGroup(StripTrailingGroup(id.Substring(0, method.Index), '(', ')'), '<', '>');
            var methodName = "." + testDetails.MethodName;
            if (!rest.EndsWith(methodName, StringComparison.Ordinal))
            {
                return null;
            }

            var classPart = TrailingClassIndexes.Match(rest.Substring(0, rest.Length - methodName.Length));
            if (!classPart.Success)
            {
                return null;
            }

            return (
                ZeroBased(classPart.Groups[1].Value, classPart.Groups[2].Value),
                ZeroBased(method.Groups[1].Value, method.Groups[2].Value));
        }

        /// <summary>Removes a bracketed group at the end of the text, with the groups inside it.</summary>
        private static string StripTrailingGroup(string text, char open, char close)
        {
            if (text.Length == 0 || text[text.Length - 1] != close)
            {
                return text;
            }

            var depth = 0;
            for (var i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] == close)
                {
                    depth++;
                }
                else if (text[i] == open && --depth == 0)
                {
                    return text.Substring(0, i);
                }
            }

            return text;
        }

        private static string ZeroBased(string source, string row) =>
            $"{long.Parse(source, System.Globalization.CultureInfo.InvariantCulture) - 1}.{long.Parse(row, System.Globalization.CultureInfo.InvariantCulture) - 1}";

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
                GlobalConfiguration.GetInstance(testClassType.Assembly).Properties,
                GlobalConfiguration.GetInstance(testClassType.Assembly).FailureMemory,
                testDetails.TestClassArguments is { Length: > 0 } classArguments ? classArguments : null,
                TUnitAdapter.RowIndex(testDetails),
                TUnitAdapter.ClassDataIndex(testDetails),
                RerunStyle.TUnitTreeNodeFilter);

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
