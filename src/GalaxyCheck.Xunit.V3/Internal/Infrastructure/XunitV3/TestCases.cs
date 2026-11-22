using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace GalaxyCheck.Internal.Infrastructure.XunitV3
{
    /// <summary>One [Property] or [Sample] test, with or without a data row.</summary>
    internal sealed class PropertyTestCase : XunitTestCase, ISelfExecutingXunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public PropertyTestCase()
        {
        }

        public PropertyTestCase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            string? testLabel,
            bool disableParallelization,
            Type[]? skipExceptions,
            string? skipReason,
            Type? skipType,
            string? skipUnless,
            string? skipWhen,
            Dictionary<string, HashSet<string>>? traits,
            object?[]? testMethodArguments,
            string? sourceFilePath,
            int? sourceLineNumber,
            int? timeout)
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, testLabel, disableParallelization, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout)
        {
        }

        public ValueTask<RunSummary> Run(
            ExplicitOption explicitOption,
            IMessageBus messageBus,
            object?[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            ParallelMode parallelMode,
            ExecutionScheduler scheduler,
            FixtureMappingManager methodFixtureMappings) =>
                GalaxyCheckTestCaseRunner.Instance.Run(PropertyBody.Instance, this, messageBus, constructorArguments, explicitOption, aggregator, cancellationTokenSource, parallelMode, scheduler, methodFixtureMappings);
    }

    /// <summary>A [Property] whose data rows are read when it runs, because a row does not serialize.</summary>
    internal sealed class PropertyDelayEnumeratedTestCase : XunitDelayEnumeratedTheoryTestCase, ISelfExecutingXunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public PropertyDelayEnumeratedTestCase()
        {
        }

        public PropertyDelayEnumeratedTestCase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            bool skipTestWithoutData,
            Type[]? skipExceptions,
            string? skipReason,
            Type? skipType,
            string? skipUnless,
            string? skipWhen,
            Dictionary<string, HashSet<string>>? traits,
            string? sourceFilePath,
            int? sourceLineNumber,
            int? timeout)
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipTestWithoutData, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, sourceFilePath, sourceLineNumber, timeout)
        {
        }

        public ValueTask<RunSummary> Run(
            ExplicitOption explicitOption,
            IMessageBus messageBus,
            object?[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            ParallelMode parallelMode,
            ExecutionScheduler scheduler,
            FixtureMappingManager methodFixtureMappings) =>
                GalaxyCheckTestCaseRunner.Instance.Run(PropertyBody.Instance, this, messageBus, constructorArguments, explicitOption, aggregator, cancellationTokenSource, parallelMode, scheduler, methodFixtureMappings);
    }

    /// <summary>One [GenSnapshot] test, which compares the value of each seed.</summary>
    internal sealed class GenSnapshotTestCase : XunitTestCase, ISelfExecutingXunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public GenSnapshotTestCase()
        {
        }

        public GenSnapshotTestCase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            Type[]? skipExceptions,
            string? skipReason,
            Type? skipType,
            string? skipUnless,
            string? skipWhen,
            Dictionary<string, HashSet<string>>? traits,
            string? sourceFilePath,
            int? sourceLineNumber,
            int? timeout)
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, null, sourceFilePath, sourceLineNumber, timeout)
        {
        }

        public ValueTask<RunSummary> Run(
            ExplicitOption explicitOption,
            IMessageBus messageBus,
            object?[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            ParallelMode parallelMode,
            ExecutionScheduler scheduler,
            FixtureMappingManager methodFixtureMappings) =>
                GalaxyCheckTestCaseRunner.Instance.Run(GenSnapshotBody.Instance, this, messageBus, constructorArguments, explicitOption, aggregator, cancellationTokenSource, parallelMode, scheduler, methodFixtureMappings);
    }
}
