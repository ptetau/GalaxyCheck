using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace GalaxyCheck
{
    [XunitTestCaseDiscoverer("GalaxyCheck.Internal.Infrastructure.Xunit.GenSnapshotDiscoverer", "GalaxyCheck.Xunit")]
    public sealed partial class GenSnapshotAttribute : FactAttribute
    {
        /// <summary>Creates the attribute. The compiler passes the path of the test file.</summary>
        public GenSnapshotAttribute([CallerFilePath] string? testFilePath = null)
        {
            TestFileName = testFilePath;
        }

        /// <summary>The path of the file of the test method.</summary>
        public string? TestFileName { get; }

        internal string? SkipReason => Skip?.Length > 0 ? Skip : null;
    }
}
