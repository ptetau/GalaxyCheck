using GalaxyCheck;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GalaxyCheck.Internal.Infrastructure.Xunit
{
    public class GenSnapshotTestCase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public GenSnapshotTestCase()
        {
        }

        public GenSnapshotTestCase(
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay defaultMethodDisplay,
            TestMethodDisplayOptions defaultMethodDisplayOptions,
            ITestMethod testMethod,
            object[]? testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
        }

        public override async Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            EnsureInitialized();

            messageBus.QueueMessage(new TestCaseStarting(this));

            var startTime = DateTime.UtcNow;
            var test = new XunitTest(this, DisplayName);
            var testOutputHelper = constructorArguments
                .OfType<TestOutputHelper>()
                .FirstOrDefault() ?? new TestOutputHelper();
            testOutputHelper.Initialize(messageBus, test);

            // Runners report a result only for a test that they saw start and finish. The seeds run concurrently, so
            // the lock keeps the three messages of one result together.
            var reportLock = new object();
            void Report(IMessageSinkMessage result, decimal executionTime)
            {
                lock (reportLock)
                {
                    messageBus.QueueMessage(new TestStarting(test));
                    messageBus.QueueMessage(result);
                    messageBus.QueueMessage(new TestFinished(test, executionTime, testOutputHelper!.Output));
                }
            }

            Task<RunSummary> Fail(Exception ex)
            {
                var executionTime = (decimal)(DateTime.UtcNow - startTime).TotalSeconds;
                Report(new TestFailed(test, executionTime, testOutputHelper!.Output, ex), executionTime);
                return Task.FromResult(new RunSummary() { Total = 0, Failed = 1 });
            }

            var testClassType = TestMethod.TestClass.Class.ToRuntimeType();
            var testMethod = TestMethod.Method.ToRuntimeMethod();

            GenSnapshotInitializationResult? initialization;
            try
            {
                initialization = GenSnapshotInitializer.Initialize(
                    testClassType,
                    testMethod,
                    constructorArguments,
                    new DefaultParametersGenFactory(),
                    GlobalConfiguration.GetInstance(testClassType.Assembly).GenSnapshots);
            }
            catch (Exception exception)
            {
                return await Fail(exception);
            }

            if (initialization.ShouldSkip)
            {
                Report(new TestSkipped(test, initialization.SkipReason), 0);
                return new RunSummary() { Skipped = initialization.Seeds.Count };
            }

            var results = await Task.WhenAll(initialization.Seeds
                .Select(async (_, seedIndex) =>
                {
                    var (exception, executionTime) = await GenSnapshotRunner.Run(initialization, testClassType, testMethod, seedIndex);

                    if (exception == null)
                    {
                        Report(new TestPassed(test, executionTime, testOutputHelper!.Output), executionTime);
                        return true;
                    }
                    else
                    {
                        Report(new TestFailed(test, executionTime, testOutputHelper!.Output, exception), executionTime);
                        return false;
                    }
                })
                .ToList());

            // TODO: Properly measure execution time
            var failures = results.Count(result => result == false);
            return new RunSummary() { Total = results.Count(), Failed = failures };
        }
    }
}
