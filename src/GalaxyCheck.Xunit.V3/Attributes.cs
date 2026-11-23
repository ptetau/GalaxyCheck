using GalaxyCheck.Internal.Infrastructure.XunitV3;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

// The xunit v3 part of each attribute. The options are in the shared core.

namespace GalaxyCheck
{
    [XunitTestCaseDiscoverer(typeof(PropertyDiscoverer))]
    public partial class PropertyAttribute : FactAttribute, ITheoryAttribute
    {
        // The constructors match the xunit v2 package, so xunit gets no source line for a property.
        /// <summary>Creates the attribute with the default options.</summary>
        public PropertyAttribute()
            : base(null, -1)
        {
        }

        bool ITheoryAttribute.DisableDiscoveryEnumeration => false;

        bool ITheoryAttribute.IncludeTestCaseIndex => false;

        bool ITheoryAttribute.SkipTestWithoutData => false;

        // xunit skips the test before it runs, so the shared core never sees a skip reason.
        internal string? SkipReason => null;
    }

    [XunitTestCaseDiscoverer(typeof(PropertyDiscoverer))]
    public partial class SampleAttribute
    {
        /// <summary>Creates the attribute with the default options.</summary>
        public SampleAttribute()
        {
        }
    }

    [XunitTestCaseDiscoverer(typeof(GenSnapshotDiscoverer))]
    public sealed partial class GenSnapshotAttribute : FactAttribute
    {
        /// <summary>Creates the attribute. The compiler passes the path of the test file.</summary>
        public GenSnapshotAttribute([CallerFilePath] string? testFilePath = null)
            : base(testFilePath, -1)
        {
            TestFileName = testFilePath;
        }

        /// <summary>The path of the file of the test method.</summary>
        public string? TestFileName { get; }

        internal string? SkipReason => null;
    }
}
