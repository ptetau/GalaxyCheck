using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace GalaxyCheck
{
    [XunitTestCaseDiscoverer("GalaxyCheck.Internal.Infrastructure.Xunit.GenSnapshotDiscoverer", "GalaxyCheck.Xunit")]
    public sealed partial class GenSnapshotAttribute : FactAttribute
    {
        public GenSnapshotAttribute([CallerFilePath] string? testFilePath = null)
        {
            TestFileName = testFilePath;
        }

        public string? TestFileName { get; }

        internal string? SkipReason => Skip?.Length > 0 ? Skip : null;
    }
}
