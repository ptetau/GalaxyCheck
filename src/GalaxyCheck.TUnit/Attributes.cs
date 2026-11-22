using GalaxyCheck.Internal.Infrastructure.TUnit;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Interfaces;

// The TUnit part of each attribute. The options are in the shared core. TUnit finds a test only by [Test], so a
// property is written [Test, Property]. Each attribute is a data source, so that TUnit accepts the generated
// parameters, and it sets the test executor that runs the property in place of one call of the method.

namespace GalaxyCheck
{
    public partial class PropertyAttribute : System.Attribute, IDataSourceAttribute, ITestRegisteredEventReceiver
    {
        internal string? SkipReason => null;

        int IEventReceiver.Order => 0;

        bool IDataSourceAttribute.SkipIfEmpty { get; set; }

        bool IDataSourceAttribute.DeferEnumeration { get => false; set { } }

        IAsyncEnumerable<System.Func<Task<object?[]?>>> IDataSourceAttribute.GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata) =>
            TUnitAdapter.PlaceholderRow(dataGeneratorMetadata);

        ValueTask ITestRegisteredEventReceiver.OnTestRegistered(TestRegisteredContext context)
        {
            context.SetTestExecutor(new PropertyTestExecutor());
            return default;
        }
    }

    public sealed partial class GenSnapshotAttribute : System.Attribute, IDataSourceAttribute, ITestRegisteredEventReceiver
    {
        public GenSnapshotAttribute([CallerFilePath] string? testFilePath = null)
        {
            TestFileName = testFilePath;
        }

        public string? TestFileName { get; }

        internal string? SkipReason => null;

        int IEventReceiver.Order => 0;

        bool IDataSourceAttribute.SkipIfEmpty { get; set; }

        bool IDataSourceAttribute.DeferEnumeration { get => false; set { } }

        IAsyncEnumerable<System.Func<Task<object?[]?>>> IDataSourceAttribute.GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata) =>
            TUnitAdapter.PlaceholderRow(dataGeneratorMetadata);

        ValueTask ITestRegisteredEventReceiver.OnTestRegistered(TestRegisteredContext context)
        {
            context.SetTestExecutor(new GenSnapshotTestExecutor());
            return default;
        }
    }
}
