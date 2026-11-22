using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace GalaxyCheck.Internal.Infrastructure.XunitV3
{
    /// <summary>
    /// The xunit test pipeline (class construction, output helper, fixtures, before and after attributes, and the
    /// result messages), with the invocation of the test method replaced by a GalaxyCheck body.
    /// </summary>
    internal sealed class GalaxyCheckTestRunnerContext : XunitTestRunnerBaseContext<IXunitTest>
    {
        private readonly ITestBody _body;

        public GalaxyCheckTestRunnerContext(
            ITestBody body,
            IXunitTest test,
            ExplicitOption explicitOption,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            ParallelMode parallelMode,
            ExecutionScheduler scheduler,
            IReadOnlyCollection<IBeforeAfterTestAttribute> beforeAfterTestAttributes,
            object?[] constructorArguments,
            FixtureMappingManager caseFixtureMappings)
            : base(test, explicitOption, messageBus, aggregator, cancellationTokenSource, parallelMode, scheduler, beforeAfterTestAttributes, constructorArguments, caseFixtureMappings)
        {
            _body = body;
        }

        // The base checks that the data row fills each parameter. A property generates the parameters that the row
        // does not give, so this replaces the check and the call.
        public override ValueTask<TimeSpan> InvokeTest(object? testClassInstance) =>
            ExecutionTimer.MeasureAsync(() => Aggregator.RunAsync(() => new ValueTask(_body.Run(Test, testClassInstance))));
    }

    internal sealed class GalaxyCheckTestRunner : XunitTestRunnerBase<GalaxyCheckTestRunnerContext, IXunitTest>
    {
        public static GalaxyCheckTestRunner Instance { get; } = new();

        public async ValueTask<RunSummary> Run(
            ITestBody body,
            IXunitTest test,
            IMessageBus messageBus,
            object?[] constructorArguments,
            ExplicitOption explicitOption,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            ParallelMode parallelMode,
            ExecutionScheduler scheduler,
            IReadOnlyCollection<IBeforeAfterTestAttribute> beforeAfterAttributes,
            FixtureMappingManager caseFixtureMappings)
        {
            await using var ctxt = new GalaxyCheckTestRunnerContext(
                body,
                test,
                explicitOption,
                messageBus,
                aggregator,
                cancellationTokenSource,
                parallelMode,
                scheduler,
                beforeAfterAttributes,
                constructorArguments,
                caseFixtureMappings);
            await ctxt.InitializeAsync();

            return await Run(ctxt);
        }
    }

    internal sealed class GalaxyCheckTestCaseRunnerContext : XunitTestCaseRunnerBaseContext<IXunitTestCase, IXunitTest>
    {
        private readonly ITestBody _body;

        public GalaxyCheckTestCaseRunnerContext(
            ITestBody body,
            IXunitTestCase testCase,
            IReadOnlyCollection<IXunitTest> tests,
            ExplicitOption explicitOption,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            string displayName,
            string? skipReason,
            CancellationTokenSource cancellationTokenSource,
            ParallelMode parallelMode,
            ExecutionScheduler scheduler,
            object?[] constructorArguments,
            FixtureMappingManager methodFixtureMappings)
            : base(testCase, tests, explicitOption, messageBus, aggregator, displayName, skipReason, cancellationTokenSource, parallelMode, scheduler, constructorArguments, methodFixtureMappings)
        {
            _body = body;
        }

        public override ValueTask<RunSummary> RunTest(IXunitTest test) =>
            GalaxyCheckTestRunner.Instance.Run(
                _body,
                test,
                MessageBus,
                ConstructorArguments,
                ExplicitOption,
                Aggregator.Clone(),
                CancellationTokenSource,
                ParallelMode,
                Scheduler,
                BeforeAfterTestAttributes,
                CaseFixtureMappings);
    }

    internal sealed class GalaxyCheckTestCaseRunner : XunitTestCaseRunnerBase<GalaxyCheckTestCaseRunnerContext, IXunitTestCase, IXunitTest>
    {
        public static GalaxyCheckTestCaseRunner Instance { get; } = new();

        /// <summary>Runs a test case as xunit runs a test case of its own, with the body in place of the method call.</summary>
        public async ValueTask<RunSummary> Run(
            ITestBody body,
            IXunitTestCase testCase,
            IMessageBus messageBus,
            object?[] constructorArguments,
            ExplicitOption explicitOption,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            ParallelMode parallelMode,
            ExecutionScheduler scheduler,
            FixtureMappingManager methodFixtureMappings)
        {
            var tests = await aggregator.RunAsync(testCase.CreateTests, []);

            if (aggregator.ToException() is Exception exception)
            {
                return XunitRunnerHelper.FailTestCases(messageBus, cancellationTokenSource, [testCase], exception);
            }

            await using var ctxt = new GalaxyCheckTestCaseRunnerContext(
                body,
                testCase,
                tests,
                explicitOption,
                messageBus,
                aggregator,
                testCase.TestCaseDisplayName,
                testCase.SkipReason,
                cancellationTokenSource,
                parallelMode,
                scheduler,
                constructorArguments,
                methodFixtureMappings);
            await ctxt.InitializeAsync();

            return await Run(ctxt);
        }
    }
}
