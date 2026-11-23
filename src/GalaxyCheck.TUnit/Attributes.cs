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
        /// <summary>Creates the attribute. The compiler passes the path of the test file.</summary>
        public GenSnapshotAttribute([CallerFilePath] string? testFilePath = null)
        {
            TestFileName = testFilePath;
        }

        /// <summary>The path of the file of the test method.</summary>
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
        // Each option is declared again here, so that it shows on this type; the value is stored by the base class.

        /// <inheritdoc cref="PropertyAttribute.Iterations"/>
        public new int Iterations
        {
            get => base.Iterations;
            set => base.Iterations = value;
        }

        /// <inheritdoc cref="PropertyAttribute.ShrinkLimit"/>
        public new int ShrinkLimit
        {
            get => base.ShrinkLimit;
            set => base.ShrinkLimit = value;
        }

        /// <inheritdoc cref="PropertyAttribute.Seed"/>
        public new int Seed
        {
            get => base.Seed;
            set => base.Seed = value;
        }

        /// <inheritdoc cref="PropertyAttribute.Size"/>
        public new int Size
        {
            get => base.Size;
            set => base.Size = value;
        }

        /// <inheritdoc cref="PropertyAttribute.TimeoutMilliseconds"/>
        public new int TimeoutMilliseconds
        {
            get => base.TimeoutMilliseconds;
            set => base.TimeoutMilliseconds = value;
        }

        /// <inheritdoc cref="PropertyAttribute.Exhaustive"/>
        public new bool Exhaustive
        {
            get => base.Exhaustive;
            set => base.Exhaustive = value;
        }
    }

    /// <summary>
    /// One data row of a <c>[Test, PropertyTest]</c> method: the values of the first parameters. The property
    /// generates the other parameters. Each attribute is one test.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PropertyArgumentsAttribute : System.Attribute
    {
        /// <summary>Creates one data row with values for the first parameters.</summary>
        public PropertyArgumentsAttribute(params object?[]? values)
        {
            Values = values ?? new object?[] { null };
        }

        /// <summary>The values of the row, in the order of the parameters.</summary>
        public object?[] Values { get; }
    }
}
