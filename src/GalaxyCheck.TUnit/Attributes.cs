using GalaxyCheck.Internal.Infrastructure.TUnit;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Interfaces;

// The TUnit part of each attribute. The options are in the shared core. TUnit finds a test only by [Test], so a
// property is written [Test, PropertyTest] (or [Test, Property] with a using alias). Each attribute is a data source, so that TUnit accepts the generated
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
            TUnitAdapter.PropertyRows(dataGeneratorMetadata);

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
            TUnitAdapter.PropertyRows(dataGeneratorMetadata);

        ValueTask ITestRegisteredEventReceiver.OnTestRegistered(TestRegisteredContext context)
        {
            context.SetTestExecutor(new GenSnapshotTestExecutor());
            return default;
        }
    }
}

namespace GalaxyCheck
{
    /// <summary>
    /// A property in TUnit, written <c>[Test, PropertyTest]</c>. It has the options and the behaviour of
    /// <see cref="PropertyAttribute"/>, whose name TUnit's own <c>[Property]</c> attribute hides.
    /// </summary>
    public sealed class PropertyTestAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// One data row of a <c>[Test, PropertyTest]</c> method: the values of the first parameters. The property
    /// generates the other parameters. Each attribute is one test.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PropertyArgumentsAttribute : System.Attribute
    {
        public PropertyArgumentsAttribute(params object?[]? values)
        {
            Values = values ?? new object?[] { null };
        }

        public object?[] Values { get; }
    }
}
