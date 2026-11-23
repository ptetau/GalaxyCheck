using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace GalaxyCheck.Internal.Infrastructure.Xunit
{
    /// <summary>Runs the property of one xunit test case and reports its result.</summary>
    public class PropertyTestRunner : TestCaseRunner<PropertyTestCase>
    {
        private readonly object[] _constructorArguments;
        private readonly object[] _testMethodArguments;

        /// <summary>Creates the runner of a test case.</summary>
        public PropertyTestRunner(
            PropertyTestCase testCase,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            object[] constructorArguments,
            object[] testMethodArguments)
            : base(testCase, messageBus, aggregator, cancellationTokenSource)
        {
            _constructorArguments = ConstructorArguments.Resolve(constructorArguments);
            _testMethodArguments = testMethodArguments;
        }

        /// <summary>Runs the property and reports one result.</summary>
        protected override async Task<RunSummary> RunTestAsync()
        {
            var startTime = DateTime.UtcNow;

            var test = new XunitTest(TestCase, TestCase.DisplayName);
            var testOutputHelper = _constructorArguments
                .OfType<TestOutputHelper>()
                .FirstOrDefault() ?? new TestOutputHelper();
            testOutputHelper.Initialize(MessageBus, test);

            // Runners report a result only for a test that they saw start and finish.
            MessageBus.QueueMessage(new TestStarting(test));

            RunSummary Fail(Exception ex)
            {
                var executionTime = (decimal)((DateTime.UtcNow - startTime).TotalSeconds);
                MessageBus.QueueMessage(new TestFailed(test, executionTime, testOutputHelper!.Output, ex));
                MessageBus.QueueMessage(new TestFinished(test, executionTime, testOutputHelper!.Output));
                return new RunSummary() { Failed = 1, Total = 1, Skipped = 0, Time = executionTime };
            }

            RunSummary Pass()
            {
                var executionTime = (decimal)((DateTime.UtcNow - startTime).TotalSeconds);
                MessageBus.QueueMessage(new TestPassed(test, executionTime, testOutputHelper!.Output));
                MessageBus.QueueMessage(new TestFinished(test, executionTime, testOutputHelper!.Output));
                return new RunSummary() { Failed = 0, Total = 1, Skipped = 0, Time = executionTime };
            }

            RunSummary Skip(string reason)
            {
                var executionTime = (decimal)((DateTime.UtcNow - startTime).TotalSeconds);
                MessageBus.QueueMessage(new TestSkipped(test, reason));
                MessageBus.QueueMessage(new TestFinished(test, executionTime, testOutputHelper!.Output));
                return new RunSummary() { Failed = 0, Total = 0, Skipped = 1, Time = executionTime };
            }

            PropertyInitializationResult? propertyInitResult;
            try
            {
                propertyInitResult = PropertyInitializer.Initialize(
                    TestCase.TestMethod.TestClass.Class.ToRuntimeType(),
                    TestCase.TestMethod.Method.ToRuntimeMethod(),
                    _constructorArguments,
                    _testMethodArguments,
                    new DefaultPropertyFactory(),
                    GlobalConfiguration.GetInstance(TestCase.TestMethod.TestClass.Class.ToRuntimeType().Assembly).Properties,
                    GlobalConfiguration.GetInstance(TestCase.TestMethod.TestClass.Class.ToRuntimeType().Assembly).FailureMemory,
                    TestCase.RowIndex,
                    FailureMessage.XunitFilter(
                        TestCase.TestMethod.TestClass.Class.ToRuntimeType(),
                        TestCase.TestMethod.Method.ToRuntimeMethod(),
                        isDataRow: _testMethodArguments is { Length: > 0 },
                        TestCase.DisplayName));
            }
            catch (Exception exception)
            {
                return Fail(exception);
            }

            if (propertyInitResult.ShouldSkip)
            {
                return Skip(propertyInitResult.SkipReason!);
            }

            try
            {
                await propertyInitResult.Runner.Run(propertyInitResult.Parameters);
                return Pass();
            }
            catch (Exception propertyFailedException)
            {
                return Fail(propertyFailedException);
            }
        }
    }
}
