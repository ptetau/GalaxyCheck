using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GalaxyCheck.Internal.Infrastructure.Xunit
{
    public class PropertyTestCase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public PropertyTestCase()
        {
        }

        public PropertyTestCase(
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay defaultMethodDisplay,
            TestMethodDisplayOptions defaultMethodDisplayOptions,
            ITestMethod testMethod,
            object[]? testMethodArguments,
            string? rowIndex = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
            RowIndex = rowIndex;
        }

        /// <summary>The index of the data row among the rows of the method, in the declared order, or null.</summary>
        public string? RowIndex { get; private set; }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue(nameof(RowIndex), RowIndex);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);
            RowIndex = data.GetValue<string?>(nameof(RowIndex));
        }

        public override Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            return new PropertyTestRunner(this, messageBus, aggregator, cancellationTokenSource, constructorArguments, TestMethodArguments).RunAsync();
        }
    }
}
