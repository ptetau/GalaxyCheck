using GalaxyCheck.Gens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GalaxyCheck.Internal.Infrastructure.MSTest
{
    internal static class MSTestAdapter
    {
        /// <summary>
        /// Runs the property of a [Property] or [Sample] method as one test. Each test case is one call of
        /// <see cref="ITestMethod.InvokeAsync"/>, so MSTest makes the instance of the class, sets its test context,
        /// and runs its initialize and cleanup methods, as for a test.
        /// </summary>
        public static Task<TestResult[]> RunProperty(ITestMethod testMethod)
        {
            var output = new InvocationOutput();
            return Run(output, async () =>
            {
                var methodInfo = testMethod.MethodInfo;
                var testClassType = methodInfo.ReflectedType ?? methodInfo.DeclaringType!;
                var arguments = testMethod.Arguments;
                var globalConfiguration = GlobalConfiguration.GetInstance(testClassType.Assembly);

                var initialization = PropertyInitializer.InitializeWithInstance(
                    testClassType,
                    methodInfo,
                    testClassInstance: null,
                    arguments is { Length: > 0 } ? arguments : null,
                    new InvokingPropertyFactory(testMethod, output),
                    globalConfiguration.Properties,
                    globalConfiguration.FailureMemory,
                    rowIndex: RowIndex(methodInfo, arguments));

                await initialization.Runner.Run(initialization.Parameters);
            });
        }

        /// <summary>
        /// The index of the data row among the rows of the data sources of the method, in the declared order, or
        /// null when no row equals the arguments. The failure memory needs it only for a value that enters its key as
        /// a type name, so a row of plain values reads no data source.
        /// </summary>
        private static string? RowIndex(MethodInfo methodInfo, object?[]? arguments)
        {
            if (arguments is not { Length: > 0 } || arguments.All(HasPlainValue))
            {
                return null;
            }

            try
            {
                var index = 0;
                foreach (var source in methodInfo.GetCustomAttributes(inherit: true).OfType<ITestDataSource>())
                {
                    foreach (var row in source.GetData(methodInfo))
                    {
                        if (row != null && row.Length == arguments.Length && row.Zip(arguments, SameValue).All(same => same))
                        {
                            return index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }

                        index++;
                    }
                }
            }
            catch (Exception)
            {
                // A data source that throws here gives no index: the key then holds the type name alone.
            }

            return null;
        }

        private static bool HasPlainValue(object? value) =>
            value is null or string or Type or Enum or decimal || value.GetType().IsPrimitive;

        private static bool SameValue(object? left, object? right) =>
            left is System.Collections.IStructuralEquatable || right is System.Collections.IStructuralEquatable
                ? System.Collections.StructuralComparisons.StructuralEqualityComparer.Equals(left, right)
                : Equals(left, right);

        private static async Task<TestResult[]> Run(InvocationOutput output, Func<Task> body)
        {
            var stopwatch = Stopwatch.StartNew();
            Exception? failure = null;
            try
            {
                await body();
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            return new[]
            {
                new TestResult
                {
                    Outcome = failure == null ? UnitTestOutcome.Passed : UnitTestOutcome.Failed,
                    TestFailureException = failure,
                    Duration = stopwatch.Elapsed,
                    LogOutput = output.LogOutput,
                    LogError = output.LogError,
                    DebugTrace = output.DebugTrace,
                    TestContextMessages = output.TestContextMessages,
                },
            };
        }

        /// <summary>The exception that the method threw, without the wrapper of MSTest.</summary>
        public static Exception Unwrap(Exception exception)
        {
            while (exception.GetType().Namespace?.StartsWith("Microsoft.VisualStudio.TestPlatform.MSTest", StringComparison.Ordinal) == true &&
                exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            return exception;
        }
    }

    /// <summary>The console output and the messages of each call of the test method, in order.</summary>
    internal sealed class InvocationOutput
    {
        private readonly object _gate = new();
        private readonly StringBuilder _logOutput = new();
        private readonly StringBuilder _logError = new();
        private readonly StringBuilder _debugTrace = new();
        private readonly StringBuilder _testContextMessages = new();

        public string? LogOutput => Text(_logOutput);

        public string? LogError => Text(_logError);

        public string? DebugTrace => Text(_debugTrace);

        public string? TestContextMessages => Text(_testContextMessages);

        public void Add(TestResult result)
        {
            lock (_gate)
            {
                _logOutput.Append(result.LogOutput);
                _logError.Append(result.LogError);
                _debugTrace.Append(result.DebugTrace);
                _testContextMessages.Append(result.TestContextMessages);
            }
        }

        private string? Text(StringBuilder builder)
        {
            lock (_gate)
            {
                return builder.Length == 0 ? null : builder.ToString();
            }
        }
    }

    /// <summary>
    /// Builds the property of a test method whose test function is one call of the method through MSTest. MSTest
    /// finds only a method that returns void, Task, or ValueTask, so the result of the call is its outcome.
    /// </summary>
    internal sealed class InvokingPropertyFactory : IPropertyFactory
    {
        private readonly ITestMethod _testMethod;
        private readonly InvocationOutput _output;

        public InvokingPropertyFactory(ITestMethod testMethod, InvocationOutput output)
        {
            _testMethod = testMethod;
            _output = output;
        }

        public AsyncProperty CreateProperty(
            MethodInfo methodInfo,
            object? target,
            IGenFactory? genFactory,
            IReadOnlyDictionary<int, IGen> customGens,
            object?[]? controlData)
        {
            var isToken = methodInfo.GetParameters()
                .Select(parameter => parameter.ParameterType == typeof(CancellationToken))
                .ToArray();

            return new AsyncProperty(Gen.Parameters(methodInfo, genFactory, customGens).Select(parameters =>
                (Property.AsyncTest<object>)new InvokedTest(
                    parameters,
                    parameters.Where((_, index) => index >= isToken.Length || !isToken[index]).ToArray(),
                    () => Invoke(parameters))));
        }

        private async ValueTask<Property.TestOutput> Invoke(object[] parameters)
        {
            TestResult result;
            try
            {
                result = await _testMethod.InvokeAsync(parameters);
                _output.Add(result);
            }
            catch (Exception ex)
            {
                return new Output(Property.TestResult.Failed, MSTestAdapter.Unwrap(ex));
            }

            return result.Outcome == UnitTestOutcome.Passed
                ? new Output(Property.TestResult.Succeeded, null)
                : new Output(
                    Property.TestResult.Failed,
                    result.TestFailureException == null ? null : MSTestAdapter.Unwrap(result.TestFailureException));
        }

        private sealed record Output(Property.TestResult Result, Exception? Exception) : Property.TestOutput;

        private sealed class InvokedTest : Property.AsyncTest<object>
        {
            public InvokedTest(object[] input, IReadOnlyList<object?> presentedInput, Func<ValueTask<Property.TestOutput>> run)
            {
                Input = input;
                PresentedInput = presentedInput;
                Output = new Lazy<ValueTask<Property.TestOutput>>(run, isThreadSafe: false);
            }

            public object Input { get; }

            public IReadOnlyList<object?> PresentedInput { get; }

            public Lazy<ValueTask<Property.TestOutput>> Output { get; }
        }
    }
}
